using System.Reflection;
using Cronos;
using GZCTF.Services.Cache;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Services.CronJob;

public delegate Task CronJob(AsyncServiceScope scope, ILogger<CronJobService> logger);

public record CronJobEntry(CronJob Job, CronExpression Expression);

public class CronJobService(IDistributedCache cache, IServiceScopeFactory provider, ILogger<CronJobService> logger)
    : IHostedService, IDisposable
{
    readonly Dictionary<string, CronJobEntry> _jobs = [];
    bool _disposed;
    bool _holdLock;
    Timer? _timer;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task StartAsync(CancellationToken token)
    {
        if (await TryHoldLock())
            LaunchCronJob();
        else
            LaunchWatchDog();
    }

    public Task StopAsync(CancellationToken token)
    {
        StopCronJob();
        return DropLock();
    }

    ~CronJobService()
    {
        Dispose();
    }

    /// <summary>
    /// Add a job to the cron job service
    /// </summary>
    public bool AddJob(CronJob job)
    {
        lock (_jobs)
        {
            (var name, var entry) = job.ToEntry();
            if (!_jobs.TryAdd(name, entry))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Remove a job from the cron job service
    /// </summary>
    /// <param name="job"></param>
    public bool RemoveJob(string job)
    {
        lock (_jobs)
        {
            if (!_jobs.Remove(job))
                return false;
        }

        return true;
    }

    void LaunchCronJob()
    {
        var methods = typeof(RuntimeCronJobs).GetMethods(BindingFlags.Static | BindingFlags.Public);
        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<CronJobAttribute>();
            if (attr is null)
                continue;

            AddJob(method.CreateDelegate<CronJob>());
        }

        _timer = new Timer(_ => Task.Run(Execute),
            null, TimeSpan.FromSeconds(60 - DateTime.UtcNow.Second), TimeSpan.FromMinutes(1));

        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.CronJob_Started)],
            TaskStatus.Success, LogLevel.Debug);
    }

    void StopCronJob()
    {
        _timer?.Change(Timeout.Infinite, 0);
        lock (_jobs)
        {
            _jobs.Clear();
        }

        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.CronJob_Stopped)], TaskStatus.Exit,
            LogLevel.Debug);
    }

    async Task<bool> TryHoldLock()
    {
        if (_holdLock)
            return true;

        var cronLock = await cache.GetAsync(CacheKey.CronJobLock);
        if (cronLock is not null)
            return false;

        await cache.SetAsync(CacheKey.CronJobLock, [],
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(2) });
        _holdLock = true;
        return true;
    }

    async Task DropLock()
    {
        if (!_holdLock)
            return;

        await cache.RemoveAsync(CacheKey.CronJobLock);
        _holdLock = false;
    }

    void LaunchWatchDog()
    {
        var delay = Random.Shared.Next(30, 120);

        _timer = new Timer(async void (_) =>
        {
            try
            {
                if (!await TryHoldLock())
                    return;

                _timer?.Change(Timeout.Infinite, 0);
                LaunchCronJob();
                logger.SystemLog(StaticLocalizer[nameof(Resources.Program.CronJob_Started)],
                    TaskStatus.Success, LogLevel.Debug);
            }
            catch (Exception e)
            {
                logger.SystemLog(StaticLocalizer[nameof(Resources.Program.CronJob_ExecuteFailed),
                        "WatchDog", e.Message],
                    TaskStatus.Failed, LogLevel.Warning);
            }
        }, null, TimeSpan.FromSeconds(delay), TimeSpan.FromMinutes(5));

        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.CronJob_LaunchedWatchDog)],
            TaskStatus.Pending, LogLevel.Debug);
    }

    async Task Execute()
    {
        var now = DateTime.UtcNow;
        var last = now - TimeSpan.FromSeconds(30);
        List<Task> handles = [];

        await cache.RefreshAsync(CacheKey.CronJobLock);

        lock (_jobs)
        {
            foreach ((var job, var entry) in _jobs)
            {
                if (entry.Expression.GetNextOccurrence(last) is not { } next ||
                    Math.Abs((next - now).TotalSeconds) > 30D)
                    continue;

                handles.Add(Task.Run(async () =>
                {
                    await using var scope = provider.CreateAsyncScope();

                    try
                    {
                        await entry.Job(scope, logger);
                    }
                    catch (Exception e)
                    {
                        logger.SystemLog(
                            StaticLocalizer[nameof(Resources.Program.CronJob_ExecuteFailed), job, e.Message],
                            TaskStatus.Failed, LogLevel.Warning);
                    }
                }));
            }
        }

        await Task.WhenAll(handles);
    }
}
