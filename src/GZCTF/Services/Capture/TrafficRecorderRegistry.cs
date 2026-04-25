using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using GZCTF.Storage.Interface;

namespace GZCTF.Services.Capture;

/// <summary>
/// Global singleton registry that manages per-container TrafficRecorder instances.
/// Ensures that concurrent connections to the same container share a single pcap writer.
///
/// Owns a root CancellationTokenSource that propagates to all child recorders.
/// On disposal (application shutdown), the root token is cancelled, which signals all
/// writer loops to stop. Each recorder's DisposeAsync then waits with a bounded timeout.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Network stream recording requires platform proxy support")]
public sealed class TrafficRecorderRegistry(
    IBlobStorage storage,
    ILogger<TrafficRecorderRegistry> logger) : IAsyncDisposable
{
    static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromSeconds(30);
    static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(15);

    readonly CancellationTokenSource _rootCts = new();
    readonly ConcurrentDictionary<Guid, TrafficRecorder> _recorders = new();
    bool _disposed;

    /// <summary>
    /// Get or create a TrafficRecorder for the specified container.
    /// If a recorder already exists, it is returned directly; the container is only
    /// read when a new recorder needs to be created.
    /// </summary>
    public TrafficRecorder GetOrCreate(Models.Data.Container container, JsonSerializerOptions jsonOptions) =>
        _recorders.GetOrAdd(container.Id, _ => new TrafficRecorder(
            container.Id,
            container.TrafficDir(),
            container.ShortId,
            container.GenerateMetadata(jsonOptions),
            storage,
            logger,
            DefaultIdleTimeout,
            _rootCts.Token
        ));

    /// <summary>
    /// Remove and dispose the TrafficRecorder for the specified container.
    /// Called when a container is destroyed to ensure all captured traffic is flushed.
    /// </summary>
    public async ValueTask RemoveAsync(Guid containerId)
    {
        if (!_recorders.TryRemove(containerId, out var recorder))
            return;

        await recorder.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await _rootCts.CancelAsync();

        var recorders = _recorders.ToArray();
        _recorders.Clear();

        if (recorders.Length == 0)
        {
            _rootCts.Dispose();
            return;
        }

        var disposeTasks = recorders.Select(async kv =>
        {
            try
            {
                await kv.Value.DisposeAsync();
            }
            catch
            {
                // Individual recorder errors are logged inside DisposeAsync
            }
        });

        try
        {
            await Task.WhenAll(disposeTasks).WaitAsync(ShutdownTimeout);
        }
        catch (TimeoutException)
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.TrafficRecorder_ShutdownTimeout), recorders.Length],
                TaskStatus.Failed, LogLevel.Warning);
        }

        _rootCts.Dispose();
    }
}
