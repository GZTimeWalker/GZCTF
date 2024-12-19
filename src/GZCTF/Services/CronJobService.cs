using System.Threading.Channels;
using Cronos;
using GZCTF.Repositories;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Services;

public delegate Task CronJob(AsyncServiceScope scope, ILogger<CronJobService> logger);

public record CronJobEntry(CronJob Job, CronExpression Expression)
{
    /// <summary>
    /// Create a cron job entry
    /// </summary>
    /// <param name="job"></param>
    /// <param name="expression"></param>
    /// <returns></returns>
    public static CronJobEntry Create(CronJob job, string expression) =>
        new(job, CronExpression.Parse(expression));
}

public class CronJobService(IDistributedCache cache, IServiceScopeFactory provider, ILogger<CronJobService> logger)
    : IHostedService, IDisposable
{
    Timer? _timer;
    bool _holdLock;
    readonly Dictionary<string, CronJobEntry> _jobs = [];

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Add a job to the cron job service
    /// </summary>
    public bool AddJob(string job, CronJobEntry task)
    {
        lock (_jobs)
        {
            if (!_jobs.TryAdd(job, task))
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
        // container checker, every 3min
        AddJob(nameof(CronJobs.ContainerChecker),
            CronJobEntry.Create(CronJobs.ContainerChecker, "* * * * *"));

        // bootstrap cache, every 10min
        AddJob(nameof(CronJobs.BootstrapCache),
            CronJobEntry.Create(CronJobs.BootstrapCache, "*/10 * * * *"));

        _timer = new Timer(_ => Task.Run(Execute),
            null, TimeSpan.FromSeconds(60 - DateTime.UtcNow.Second), TimeSpan.FromMinutes(1));

        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.CronJob_Started)],
            TaskStatus.Success, LogLevel.Debug);
    }

    void StopCronJob()
    {
        _timer?.Change(Timeout.Infinite, 0);
        lock (_jobs)
        {
            _jobs.Clear();
        }

        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.CronJob_Stopped)], TaskStatus.Exit,
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
                logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.CronJob_Started)],
                    TaskStatus.Success, LogLevel.Debug);
            }
            catch (Exception e)
            {
                logger.SystemLog(Program.StaticLocalizer[
                        nameof(Resources.Program.CronJob_ExecuteFailed),
                        "WatchDog", e.Message],
                    TaskStatus.Failed, LogLevel.Warning);
            }
        }, null, TimeSpan.FromSeconds(delay), TimeSpan.FromMinutes(5));

        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.CronJob_LaunchedWatchDog)],
            TaskStatus.Pending, LogLevel.Debug);
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

    async Task Execute()
    {
        var now = DateTime.UtcNow;
        var last = now - TimeSpan.FromSeconds(30);
        List<Task> handles = [];

        await cache.RefreshAsync(CacheKey.CronJobLock);

        lock (_jobs)
        {
            foreach ((var job, CronJobEntry entry) in _jobs)
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
                            Program.StaticLocalizer[nameof(Resources.Program.CronJob_ExecuteFailed), job, e.Message],
                            TaskStatus.Failed, LogLevel.Warning);
                    }
                }));
            }
        }

        await Task.WhenAll(handles);
    }
}

public static class CronJobs
{
    public static async Task ContainerChecker(AsyncServiceScope scope, ILogger<CronJobService> logger)
    {
        var containerRepo = scope.ServiceProvider.GetRequiredService<IContainerRepository>();

        foreach (Models.Data.Container container in await containerRepo.GetDyingContainers())
        {
            await containerRepo.DestroyContainer(container);
            logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.CronJob_RemoveExpiredContainer),
                    container.ContainerId],
                TaskStatus.Success, LogLevel.Debug);
        }
    }

    public static async Task BootstrapCache(AsyncServiceScope scope, ILogger<CronJobService> logger)
    {
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var upcoming = await gameRepo.GetUpcomingGames();

        if (upcoming.Length <= 0)
            return;

        var channelWriter = scope.ServiceProvider.GetRequiredService<ChannelWriter<CacheRequest>>();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

        foreach (var game in upcoming)
        {
            var key = CacheKey.ScoreBoard(game);
            var value = await cache.GetAsync(key);
            if (value is not null)
                continue;

            await channelWriter.WriteAsync(ScoreboardCacheHandler.MakeCacheRequest(game));
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.CronJob_BootstrapRankingCache), key],
                TaskStatus.Success,
                LogLevel.Debug);
        }
    }
}
