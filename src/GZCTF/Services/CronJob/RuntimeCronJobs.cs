using System.Threading.Channels;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Cache.Handlers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Services.CronJob;

public static class RuntimeCronJobs
{
    [CronJob("*/3 * * * *")]
    public static async Task ContainerChecker(AsyncServiceScope scope, ILogger<CronJobService> logger)
    {
        var containerRepo = scope.ServiceProvider.GetRequiredService<IContainerRepository>();

        foreach (var container in await containerRepo.GetDyingContainers())
        {
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

        var channelWriter =
            scope.ServiceProvider.GetRequiredService<ChannelWriter<CacheRequest>>();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

        foreach (var game in upcoming)
        {
            var key = CacheKey.ScoreBoard(game);
            if (await cache.GetAsync(key) is not null)
                continue;

            await channelWriter.WriteAsync(ScoreboardCacheHandler.MakeCacheRequest(game));
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

    [CronJob("0 */4 * * *")]
    public static async Task RemoveUnactivatedUsers(AsyncServiceScope scope, ILogger<CronJobService> logger)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserInfo>>();
        var timeThreshold = DateTimeOffset.UtcNow.AddHours(-48);

        var usersToDelete = userManager.Users
            .Where(u => !u.EmailConfirmed && u.RegisterTimeUtc < timeThreshold)
            .ToList();

        if (usersToDelete.Count == 0)
            return;

        foreach (var user in usersToDelete)
        {
            await userManager.DeleteAsync(user);
        }

        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.CronJob_RemoveUnactivatedUsers), usersToDelete.Count],
            TaskStatus.Success, LogLevel.Information);
    }
}
