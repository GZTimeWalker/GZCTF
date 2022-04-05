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

    private async Task Consumer(CancellationToken token = default)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        try
        {
            while(!channelReader.Completion.IsCompleted)
            {
                var item = await channelReader.ReadAsync(token);
                // TODO
            }
        }
        catch (TaskCanceledException) { }
        catch (ChannelClosedException) 
        {
            logger.SystemLog("管道关闭，消费者将退出", TaskStatus.Exit, LogLevel.Warning);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        TokenSource = new CancellationTokenSource();

        for(int i = 0; i < 4; ++i)
            _ = Consumer(TokenSource.Token);

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
