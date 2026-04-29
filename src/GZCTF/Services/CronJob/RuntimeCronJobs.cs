using System.Threading.Channels;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Cache.Handlers;
using GZCTF.Services.Traffic;
using Microsoft.Extensions.Caching.Distributed;

// ReSharper disable UnusedMember.Global

namespace GZCTF.Services.CronJob;

public static class RuntimeCronJobs
{
    [CronJob("*/3 * * * *")]
    public static async Task ContainerChecker(AsyncServiceScope scope, ILogger<CronJobService> logger)
    {
        var containerRepo = scope.ServiceProvider.GetRequiredService<IContainerRepository>();
        var trafficRegistry = scope.ServiceProvider.GetRequiredService<TrafficRecorderRegistry>();

        foreach (var container in await containerRepo.GetDyingContainers())
        {
            await trafficRegistry.ArchiveAsync(container.Id);
            await containerRepo.DestroyContainer(container);
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.CronJob_RemoveExpiredContainer),
                    container.LogId],
                TaskStatus.Success, LogLevel.Debug);
        }
    }

    [CronJob("*/10 * * * *")]
    public static async Task BootstrapCache(AsyncServiceScope scope, ILogger<CronJobService> logger)
    {
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var upcoming = await gameRepo.GetUpcomingGames();

        if (upcoming.Length <= 0)
            return;

        var writer = scope.ServiceProvider.GetRequiredService<ChannelWriter<CacheRequest>>();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

        foreach (var game in upcoming)
        {
            var key = CacheKey.ScoreBoard(game);
            if (await cache.GetAsync(key) is not null)
                continue;

            await writer.WriteAsync(ScoreboardCacheHandler.MakeCacheRequest(game));
            logger.SystemLog(StaticLocalizer[nameof(Resources.Program.CronJob_BootstrapRankingCache), key],
                TaskStatus.Success,
                LogLevel.Debug);
        }
    }

    [CronJob("0 * * * *")]
    public static async Task FlushRecentGames(AsyncServiceScope scope, ILogger<CronJobService> logger)
    {
        var helper = scope.ServiceProvider.GetRequiredService<CacheHelper>();

        await helper.FlushRecentGamesCache(CancellationToken.None);
    }
}
