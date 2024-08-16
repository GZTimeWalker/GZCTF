namespace GZCTF.Utils;

/// <summary>
/// Asynchronous manual reset event
/// This class is similar to <see cref="ManualResetEvent" /> but asynchronous and non-blocking
/// </summary>
public sealed class AsyncManualResetEvent
{
    volatile TaskCompletionSource<bool> _tcs = new();

    /// <summary>
    /// Wait for the event to be signaled
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<bool> tcs = _tcs;
        var cancelTcs = new TaskCompletionSource<bool>();

        cancellationToken.Register(
            s => ((TaskCompletionSource<bool>)s!).TrySetCanceled(), cancelTcs);

        await await Task.WhenAny(tcs.Task, cancelTcs.Task);
    }

    static async Task<bool> Delay(int milliseconds)
    {
        await Task.Delay(milliseconds);
        return false;
    }

    /// <summary>
    /// Wait for the event to be signaled
    /// </summary>
    /// <param name="milliseconds">Timeout</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Returns false if timeout</returns>
    public async Task<bool> WaitAsync(int milliseconds, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<bool> tcs = _tcs;
        var cancelTcs = new TaskCompletionSource<bool>();

        cancellationToken.Register(
            s => ((TaskCompletionSource<bool>)s!).TrySetCanceled(), cancelTcs);

        return await await Task.WhenAny(tcs.Task, cancelTcs.Task, Delay(milliseconds));
    }

    /// <summary>
    /// Set the event to signaled
    /// </summary>
    public void Set()
    {
        TaskCompletionSource<bool> tcs = _tcs;
        Task.Factory.StartNew(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true),
            tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
        tcs.Task.Wait();
    }

    /// <summary>
    /// Reset the event to non-signaled
    /// </summary>
    public void Reset()
    {
        var newTcs = new TaskCompletionSource<bool>();
        while (true)
        {
            TaskCompletionSource<bool> tcs = _tcs;
            if (!tcs.Task.IsCompleted ||
                Interlocked.CompareExchange(ref _tcs, newTcs, tcs) == tcs)
                return;
        }
    }
}
