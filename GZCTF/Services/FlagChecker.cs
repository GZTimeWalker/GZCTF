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
        var submissionRepository = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();

        try
        {
            await foreach(var item in channelReader.ReadAllAsync(token))
            {
                logger.SystemLog($"消费者 #{id} 开始处理提交：{item.Answer}", TaskStatus.Pending, LogLevel.Debug);

                item.Status = await instanceRepository.VerifyAnswer(item, token);

                await submissionRepository.UpdateSubmission(item, token);

                if (item.Status == AnswerResult.Accepted)
                {
                    logger.Log($"[提交正确] 队伍[{item.Participation.Team.Name}]提交题目[{item.Challenge.Title}]的答案[{item.Answer}]", item.User!, TaskStatus.Success, LogLevel.Information);
                    continue;
                }

                if (item.Status == AnswerResult.NotFound)
                {
                    logger.Log($"[实例未知] 未找到队伍[{item.Participation.Team.Name}]提交题目[{item.Challenge.Title}]的实例", item.User!, TaskStatus.NotFound, LogLevel.Warning);
                    continue;
                }

                logger.Log($"[提交错误] 队伍[{item.Participation.Team.Name}]提交题目[{item.Challenge.Title}]的答案[{item.Answer}]", item.User!, TaskStatus.Fail, LogLevel.Information);

                var result = await instanceRepository.CheckCheat(item, token);

                if (result.AnswerResult == AnswerResult.WrongAnswer)
                    continue;

                logger.Log($"[作弊检查] 题目[{item.Challenge.Title}]中队伍[{item.Participation.Team.Name}]疑似作弊，相关队伍[{result.SourceTeam!.Name}]", item.User!, TaskStatus.Fail, LogLevel.Warning);

                await submissionRepository.UpdateSubmission(item, token);

                // TODO: send game event

                token.ThrowIfCancellationRequested();
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
