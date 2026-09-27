using System.Collections.Concurrent;

namespace GZCTF.Features.Dashboard.Application;

public sealed class DashboardRequestLimiter
{
    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _requests = new();
    private readonly int _limit;
    private readonly TimeSpan _window;
    private readonly int _cleanupInterval;
    private long _requestCount;

    public DashboardRequestLimiter(
        int limit = 120,
        TimeSpan? window = null,
        int cleanupInterval = 128)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cleanupInterval);
        _limit = limit;
        _window = window ?? TimeSpan.FromMinutes(1);
        if (_window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window));
        _cleanupInterval = cleanupInterval;
    }

    internal int EntryCount => _requests.Count;

    internal bool Allow(string key, DateTimeOffset now)
    {
        if (Interlocked.Increment(ref _requestCount) % _cleanupInterval == 0)
            Cleanup(now);

        var queue = _requests.GetOrAdd(key, _ => new Queue<DateTimeOffset>());
        lock (queue)
        {
            RemoveExpired(queue, now);
            if (queue.Count >= _limit)
                return false;

            queue.Enqueue(now);
            return true;
        }
    }

    private void Cleanup(DateTimeOffset now)
    {
        foreach (var pair in _requests)
        {
            lock (pair.Value)
            {
                RemoveExpired(pair.Value, now);
                if (pair.Value.Count == 0)
                    ((ICollection<KeyValuePair<string, Queue<DateTimeOffset>>>)_requests).Remove(pair);
            }
        }
    }

    private void RemoveExpired(Queue<DateTimeOffset> queue, DateTimeOffset now)
    {
        while (queue.TryPeek(out var timestamp) && now - timestamp > _window)
            queue.Dequeue();
    }
}
