using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Channels;
using GZCTF.Storage.Interface;
using PacketDotNet;
using PacketDotNet.Utils;
using SharpPcap;
using SharpPcap.LibPcap;

namespace GZCTF.Services.Traffic;

/// <summary>
/// Records traffic for a single container on this GZCTF instance into
/// one pcap file. Multiplexes concurrent connections via IP:Port in packets.
///
/// Lifecycle:
///   Created → Active (ref &gt; 0) → Idle (ref == 0, timer started)
///            ↗ new Acquire cancels timer   ↓ 30s timeout
///                                        Archiving → Archived
///
/// Once Channel is completed (archiving), no new packets can be enqueued.
/// The registry detects this state and creates a new Recorder for subsequent connections.
/// </summary>
internal sealed class TrafficRecorder : IAsyncDisposable
{
    static readonly PhysicalAddress DummyMac = PhysicalAddress.Parse("00-11-00-11-00-11");
    static readonly IPEndPoint MetadataHost = new(IPAddress.Any, 65535);
    static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);

    readonly Channel<TrafficPacket> _channel = Channel.CreateUnbounded<TrafficPacket>(
        new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });

    readonly IBlobStorage _storage;
    readonly ILogger _logger;

    public Guid RegistryKey { get; }
    public string BlobPath { get; }

    readonly string _tempFile;
    readonly CaptureFileWriterDevice _device;

    readonly Task _writeLoop;

    int _refCount;
    bool _hasRecords;
    bool _disposed;
    Timer? _idleTimer;

    readonly Action<Guid, TrafficRecorder> _onArchived;

    internal TrafficRecorder(
        Guid registryKey,
        string blobPath,
        byte[]? metadata,
        IPEndPoint firstClient,
        IBlobStorage storage,
        ILogger logger,
        Action<Guid, TrafficRecorder> onArchived)
    {
        RegistryKey = registryKey;
        BlobPath = blobPath;
        _storage = storage;
        _logger = logger;
        _onArchived = onArchived;

        _tempFile = Path.GetTempFileName();
        _device = new CaptureFileWriterDevice(_tempFile, FileMode.Open);
        _device.Open();

        if (metadata is not null)
            WritePcapPacket(MetadataHost, firstClient, metadata);

        _refCount = 1;
        _writeLoop = Task.Factory.StartNew(
            WriteLoopAsync, TaskCreationOptions.LongRunning).Unwrap();
    }

    public bool IsCompleted => _channel.Reader.Completion.IsCompleted;

    public bool IsFullyDrained =>
        _channel.Reader.Completion.IsCompleted && !_channel.Reader.TryPeek(out _);

    internal bool TryAcquire()
    {
        if (_channel.Reader.Completion.IsCompleted)
            return false;

        while (true)
        {
            var current = Volatile.Read(ref _refCount);
            if (current < 0)
                return false;

            if (Interlocked.CompareExchange(ref _refCount, current + 1, current) == current)
            {
                _idleTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                return true;
            }
        }
    }

    internal void Enqueue(TrafficPacket packet) =>
        _channel.Writer.TryWrite(packet);

    internal ValueTask ReleaseAsync()
    {
        var newCount = Interlocked.Decrement(ref _refCount);
        if (newCount == 0)
            StartIdleTimer();
        return ValueTask.CompletedTask;
    }

    void StartIdleTimer()
    {
        _idleTimer ??= new Timer(OnIdleTimeout, null, Timeout.Infinite, Timeout.Infinite);
        _idleTimer.Change(IdleTimeout, Timeout.InfiniteTimeSpan);
    }

    void OnIdleTimeout(object? state)
    {
        if (Volatile.Read(ref _refCount) == 0)
            _ = ArchiveAsync();
    }

    internal async ValueTask ArchiveAsync()
    {
        var prev = Interlocked.Exchange(ref _refCount, -1);
        if (prev < 0)
            return;

        _idleTimer?.Change(Timeout.Infinite, Timeout.Infinite);

        _channel.Writer.TryComplete();

        // Remove from registry immediately so new connections
        // can create a fresh recorder without spinning.
        _onArchived(RegistryKey, this);

        try
        {
            await _writeLoop;
            await FlushAndUploadAsync();
        }
        finally
        {
            _device.Close();
            _device.Dispose();
            try { File.Delete(_tempFile); } catch { /* best effort */ }
            _idleTimer?.Dispose();
        }
    }

    async Task WriteLoopAsync()
    {
        try
        {
            await foreach (var packet in _channel.Reader.ReadAllAsync())
                WritePcapPacket(packet.Source, packet.Dest, packet.Data);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "TrafficRecorder WriteLoop error for {Key}", RegistryKey);
        }
    }

    void WritePcapPacket(IPEndPoint source, IPEndPoint dest, byte[] data)
    {
        var udp = new UdpPacket((ushort)source.Port, (ushort)dest.Port)
        {
            PayloadDataSegment = new ByteArraySegment(data)
        };

        var packet = new EthernetPacket(DummyMac, DummyMac, EthernetType.IPv6)
        {
            PayloadPacket = new IPv6Packet(
                source.Address.MapToIPv6(),
                dest.Address.MapToIPv6()) { PayloadPacket = udp }
        };

        udp.UpdateUdpChecksum();

        _device.Write(new RawCapture(LinkLayers.Ethernet, new(), packet.Bytes));

        _hasRecords = true;
    }

    async Task FlushAndUploadAsync()
    {
        if (_hasRecords)
        {
            try
            {
                await _storage.WriteFileAsync(BlobPath, _tempFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload pcap to {Path}", BlobPath);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await ArchiveAsync();
    }
}
