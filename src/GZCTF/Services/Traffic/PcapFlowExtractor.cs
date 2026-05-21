using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using GZCTF.Models.Internal;
using GZCTF.Storage.Interface;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace GZCTF.Services.Traffic;

/// <summary>
/// Reads the gzipped pcap files written by <see cref="TrafficRecorder"/> and
/// rehydrates them into per-TCP-session "flows" for the monitor UI.
///
/// The on-disk format is a synthetic Ethernet/IPv6/UDP wrapper — see
/// <see cref="TrafficRecorder.WritePcapPacket"/>. UDP src/dst port carry the
/// per-connection identifier in <c>[10001, 65000]</c> (a metadata frame on
/// port 10000 is skipped). Direction is whichever side holds the connection
/// port: source-port &gt;= 10001 means team-&gt;container (a <c>WriteAsync</c>
/// fired in <see cref="GZCTF.Utils.CaptureNetworkStream"/>), otherwise the
/// frame came from <c>ReadAsync</c> and is container-&gt;team.
/// </summary>
public interface IPcapFlowExtractor
{
    Task<IReadOnlyList<TrafficFlowSummary>> ListFlowsAsync(
        string blobPath,
        IReadOnlyList<string> flags,
        FlowFilter? filter,
        CancellationToken token);

    Task<TrafficFlowDetail?> GetFlowAsync(
        string blobPath,
        int connectionPort,
        IReadOnlyList<string> flags,
        CancellationToken token);
}

