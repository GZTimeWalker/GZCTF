﻿using System.Threading.Channels;
using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Services;

public class FlagChecker(
    ChannelReader<Submission> channelReader,
    ChannelWriter<Submission> channelWriter,
    ILogger<FlagChecker> logger,
    IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    CancellationTokenSource TokenSource { get; set; } = new();
    const int MaxWorkerCount = 4;

    internal static int GetWorkerCount()
    {
        // if RAM < 2GiB or CPU <= 3, return 1
        // if RAM < 4GiB or CPU <= 6, return 2
        // otherwise, return 4
        var memoryInfo = GC.GetGCMemoryInfo();
        double freeMemory = memoryInfo.TotalAvailableMemoryBytes / 1024.0 / 1024.0 / 1024.0;
        var cpuCount = Environment.ProcessorCount;
        
        if (freeMemory < 2 || cpuCount <= 3)
            return 1;
        if (freeMemory < 4 || cpuCount <= 6)
            return 2;
        return MaxWorkerCount;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        TokenSource = new CancellationTokenSource();

        for (var i = 0; i < GetWorkerCount(); ++i)
        {
            await Task.Factory.StartNew(() => Checker(i, TokenSource.Token), cancellationToken,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();

        var submissionRepository = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
        Submission[] flags = await submissionRepository.GetUncheckedFlags(TokenSource.Token);

        foreach (Submission item in flags)
            await channelWriter.WriteAsync(item, TokenSource.Token);

        if (flags.Length > 0)
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.FlagsChecker_Recheck), flags.Length],
                TaskStatus.Pending,
                LogLevel.Debug);

        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.FlagsChecker_Started)], TaskStatus.Success,
            LogLevel.Debug);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        TokenSource.Cancel();

        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.FlagsChecker_Stopped)], TaskStatus.Exit,
            LogLevel.Debug);

        return Task.CompletedTask;
    }

    async Task Checker(int id, CancellationToken token = default)
    {
        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.FlagsChecker_WorkerStarted), id],
            TaskStatus.Pending,
            LogLevel.Debug);

        try
        {
            await foreach (Submission item in channelReader.ReadAllAsync(token))
            {
                logger.SystemLog(
                    Program.StaticLocalizer[nameof(Resources.Program.FlagsChecker_WorkerStartProcessing), id,
                        item.Answer],
                    TaskStatus.Pending, LogLevel.Debug);

                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();

                var cacheHelper = scope.ServiceProvider.GetRequiredService<CacheHelper>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IGameEventRepository>();
                var instanceRepository = scope.ServiceProvider.GetRequiredService<IGameInstanceRepository>();
                var gameNoticeRepository = scope.ServiceProvider.GetRequiredService<IGameNoticeRepository>();
                var submissionRepository = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();

                try
                {
                    (SubmissionType type, AnswerResult ans) = await instanceRepository.VerifyAnswer(item, token);

                    switch (ans)
                    {
                        case AnswerResult.NotFound:
                            logger.Log(
                                Program.StaticLocalizer[nameof(Resources.Program.FlagChecker_UnknownInstance),
                                    item.Team.Name,
                                    item.GameChallenge.Title],
                                item.User,
                                TaskStatus.NotFound, LogLevel.Warning);
                            break;
                        case AnswerResult.Accepted:
                            {
                                logger.Log(
                                    Program.StaticLocalizer[nameof(Resources.Program.FlagChecker_AnswerAccepted),
                                        item.Team.Name,
                                        item.GameChallenge.Title,
                                        item.Answer],
                                    item.User, TaskStatus.Success, LogLevel.Information);

                                await eventRepository.AddEvent(
                                    GameEvent.FromSubmission(item, type, ans, Program.StaticLocalizer), token);

                                // only flush the scoreboard if the contest is not ended and the submission is accepted
                                if (item.Game.EndTimeUtc > item.SubmitTimeUtc)
                                    await cacheHelper.FlushScoreboardCache(item.GameId, token);
                                break;
                            }
                        default:
                            {
                                logger.Log(
                                    Program.StaticLocalizer[nameof(Resources.Program.FlagChecker_AnswerRejected),
                                        item.Team.Name,
                                        item.GameChallenge.Title,
                                        item.Answer],
                                    item.User, TaskStatus.Failed, LogLevel.Information);

                                await eventRepository.AddEvent(
                                    GameEvent.FromSubmission(item, type, ans, Program.StaticLocalizer), token);

                                CheatCheckInfo result = await instanceRepository.CheckCheat(item, token);
                                ans = result.AnswerResult;

                                if (ans == AnswerResult.CheatDetected)
                                {
                                    logger.Log(
                                        Program.StaticLocalizer[nameof(Resources.Program.FlagChecker_CheatDetected),
                                            item.Team.Name,
                                            item.GameChallenge.Title,
                                            result.SourceTeamName ?? ""],
                                        item.User, TaskStatus.Success, LogLevel.Information);

                                    await eventRepository.AddEvent(
                                        new()
                                        {
                                            Type = EventType.CheatDetected,
                                            Values =
                                                [item.GameChallenge.Title, item.Team.Name, result.SourceTeamName ?? ""],
                                            TeamId = item.TeamId,
                                            UserId = item.UserId,
                                            GameId = item.GameId
                                        }, token);
                                }

                                break;
                            }
                    }

                    if (item.Game.EndTimeUtc > DateTimeOffset.UtcNow
                        && type != SubmissionType.Unaccepted
                        && type != SubmissionType.Normal)
                        await gameNoticeRepository.AddNotice(
                            GameNotice.FromSubmission(item, type, Program.StaticLocalizer), token);

                    item.Status = ans;
                    await submissionRepository.SendSubmission(item);
                }
                catch (DbUpdateConcurrencyException)
                {
                    logger.SystemLog(
                        Program.StaticLocalizer[nameof(Resources.Program.FlagChecker_ConcurrencyFailed), item.Id],
                        TaskStatus.Failed,
                        LogLevel.Warning);
                    await channelWriter.WriteAsync(item, token);
                }
                catch (Exception e)
                {
                    logger.SystemLog(
                        Program.StaticLocalizer[nameof(Resources.Program.FlagsChecker_WorkerExceptionOccurred), id],
                        TaskStatus.Failed,
                        LogLevel.Debug);
                    logger.LogError(e.Message, e);
                }

                token.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException)
        {
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.FlagsChecker_WorkerCancelled), id],
                TaskStatus.Exit,
                LogLevel.Debug);
        }
        finally
        {
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.FlagsChecker_WorkerStopped), id],
                TaskStatus.Exit,
                LogLevel.Debug);
        }
    }
}
