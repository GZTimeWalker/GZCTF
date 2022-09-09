using CTFServer.Repositories.Interface;
using CTFServer.Services.Interface;
using CTFServer.Utils;

namespace CTFServer.Services;

public class ContainerChecker : IHostedService, IDisposable
{
    private readonly ILogger<ContainerChecker> logger;
    private readonly IServiceScopeFactory serviceProvider;
    private Timer? timer;

    public ContainerChecker(IServiceScopeFactory provider, ILogger<ContainerChecker> logger)
    {
        serviceProvider = provider;
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        timer = new Timer(Execute, null, TimeSpan.Zero, TimeSpan.FromMinutes(3));
        logger.SystemLog("容器生命周期检查已启动", TaskStatus.Success, LogLevel.Debug);
        return Task.CompletedTask;
    }

    private async void Execute(object? state)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var containerRepo = scope.ServiceProvider.GetRequiredService<IContainerRepository>();
        var containerService = scope.ServiceProvider.GetRequiredService<IContainerService>();


        foreach (var container in await containerRepo.GetDyingContainers())
        {
            await containerService.DestoryContainer(container);
            await containerRepo.RemoveContainer(container);
            logger.SystemLog($"移除到期容器 [{container.ContainerId[..12]}]");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        timer?.Change(Timeout.Infinite, 0);
        logger.SystemLog("容器生命周期检查已停止", TaskStatus.Exit, LogLevel.Debug);
        return Task.CompletedTask;
    }

    public void Dispose() => timer?.Dispose();
}