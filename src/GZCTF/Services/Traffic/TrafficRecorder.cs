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
/// Thread-safety:
///   _refCount: reference count (≥ 0) for active writers, or -1 sentinel (archiving).
///   Transitions to -1 are atomic: OnIdleTimeout uses CAS(0→-1), ArchiveAsync uses Exchange(→-1).
///   Once _refCount &lt; 0, TryAcquire rejects all new connections.
///
/// Best-effort semantics:
///   Enqueue uses TryWrite on a bounded channel; packets are silently dropped when the
///   channel is full, or when the channel has already been completed (e.g. during the
///   brief window between TryComplete and the last writer releasing).
/// </summary>
internal sealed class TrafficRecorder : IAsyncDisposable
{
    static readonly PhysicalAddress DummyMac = PhysicalAddress.Parse("00-11-00-11-00-11");
    static readonly IPEndPoint MetadataHost = new(IPAddress.IPv6Any, 65535);
    static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);

    const int MetaPort = 10000;
    const int PortMin = MetaPort + 1;
    const int PortMax = 65000;
    const int PortTotal = PortMax - PortMin + 1;

    readonly Channel<TrafficPacket> _channel = Channel.CreateBounded<TrafficPacket>(
        new BoundedChannelOptions(1024) { SingleWriter = false, SingleReader = true });

    readonly IBlobStorage _storage;
    readonly ILogger _logger;

    public Guid RegistryKey { get; }
    public string BlobPath { get; }

    readonly string _tempFile;
    readonly CaptureFileWriterDevice _device;

    readonly Task _writeLoop;
    readonly Lock _archiveLock = new();
    Task? _archiveTask;

    int _refCount;
    int _disposed;
    uint _connectionCounter;
    bool _hasRecords;
    readonly Timer _idleTimer;

    readonly Action<Guid, TrafficRecorder> _onArchived;

    internal TrafficRecorder(
        Guid registryKey,
        string blobPath,
        byte[]? metadata,
        IPAddress remoteAddress,
        IBlobStorage storage,
        ILogger logger,
        Action<Guid, TrafficRecorder> onArchived)
    {
        RegistryKey = registryKey;
        BlobPath = blobPath;
        _storage = storage;
        _logger = logger;
        _onArchived = onArchived;

        _idleTimer = new Timer(OnIdleTimeout, null, Timeout.Infinite, Timeout.Infinite);

        _tempFile = Path.GetTempFileName();
        _device = new CaptureFileWriterDevice(_tempFile, FileMode.Open);
        _device.Open();

        if (metadata is not null)
        {
            var endPoint = new IPEndPoint(remoteAddress, PortMin);
            WritePcapPacket(new(MetadataHost, endPoint, metadata, DateTimeOffset.UtcNow));
        }

        _refCount = 0;
        _writeLoop = Task.Run(WriteLoopAsync);
    }

    internal int TryAcquire()
    {
        while (true)
        {
            var current = Volatile.Read(ref _refCount);
            if (current < 0)
                return 0;

            if (Interlocked.CompareExchange(ref _refCount, current + 1, current) == current)
            {
                _idleTimer.Change(Timeout.Infinite, Timeout.Infinite);
                var raw = Interlocked.Increment(ref _connectionCounter);
                return (int)(raw % PortTotal) + PortMin;
            }
        }
    }

    internal void Enqueue(TrafficPacket packet) => _channel.Writer.TryWrite(packet);

    void StartIdleTimer() => _idleTimer.Change(IdleTimeout, Timeout.InfiniteTimeSpan);

    void OnIdleTimeout(object? state)
    {
        // Atomically try to transition from 0 to -1.
        // If refCount changed (e.g. TryAcquire raced in), CAS fails and we bail.
        if (Interlocked.CompareExchange(ref _refCount, -1, 0) != 0)
            return;

        _ = StartArchiveAsync();
    }

    /// <summary>
    /// Force-archive regardless of active connection count.
    /// Called by container destroy and server shutdown.
    /// </summary>
    internal async ValueTask ArchiveAsync()
    {
        Interlocked.Exchange(ref _refCount, -1);
        await StartArchiveAsync();
    }

    Task StartArchiveAsync()
    {
        lock (_archiveLock)
            return _archiveTask ??= RunArchiveAsync();
    }

    /// <summary>
    /// Core archive logic. Assumes _refCount is already -1 (set by caller).
    /// </summary>
    async Task RunArchiveAsync()
    {
        _idleTimer.Change(Timeout.Infinite, Timeout.Infinite);

        _channel.Writer.TryComplete();

        // Remove from registry immediately so new connections
        // can create a fresh recorder without spinning.
        _onArchived(RegistryKey, this);

        try
        {
            await _writeLoop;
        }
        finally
        {
            _device.Close();
            _device.Dispose();
        }

        await FlushAndUploadAsync();

        try
        {
            File.Delete(_tempFile);
        }
        catch
        {
            /* best effort */
        }

        await _idleTimer.DisposeAsync();
    }

    async Task WriteLoopAsync()
    {
        try
        {
            await foreach (var packet in _channel.Reader.ReadAllAsync())
                WritePcapPacket(packet);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "TrafficRecorder WriteLoop error for {Key}", RegistryKey);
        }
    }

    void WritePcapPacket(TrafficPacket packet)
    {
        var udp = new UdpPacket((ushort)packet.Source.Port, (ushort)packet.Dest.Port)
        {
            PayloadDataSegment = new ByteArraySegment(packet.Data)
        };

        var eth = new EthernetPacket(DummyMac, DummyMac, EthernetType.IPv6)
        {
            PayloadPacket = new IPv6Packet(packet.Source.Address, packet.Dest.Address) { PayloadPacket = udp }
        };

        udp.UpdateUdpChecksum();

        var timeval = new PosixTimeval(packet.Timestamp.UtcDateTime);
        _device.Write(new RawCapture(LinkLayers.Ethernet, timeval, eth.Bytes));

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

    internal void Release()
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
            StartIdleTimer();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await ArchiveAsync();
    }
}
