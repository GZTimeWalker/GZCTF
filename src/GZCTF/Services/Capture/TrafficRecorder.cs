using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Channels;
using GZCTF.Storage.Interface;
using PacketDotNet;
using PacketDotNet.Utils;
using SharpPcap;
using SharpPcap.LibPcap;

namespace GZCTF.Services.Capture;

/// <summary>
/// Represents a command sent through the capture channel.
/// Either a data packet to write, or a flush signal.
/// </summary>
readonly struct CaptureCommand
{
    public CapturePacket? Packet { get; init; }
    public bool IsFlush { get; init; }

    public static CaptureCommand Data(CapturePacket packet) => new() { Packet = packet };
    public static CaptureCommand Flush() => new() { IsFlush = true };
}

/// <summary>
/// A per-container traffic recorder that multiplexes concurrent connections into shared pcap files.
/// Uses a Channel-based pipeline with a single writer task to ensure thread-safe pcap writes.
/// Implements idle-timeout based file rotation: when all connections disconnect and no new
/// connection arrives within the idle timeout, the current pcap file is flushed and persisted.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Network stream recording requires platform proxy support")]
public sealed class TrafficRecorder : IAsyncDisposable
{
    /// <summary>
    /// Maximum time to wait for the writer task to finish during disposal.
    /// If exceeded, the writer task is abandoned and resources are cleaned up best-effort.
    /// </summary>
    static readonly TimeSpan DisposeTimeout = TimeSpan.FromSeconds(10);

    static readonly PhysicalAddress DummyPhysicalAddress = PhysicalAddress.Parse("00-11-00-11-00-11");
    
    readonly string _shortId;
    readonly string _directory;
    readonly string _filePrefix;
    readonly byte[]? _metadata;
    readonly IBlobStorage _storage;
    readonly ILogger<TrafficRecorderRegistry> _logger;
    readonly TimeSpan _idleTimeout;
    readonly long _maxFileSize;

    readonly Channel<CaptureCommand> _channel;
    readonly CancellationTokenSource _cts;
    readonly Task _writerTask;

    int _activeConnections;
    int _fileSequence;
    readonly Timer _idleTimer;

    // Only accessed by the writer task (single reader)
    CaptureFileWriterDevice? _device;
    string _tempFile = string.Empty;
    bool _hasRecord;
    long _currentFileSize;
    DateTimeOffset _windowStart;

    bool _disposed;
    
