using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using System.Threading.Channels;

namespace CTFServer.Services;

public static class ChannelService
{
    internal static IServiceCollection AddChannel<T>(this IServiceCollection services)
    {
        var channel = Channel.CreateUnbounded<T>();
        services.AddSingleton(channel);
        services.AddSingleton(channel.Reader);
        services.AddSingleton(channel.Writer);
        return services;
    }
}

public class FlagChecker : IHostedService
{
    private readonly ILogger<FlagChecker> logger;
    private readonly ChannelReader<Submission> channelReader;
    private readonly IServiceScopeFactory serviceScopeFactory;

    private CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();

    public FlagChecker(ChannelReader<Submission> _channelReader,
        ILogger<FlagChecker> _logger,
        IServiceScopeFactory _serviceScopeFactory)
    {
        logger = _logger;
        channelReader = _channelReader;
        serviceScopeFactory = _serviceScopeFactory;
    }

    private async Task Consumer(int id, CancellationToken token = default)
    {
        logger.SystemLog($"消费者 #{id} 已启动", TaskStatus.Pending, LogLevel.Debug);

        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var eventRepository = scope.ServiceProvider.GetRequiredService<IGameEventRepository>();
        var instanceRepository = scope.ServiceProvider.GetRequiredService<IInstanceRepository>();

        try
        {
            await foreach(var item in channelReader.ReadAllAsync(token))
            {
                logger.SystemLog($"消费者 #{id} 开始处理提交：{item.Answer}", TaskStatus.Exit, LogLevel.Debug);
                var result = await instanceRepository.CheckCheat(item, token);
                // TODO
            }
        }
        catch (TaskCanceledException) 
        {
            logger.SystemLog($"任务取消，消费者 #{id} 将退出", TaskStatus.Exit, LogLevel.Warning);
        }
        finally
        {
            logger.SystemLog($"消费者 #{id} 已退出", TaskStatus.Exit, LogLevel.Debug);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        TokenSource = new CancellationTokenSource();

        for(int i = 0; i < 4; ++i)
            _ = Consumer(i, TokenSource.Token);

        logger.SystemLog("Flag 作弊检查已启用", TaskStatus.Success, LogLevel.Information);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        TokenSource.Cancel();

        logger.SystemLog("Flag 作弊检查已停止", TaskStatus.Success, LogLevel.Information);

        return Task.CompletedTask;
    }
}
