using System.Threading.Channels;
using GZCTF.Repositories;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Interface;
using GZCTF.Utils;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Services;

public class CronJobService : IHostedService, IDisposable
{
    private readonly ILogger<CronJobService> _logger;
    private readonly IServiceScopeFactory _serviceProvider;
    private Timer? _timer;

    public CronJobService(IServiceScopeFactory provider, ILogger<CronJobService> logger)
    {
        _serviceProvider = provider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken token)
    {
        _timer = new Timer(Execute, null, TimeSpan.Zero, TimeSpan.FromMinutes(3));
        _logger.SystemLog("定时任务已启动", TaskStatus.Success, LogLevel.Debug);
        return Task.CompletedTask;
    }

    private async Task ContainerChecker(AsyncServiceScope scope)
    {
        var containerRepo = scope.ServiceProvider.GetRequiredService<IContainerRepository>();
        var containerService = scope.ServiceProvider.GetRequiredService<IContainerService>();

        foreach (var container in await containerRepo.GetDyingContainers())
        {
            await containerService.DestroyContainerAsync(container);
            await containerRepo.RemoveContainer(container);
            _logger.SystemLog($"移除到期容器 [{container.ContainerId}]", TaskStatus.Success, LogLevel.Debug);
        }
    }

    private async Task BootstrapCache(AsyncServiceScope scope)
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
            if (value is null)
            {
                await channelWriter.WriteAsync(ScoreboardCacheHandler.MakeCacheRequest(game));
                _logger.SystemLog($"比赛 #{key} 即将开始，积分榜缓存已加入缓存队列", TaskStatus.Success, LogLevel.Debug);
            }
        }
    }

    private async void Execute(object? state)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        await ContainerChecker(scope);
        await BootstrapCache(scope);
    }

    public Task StopAsync(CancellationToken token)
    {
        _timer?.Change(Timeout.Infinite, 0);
        _logger.SystemLog("定时任务已停止", TaskStatus.Exit, LogLevel.Debug);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
