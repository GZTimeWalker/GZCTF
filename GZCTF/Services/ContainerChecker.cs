using CTFServer.Repositories.Interface;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using NLog;

namespace CTFServer.Services;

public class ContainerChecker : IHostedService, IDisposable
{
    private static readonly Logger logger = LogManager.GetLogger("ContainerChecker");

    private readonly IServiceScopeFactory serviceProvider;
    private Timer? timer;

    public ContainerChecker(IServiceScopeFactory provider)
    {
        serviceProvider = provider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        LogHelper.SystemLog(logger, "容器生命周期检查已启动");
        timer = new Timer(Execute, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        return Task.CompletedTask;
    }

    private async void Execute(object? state)
    {
        using var scope = serviceProvider.CreateScope();

        var containerRepo = scope.ServiceProvider.GetRequiredService<IContainerRepository>();
        var containerService = scope.ServiceProvider.GetRequiredService<IContainerService>();

        foreach (var container in await containerRepo.GetDyingContainers())
        {
            await containerService.DestoryContainer(container);
            await containerRepo.RemoveContainer(container);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        timer?.Change(Timeout.Infinite, 0);
        LogHelper.SystemLog(logger, "容器生命周期检查已停止");
        return Task.CompletedTask;
    }

    public void Dispose() => timer?.Dispose();
}
