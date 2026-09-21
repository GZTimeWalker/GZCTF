using GZCTF.Repositories.Interface;
using GZCTF.Services.Traffic;

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
}