public sealed class PcapFlowExtractor(IBlobStorage storage, ILogger<PcapFlowExtractor> logger)
    : IPcapFlowExtractor
{
    private const int PortMin = 10001;
    private const int MetaPort = 10000;

    public async Task<IReadOnlyList<TrafficFlowSummary>> ListFlowsAsync(
        string blobPath,
        IReadOnlyList<string> flags,
        FlowFilter? filter,
        CancellationToken token)
    {
        var temp = await DownloadDecompressedAsync(blobPath, token);
        try
        {
            var accumulators = ReadFlows(temp, includePayloads: filter?.RegexPattern is not null || flags.Count > 0, token);
            var flagBytes = ToFlagByteArrays(flags);

            var summaries = accumulators
                .Select(a => a.ToSummary(flagBytes))
                .ToList();

            if (filter is not null)
                summaries = ApplyFilter(accumulators, summaries, filter);

            return summaries;
        }
        finally
        {
            TryDelete(temp);
        }
    }

    public async Task<TrafficFlowDetail?> GetFlowAsync(
        string blobPath,
        int connectionPort,
        IReadOnlyList<string> flags,
        CancellationToken token)
    {
        var temp = await DownloadDecompressedAsync(blobPath, token);
        try
        {
            var accumulators = ReadFlows(temp, includePayloads: true, token);
            var match = accumulators.FirstOrDefault(a => a.ConnectionPort == connectionPort);
            if (match is null) return null;

            var flagBytes = ToFlagByteArrays(flags);
            return match.ToDetail(flagBytes);
        }
        finally
        {
            TryDelete(temp);
        }
    }

    private async Task<string> DownloadDecompressedAsync(string blobPath, CancellationToken token)
    {
        var temp = Path.GetTempFileName();
        try
        {
            await using var src = await storage.OpenReadAsync(blobPath, token);
            await using var gz = new GZipStream(src, CompressionMode.Decompress, leaveOpen: false);
            await using var dst = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await gz.CopyToAsync(dst, token);
            return temp;
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    private List<FlowAccumulator> ReadFlows(string pcapPath, bool includePayloads, CancellationToken token)
    {
        using var device = new CaptureFileReaderDevice(pcapPath);
        device.Open();

        var flows = new Dictionary<int, FlowAccumulator>();

        while (true)
        {
            token.ThrowIfCancellationRequested();
            var status = device.GetNextPacket(out var capture);
            if (status != GetPacketStatus.PacketRead) break;

            var raw = capture.GetPacket();
            EthernetPacket? eth;
            try
            {
                eth = Packet.ParsePacket(raw.LinkLayerType, raw.Data) as EthernetPacket;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "PcapFlowExtractor: failed to parse frame, skipping");
                continue;
            }

            if (eth?.PayloadPacket is not IPv6Packet ipv6) continue;
            if (ipv6.PayloadPacket is not UdpPacket udp) continue;

            // Skip the metadata frame written once per recorder
            // (TrafficRecorder.cs: `MetadataHost = new(IPv6Any, 65535)` → peerIp ::).
            if (ipv6.SourceAddress.Equals(IPAddress.IPv6Any) ||
                ipv6.DestinationAddress.Equals(IPAddress.IPv6Any))
                continue;

            int srcPort = udp.SourcePort;
            int dstPort = udp.DestinationPort;
            if (srcPort == MetaPort || dstPort == MetaPort) continue;

            int connectionPort;
            TrafficFlowDirection direction;
            IPAddress peerAddress;

            if (srcPort >= PortMin)
            {
                connectionPort = srcPort;
                direction = TrafficFlowDirection.TeamToContainer;
                peerAddress = ipv6.SourceAddress;
            }
            else if (dstPort >= PortMin)
            {
                connectionPort = dstPort;
                direction = TrafficFlowDirection.ContainerToTeam;
                peerAddress = ipv6.DestinationAddress;
            }
            else
            {
                continue;
            }

            var payload = udp.PayloadData ?? Array.Empty<byte>();
            var ts = new DateTimeOffset(raw.Timeval.Date, TimeSpan.Zero);
            var peerIp = NormalizeIp(peerAddress);

            if (!flows.TryGetValue(connectionPort, out var acc))
                flows[connectionPort] = acc = new FlowAccumulator(connectionPort, peerIp);

            acc.Add(direction, payload, ts, retainPayload: includePayloads);
        }

        return flows.Values.OrderBy(f => f.FirstSeenUtc).ToList();
    }

    private static List<TrafficFlowSummary> ApplyFilter(
        List<FlowAccumulator> accumulators,
        List<TrafficFlowSummary> summaries,
        FlowFilter filter)
    {
        var result = new List<TrafficFlowSummary>(summaries.Count);

        Regex? regex = null;
        if (!string.IsNullOrEmpty(filter.RegexPattern))
        {
            try
            {
                regex = new Regex(filter.RegexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase,
                    TimeSpan.FromMilliseconds(500));
            }
            catch (ArgumentException)
            {
                // Invalid user regex → return empty rather than 500.
                return result;
            }
        }

        for (var i = 0; i < summaries.Count; i++)
        {
            var summary = summaries[i];
            var acc = accumulators[i];

            if (filter.PeerIpContains is { Length: > 0 } needle &&
                !summary.PeerIp.Contains(needle, StringComparison.OrdinalIgnoreCase))
                continue;

            if (filter.StartUtc is { } startUtc && summary.LastSeenUtc < startUtc) continue;
            if (filter.EndUtc is { } endUtc && summary.FirstSeenUtc > endUtc) continue;

            if (filter.Direction is { } direction)
            {
                if (direction == TrafficFlowDirection.ContainerToTeam && summary.PacketsIn == 0) continue;
                if (direction == TrafficFlowDirection.TeamToContainer && summary.PacketsOut == 0) continue;
            }

            if (filter.FlagsOnly && summary.FlagHits == 0) continue;

            if (regex is not null)
            {
                var ascii = acc.RenderAscii();
                try
                {
                    if (!regex.IsMatch(ascii)) continue;
                }
                catch (RegexMatchTimeoutException)
                {
                    continue;
                }
            }

            result.Add(summary);
        }

        return result;
    }

    private static byte[][] ToFlagByteArrays(IReadOnlyList<string> flags) =>
        flags.Where(f => !string.IsNullOrEmpty(f))
             .Select(Encoding.UTF8.GetBytes)
             .ToArray();

    private static string NormalizeIp(IPAddress addr) =>
        addr.IsIPv4MappedToIPv6 ? addr.MapToIPv4().ToString() : addr.ToString();

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best effort */ }
    }

    private sealed class FlowAccumulator(int connectionPort, string peerIp)
    {
        public int ConnectionPort { get; } = connectionPort;
        public string PeerIp { get; } = peerIp;
        public DateTimeOffset FirstSeenUtc { get; private set; } = DateTimeOffset.MaxValue;
        public DateTimeOffset LastSeenUtc { get; private set; } = DateTimeOffset.MinValue;
        public int PacketsIn { get; private set; }
        public int PacketsOut { get; private set; }
        public long BytesIn { get; private set; }
        public long BytesOut { get; private set; }
        public List<TrafficFlowChunkBuffer> Chunks { get; } = [];

        public void Add(TrafficFlowDirection direction, byte[] payload, DateTimeOffset timestamp, bool retainPayload)
        {
            if (timestamp < FirstSeenUtc) FirstSeenUtc = timestamp;
            if (timestamp > LastSeenUtc) LastSeenUtc = timestamp;

            if (direction == TrafficFlowDirection.ContainerToTeam)
            {
                PacketsIn++;
                BytesIn += payload.Length;
            }
            else
            {
                PacketsOut++;
                BytesOut += payload.Length;
            }

            if (retainPayload && payload.Length > 0)
                Chunks.Add(new TrafficFlowChunkBuffer(direction, timestamp, payload));
        }

        public TrafficFlowSummary ToSummary(byte[][] flagBytes)
        {
            return new TrafficFlowSummary
            {
                ConnectionPort = ConnectionPort,
                FirstSeenUtc = FirstSeenUtc == DateTimeOffset.MaxValue ? DateTimeOffset.MinValue : FirstSeenUtc,
                LastSeenUtc = LastSeenUtc == DateTimeOffset.MinValue ? DateTimeOffset.MinValue : LastSeenUtc,
                PeerIp = PeerIp,
                PacketsIn = PacketsIn,
                PacketsOut = PacketsOut,
                BytesIn = BytesIn,
                BytesOut = BytesOut,
                FlagHits = CountFlagHits(flagBytes)
            };
        }

        public TrafficFlowDetail ToDetail(byte[][] flagBytes)
        {
            var detail = new TrafficFlowDetail
            {
                ConnectionPort = ConnectionPort,
                FirstSeenUtc = FirstSeenUtc == DateTimeOffset.MaxValue ? DateTimeOffset.MinValue : FirstSeenUtc,
                LastSeenUtc = LastSeenUtc == DateTimeOffset.MinValue ? DateTimeOffset.MinValue : LastSeenUtc,
                PeerIp = PeerIp,
                PacketsIn = PacketsIn,
                PacketsOut = PacketsOut,
                BytesIn = BytesIn,
                BytesOut = BytesOut,
                FlagHits = CountFlagHits(flagBytes),
                Chunks = Chunks.Select(c => new TrafficFlowChunk
                {
                    Direction = c.Direction,
                    TimestampUtc = c.Timestamp,
                    PayloadBase64 = Convert.ToBase64String(c.Payload),
                    FlagOffsets = FindOffsets(c.Payload, flagBytes)
                }).ToArray()
            };
            return detail;
        }

        public string RenderAscii()
        {
            var sb = new StringBuilder();
            foreach (var c in Chunks)
            {
                foreach (var b in c.Payload)
                    sb.Append(b is >= 0x20 and < 0x7f ? (char)b : '.');
            }
            return sb.ToString();
        }

        private int CountFlagHits(byte[][] flagBytes)
        {
            if (flagBytes.Length == 0 || Chunks.Count == 0) return 0;

            var hits = 0;
            foreach (var chunk in Chunks)
            {
                var span = (ReadOnlySpan<byte>)chunk.Payload;
                foreach (var flag in flagBytes)
                {
                    if (flag.Length == 0) continue;
                    var idx = 0;
                    while (idx <= span.Length - flag.Length)
                    {
                        var pos = span[idx..].IndexOf(flag);
                        if (pos < 0) break;
                        hits++;
                        idx += pos + flag.Length;
                    }
                }
            }
            return hits;
        }

        private static int[] FindOffsets(byte[] payload, byte[][] flagBytes)
        {
            if (flagBytes.Length == 0 || payload.Length == 0) return [];

            var offsets = new List<int>();
            var span = (ReadOnlySpan<byte>)payload;
            foreach (var flag in flagBytes)
            {
                if (flag.Length == 0) continue;
                var idx = 0;
                while (idx <= span.Length - flag.Length)
                {
                    var pos = span[idx..].IndexOf(flag);
                    if (pos < 0) break;
                    offsets.Add(idx + pos);
                    idx += pos + flag.Length;
                }
            }
            offsets.Sort();
            return offsets.ToArray();
        }
    }

    private sealed record TrafficFlowChunkBuffer(
        TrafficFlowDirection Direction,
        DateTimeOffset Timestamp,
        byte[] Payload);
}