    /// <summary>
    /// Creates a new TrafficRecorder for a container.
    /// </summary>
    public TrafficRecorder(
        Guid containerId,
        string directory,
        string filePrefix,
        byte[]? metadata,
        IBlobStorage storage,
        ILogger<TrafficRecorderRegistry> logger,
        TimeSpan idleTimeout,
        CancellationToken registryToken,
        long maxFileSize = 50 * 1024 * 1024)
    {
        _shortId = containerId.ToString("N")[..12];
        _directory = directory;
        _filePrefix = filePrefix;
        _metadata = metadata;
        _storage = storage;
        _logger = logger;
        _idleTimeout = idleTimeout;
        _maxFileSize = maxFileSize;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(registryToken);
        _idleTimer = new Timer(OnIdleTimeout, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        _channel = Channel.CreateUnbounded<CaptureCommand>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _writerTask = Task.Run(WriterLoopAsync);
    }

    /// <summary>
    /// Write a captured packet. Called from any connection's read/write path.
    /// </summary>
    public void WritePacket(CapturePacket packet) => _channel.Writer.TryWrite(CaptureCommand.Data(packet));

    /// <summary>
    /// Register a new connection to this recorder.
    /// Cancels any pending idle timer and increments the active connection count.
    /// </summary>
    public void RegisterConnection()
    {
        Interlocked.Increment(ref _activeConnections);
        CancelIdleTimer();
    }

    /// <summary>
    /// Unregister a connection from this recorder.
    /// When the last connection disconnects, starts the idle timer.
    /// </summary>
    public void UnregisterConnection()
    {
        var remaining = Interlocked.Decrement(ref _activeConnections);
        if (remaining <= 0)
            StartIdleTimer();
    }

    /// <summary>
    /// The single-threaded writer loop that consumes commands from the channel.
    /// </summary>
    async Task WriterLoopAsync()
    {
        var token = _cts.Token;

        try
        {
            await foreach (var command in _channel.Reader.ReadAllAsync(token))
            {
                if (command.IsFlush)
                {
                    await FlushAndPersistAsync(token);
                    continue;
                }

                if (command.Packet is not { } packet)
                    continue;

                EnsureDeviceOpen();
                WritePcapPacket(packet);

                if (_currentFileSize > _maxFileSize)
                    await FlushAndPersistAsync(token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during disposal or registry shutdown
        }
        catch (Exception ex)
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.TrafficRecorder_WriterLoopError), _shortId],
                TaskStatus.Failed, LogLevel.Error);
            _logger.LogErrorMessage(ex);
        }
        finally
        {
            // Best-effort final flush, bounded by DisposeTimeout in DisposeAsync
            await FlushAndPersistAsync(CancellationToken.None);
        }
    }

    void EnsureDeviceOpen()
    {
        if (_device is not null)
            return;

        _windowStart = DateTimeOffset.UtcNow;
        _tempFile = Path.GetTempFileName();
        _device = new CaptureFileWriterDevice(_tempFile, FileMode.Open);
        _device.Open();
        _hasRecord = false;
        _currentFileSize = 0;

        if (_metadata is null)
            return;

        var host = new IPEndPoint(IPAddress.Any.MapToIPv6(), 65535);
        var dest = new IPEndPoint(IPAddress.Any.MapToIPv6(), 0);
        WritePcapPacketCore(host, dest, _metadata);
    }

    void WritePcapPacket(CapturePacket packet) =>
        WritePcapPacketCore(packet.Source, packet.Dest, packet.Data, packet.Timestamp);

    void WritePcapPacketCore(IPEndPoint source, IPEndPoint dest, byte[] data,
        DateTimeOffset? timestamp = null)
    {
        var udp = new UdpPacket((ushort)source.Port, (ushort)dest.Port)
        {
            PayloadDataSegment = new ByteArraySegment(data)
        };

        var packet = new EthernetPacket(DummyPhysicalAddress, DummyPhysicalAddress, EthernetType.IPv6)
        {
            PayloadPacket = new IPv6Packet(source.Address, dest.Address) { PayloadPacket = udp }
        };

        udp.UpdateUdpChecksum();

        var rawBytes = packet.Bytes;
        var time = new PosixTimeval(timestamp?.UtcDateTime ?? DateTime.UtcNow);
        _device?.Write(new RawCapture(LinkLayers.Ethernet, time, rawBytes));
        _currentFileSize += rawBytes.Length;
        _hasRecord = true;
    }

    /// <summary>
    /// Flush and persist the current pcap file to blob storage.
    /// Only called from the writer task, ensuring thread safety.
    /// </summary>
    async Task FlushAndPersistAsync(CancellationToken token)
    {
        if (_device is null)
            return;

        var device = _device;
        var tempFile = _tempFile;
        var hasRecord = _hasRecord;
        var blobPath = CurrentBlobPath();

        _device = null;
        _tempFile = string.Empty;
        _hasRecord = false;
        _currentFileSize = 0;

        try
        {
            device.Close();
            device.Dispose();

            if (hasRecord)
            {
                await _storage.WriteFileAsync(blobPath, tempFile, token);
                _logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.TrafficRecorder_Persisted), _shortId, blobPath],
                    TaskStatus.Success, LogLevel.Debug);
            }
        }
        catch (OperationCanceledException)
        {
            // Persist canceled — logged at Warning since data may be lost
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.TrafficRecorder_PersistFailed), _shortId, blobPath],
                TaskStatus.Failed, LogLevel.Warning);
        }
        catch (Exception ex)
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.TrafficRecorder_PersistFailed), _shortId, blobPath],
                TaskStatus.Failed, LogLevel.Error);
            _logger.LogErrorMessage(ex);
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            catch
            {
                // best effort
            }

            _fileSequence++;
        }
    }

    string CurrentBlobPath()
    {
        var timestamp = _windowStart.ToString("yyyyMMdd-HHmmss");
        var suffix = _fileSequence > 0 ? $"-{_fileSequence}" : "";
        return StoragePath.Combine(_directory, $"{_filePrefix}-{timestamp}{suffix}.pcap");
    }

    void StartIdleTimer() => _idleTimer.Change(_idleTimeout, Timeout.InfiniteTimeSpan);

    void CancelIdleTimer() => _idleTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

    void OnIdleTimeout(object? state)
    {
        if (Volatile.Read(ref _activeConnections) > 0)
            return;

        _channel.Writer.TryWrite(CaptureCommand.Flush());
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _channel.Writer.TryComplete();
        await _idleTimer.DisposeAsync();
        await _cts.CancelAsync();

        try
        {
            await _writerTask.WaitAsync(DisposeTimeout);
        }
        catch (TimeoutException)
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.TrafficRecorder_DisposeTimeout), _shortId],
                TaskStatus.Failed, LogLevel.Warning);
        }
        catch
        {
            // Swallow — we're disposing, nothing useful to do with the error
        }

        _cts.Dispose();
    }
}
