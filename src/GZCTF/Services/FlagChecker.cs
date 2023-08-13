using System.Threading.Channels;
using GZCTF.Models;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Services;

public class FlagChecker : IHostedService
{
    private readonly ILogger<FlagChecker> _logger;
    private readonly ChannelReader<Submission> _channelReader;
    private readonly ChannelWriter<Submission> _channelWriter;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();

    public FlagChecker(ChannelReader<Submission> channelReader,
        ChannelWriter<Submission> channelWriter,
        ILogger<FlagChecker> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _channelReader = channelReader;
        _channelWriter = channelWriter;
        _serviceScopeFactory = serviceScopeFactory;
    }

    private async Task Checker(int id, CancellationToken token = default)
    {
        _logger.SystemLog($"检查线程 #{id} 已启动", TaskStatus.Pending, LogLevel.Debug);

        try
        {
            await foreach (var item in _channelReader.ReadAllAsync(token))
            {
                _logger.SystemLog($"检查线程 #{id} 开始处理提交：{item.Answer}", TaskStatus.Pending, LogLevel.Debug);

                await using var scope = _serviceScopeFactory.CreateAsyncScope();

                var eventRepository = scope.ServiceProvider.GetRequiredService<IGameEventRepository>();
                var instanceRepository = scope.ServiceProvider.GetRequiredService<IInstanceRepository>();
                var gameNoticeRepository = scope.ServiceProvider.GetRequiredService<IGameNoticeRepository>();
                var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
                var submissionRepository = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();

                try
                {
                    var (type, ans) = await instanceRepository.VerifyAnswer(item, token);

                    if (ans == AnswerResult.NotFound)
                        _logger.Log($"[实例未知] 未找到队伍 [{item.Team.Name}] 提交题目 [{item.Challenge.Title}] 的实例", item.User!, TaskStatus.NotFound, LogLevel.Warning);
                    else if (ans == AnswerResult.Accepted)
                    {
                        _logger.Log($"[提交正确] 队伍 [{item.Team.Name}] 提交题目 [{item.Challenge.Title}] 的答案 [{item.Answer}]", item.User!, TaskStatus.Success, LogLevel.Information);

                        await eventRepository.AddEvent(GameEvent.FromSubmission(item, type, ans), token);

                        // only flush the scoreboard if the contest is not ended and the submission is accepted
                        if (item.Game.EndTimeUTC > item.SubmitTimeUTC)
                            await gameRepository.FlushScoreboardCache(item.GameId, token);
                    }
                    else
                    {
                        _logger.Log($"[提交错误] 队伍 [{item.Team.Name}] 提交题目 [{item.Challenge.Title}] 的答案 [{item.Answer}]", item.User!, TaskStatus.Failed, LogLevel.Information);

                        await eventRepository.AddEvent(GameEvent.FromSubmission(item, type, ans), token);

                        var result = await instanceRepository.CheckCheat(item, token);
                        ans = result.AnswerResult;

                        if (ans == AnswerResult.CheatDetected)
                        {
                            _logger.Log($"[作弊检查] 队伍 [{item.Team.Name}] 疑似违规 [{item.Challenge.Title}]，相关队伍 [{result.SourceTeamName}]", item.User!, TaskStatus.Success, LogLevel.Information);
                            await eventRepository.AddEvent(new()
                            {
                                Type = EventType.CheatDetected,
                                Content = $"题目 [{item.Challenge.Title}] 疑似发生违规，相关队伍 [{item.Team.Name}] 和 [{result.SourceTeamName}]",
                                TeamId = item.TeamId,
                                UserId = item.UserId,
                                GameId = item.GameId,
                            }, token);
                        }
                    }

                    if (item.Game.EndTimeUTC > DateTimeOffset.UtcNow
                        && type != SubmissionType.Unaccepted
                        && type != SubmissionType.Normal)
                        await gameNoticeRepository.AddNotice(GameNotice.FromSubmission(item, type), token);

                    item.Status = ans;
                    await submissionRepository.SendSubmission(item);
                }
                catch (DbUpdateConcurrencyException)
                {
                    _logger.SystemLog($"[数据库并发] 未能更新提交 #{item.Id} 的状态", TaskStatus.Failed, LogLevel.Warning);
                    await _channelWriter.WriteAsync(item, token);
                }
                catch (Exception e)
                {
                    _logger.SystemLog($"检查线程 #{id} 发生异常", TaskStatus.Failed, LogLevel.Debug);
                    _logger.LogError(e.Message, e);
                }

                token.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.SystemLog($"任务取消，检查线程 #{id} 将退出", TaskStatus.Exit, LogLevel.Debug);
        }
        finally
        {
            _logger.SystemLog($"检查线程 #{id} 已退出", TaskStatus.Exit, LogLevel.Debug);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        TokenSource = new CancellationTokenSource();

        for (int i = 0; i < 2; ++i)
            _ = Checker(i, TokenSource.Token);

        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var submissionRepository = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
        var flags = await submissionRepository.GetUncheckedFlags(TokenSource.Token);

        foreach (var item in flags)
            await _channelWriter.WriteAsync(item, TokenSource.Token);

        if (flags.Length > 0)
            _logger.SystemLog($"重新开始检查 {flags.Length} 个 flag", TaskStatus.Pending, LogLevel.Debug);

        _logger.SystemLog("Flag 检查已启用", TaskStatus.Success, LogLevel.Debug);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        TokenSource.Cancel();

        _logger.SystemLog("Flag 检查已停止", TaskStatus.Exit, LogLevel.Debug);

        return Task.CompletedTask;
    }
}
