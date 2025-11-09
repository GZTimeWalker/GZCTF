using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Container.Manager;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Repositories;

public class GameInstanceRepository(
    AppDbContext context,
    IContainerManager service,
    ICheatInfoRepository cheatInfoRepository,
    IContainerRepository containerRepository,
    IGameEventRepository gameEventRepository,
    IOptionsSnapshot<ContainerPolicy> containerPolicy,
    ILogger<GameInstanceRepository> logger,
    IStringLocalizer<Program> localizer) : RepositoryBase(context), IGameInstanceRepository
{
    public async Task<GameInstance?> GetInstance(Participation part, int challengeId, CancellationToken token = default)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(token);

        var instance = await Context.GameInstances
            .Include(i => i.FlagContext)
            .Where(e => e.ChallengeId == challengeId && e.Participation == part)
            .SingleOrDefaultAsync(token);

        if (instance is null)
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.InstanceRepository_NoInstance), part.Id, challengeId],
                TaskStatus.NotFound,
                LogLevel.Warning);
            await transaction.RollbackAsync(token);
            return null;
        }

        var challenge = instance.Challenge;

        if (!challenge.IsEnabled)
        {
            await transaction.RollbackAsync(token);
            return null;
        }

        if (instance.IsLoaded)
        {
            await transaction.RollbackAsync(token);
            return instance;
        }

        try
        {
            switch (instance.Challenge.Type)
            {
                // dynamic flag dispatch
                case ChallengeType.DynamicContainer:
                    instance.FlagContext = new()
                    {
                        Challenge = challenge,
                        Flag = challenge.GenerateDynamicFlag(part),
                        IsOccupied = true
                    };
                    break;
                case ChallengeType.DynamicAttachment:
                    var flags = await Context.FlagContexts
                        .Where(e => e.Challenge == challenge && !e.IsOccupied)
                        .ToListAsync(token);

                    if (flags.Count == 0)
                    {
                        logger.SystemLog(
                            StaticLocalizer[nameof(Resources.Program.InstanceRepository_DynamicFlagsNotEnough),
                                challenge.Title,
                                challenge.Id], TaskStatus.Failed,
                            LogLevel.Warning);
                        return null;
                    }

                    var pos = Random.Shared.Next(flags.Count);
                    flags[pos].IsOccupied = true;

                    instance.FlagId = flags[pos].Id;
                    break;
            }

            // instance.FlagContext is null by default
            // static flag does not need to be dispatched

            instance.IsLoaded = true;
            await SaveAsync(token);
            await transaction.CommitAsync(token);
        }
        catch
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.InstanceRepository_GetInstanceFailed), part.Team.Name,
                    challenge.Title,
                    challenge.Id],
                TaskStatus.Failed, LogLevel.Warning);
            await transaction.RollbackAsync(token);
            return null;
        }

        return instance;
    }

    public Task<GameInstance?> GetInstanceForSubmission(Participation team, int challengeId,
        CancellationToken token = default)
        => Context.GameInstances.IgnoreAutoIncludes()
            .Include(i => i.Challenge)
            .Where(i => i.ParticipationId == team.Id && i.ChallengeId == challengeId)
            .SingleOrDefaultAsync(token);

    public async Task<TaskResult<Container>> CreateContainer(GameInstance gameInstance, Team team, UserInfo user,
        Game game, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(gameInstance.Challenge.ContainerImage) ||
            gameInstance.Challenge.ContainerExposePort is null)
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreationFailed),
                    gameInstance.Challenge.Title],
                TaskStatus.Denied, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        // containerLimit == 0 means unlimited
        if (game.ContainerCountLimit > 0)
        {
            if (containerPolicy.Value.AutoDestroyOnLimitReached)
            {
                var running = await Context.GameInstances
                    .Where(i => i.Participation == gameInstance.Participation && i.Container != null)
                    .OrderBy(i => i.Container!.StartedAt).ToListAsync(token);

                var first = running.FirstOrDefault();
                if (running.Count >= game.ContainerCountLimit && first is not null)
                {
                    logger.Log(
                        StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerAutoDestroy),
                            team.Name, first.Challenge.Title,
                            first.Container!.LogId],
                        user, TaskStatus.Success);
                    await containerRepository.DestroyContainer(running.First().Container!, token);
                }
            }
            else
            {
                var count = await Context.GameInstances.CountAsync(
                    i => i.Participation == gameInstance.Participation &&
                         i.Container != null, token);

                if (count >= game.ContainerCountLimit)
                    return new TaskResult<Container>(TaskStatus.Denied);
            }
        }

        if (gameInstance.Container is not null)
            return new TaskResult<Container>(TaskStatus.Success, gameInstance.Container);

        await Context.Entry(gameInstance).Reference(e => e.FlagContext).LoadAsync(token);
        var container = await service.CreateContainerAsync(new ContainerConfig
        {
            TeamId = team.Id.ToString(),
            UserId = user.Id,
            ChallengeId = gameInstance.ChallengeId,
            Flag = gameInstance.FlagContext?.Flag, // static challenge has no specific flag
            Image = gameInstance.Challenge.ContainerImage,
            CPUCount = gameInstance.Challenge.CPUCount ?? 1,
            MemoryLimit = gameInstance.Challenge.MemoryLimit ?? 64,
            StorageLimit = gameInstance.Challenge.StorageLimit ?? 256,
            EnableTrafficCapture = gameInstance.Challenge.EnableTrafficCapture && game.IsActive,
            ExposedPort = gameInstance.Challenge.ContainerExposePort ??
                          throw new ArgumentException(
                              localizer[nameof(Resources.Program.InstanceRepository_InvalidPort)])
        }, token);

        if (container is null)
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreationFailed),
                    gameInstance.Challenge.Title],
                TaskStatus.Failed, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        // update the ExpectStopAt with config
        container.ExpectStopAt = container.StartedAt.AddMinutes(containerPolicy.Value.DefaultLifetime);

        gameInstance.Container = container;
        gameInstance.LastContainerOperation = DateTimeOffset.UtcNow;

        await gameEventRepository.AddEvent(
            new()
            {
                Type = EventType.ContainerStart,
                GameId = gameInstance.Challenge.GameId,
                TeamId = gameInstance.Participation.TeamId,
                UserId = user.Id,
                Values = [gameInstance.Challenge.Id.ToString(), gameInstance.Challenge.Title]
            }, token);

        logger.Log(
            StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreated), team.Name,
                gameInstance.Challenge.Title,
                container.LogId], user,
            TaskStatus.Success);

        return new TaskResult<Container>(TaskStatus.Success, gameInstance.Container);
    }

    public async Task DestroyAllContainers(GameChallenge challenge, CancellationToken token = default)
    {
        foreach (var container in await Context.GameInstances
                     .Include(i => i.Container)
                     .Where(i => i.Challenge == challenge && i.ContainerId != null)
                     .Select(i => i.Container)
                     .ToArrayAsync(token))
        {
            if (container is null)
                continue;

            await containerRepository.DestroyContainer(container, token);
        }
    }

    public async Task<CheatCheckInfo> CheckCheat(Submission submission, CancellationToken token = default)
    {
        CheatCheckInfo checkInfo = new();

        var instance = await Context.GameInstances
            .Include(i => i.Participation)
            .ThenInclude(i => i.Team)
            .Include(i => i.FlagContext)
            .Where(i => i.ChallengeId == submission.ChallengeId &&
                        i.ParticipationId != submission.ParticipationId &&
                        i.FlagContext != null && i.FlagContext.Flag == submission.Answer)
            .FirstOrDefaultAsync(token);

        if (instance is null)
            return checkInfo;

        var updateSub = await Context.Submissions.Where(s => s.Id == submission.Id).SingleAsync(token);

        var cheatInfo = await cheatInfoRepository.CreateCheatInfo(updateSub, instance, token);

        checkInfo = CheatCheckInfo.FromCheatInfo(cheatInfo);

        updateSub.Status = AnswerResult.CheatDetected;

        await SaveAsync(token);

        return checkInfo;
    }

    public async Task<VerifyResult> VerifyAnswer(Submission submission, CancellationToken token = default)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(token);

        try
        {
            var instance = await Context.GameInstances.IgnoreAutoIncludes()
                .Include(i => i.FlagContext)
                .SingleOrDefaultAsync(i => i.ChallengeId == submission.ChallengeId &&
                                           i.ParticipationId == submission.ParticipationId, token);

            if (instance is null)
            {
                submission.Status = AnswerResult.NotFound;
                await transaction.RollbackAsync(token);
                return new(SubmissionType.Unaccepted, AnswerResult.NotFound);
            }

            var updateSub = await Context.Submissions
                .IgnoreAutoIncludes()
                .SingleAsync(s => s.Id == submission.Id, token);

            var challenge = await Context.GameChallenges
                .AsNoTracking()
                .Select(c => new { c.Id, c.Type, c.DisableBloodBonus, c.DeadlineUtc })
                .SingleAsync(c => c.Id == submission.ChallengeId, token);

            if (instance.FlagContext is null && challenge.Type.IsStatic())
            {
                updateSub.Status = await Context.FlagContexts.AsNoTracking()
                    .AnyAsync(
                        f => f.ChallengeId == submission.ChallengeId && f.Flag == submission.Answer,
                        token)
                    ? AnswerResult.Accepted
                    : AnswerResult.WrongAnswer;
            }
            else
            {
                updateSub.Status = instance.FlagContext?.Flag == submission.Answer
                    ? AnswerResult.Accepted
                    : AnswerResult.WrongAnswer;
            }

            if (updateSub.Status != AnswerResult.Accepted)
            {
                await SaveAsync(token);
                await transaction.CommitAsync(token);
                return new(SubmissionType.Unaccepted, updateSub.Status);
            }

            // Acquire a PostgresSQL advisory lock to prevent race conditions:
            // This lock ensures that only one concurrent submission for the same participation/challenge pair
            // can proceed past this point, preventing duplicate FirstSolve entries if multiple submissions
            // are processed at the same time. Without this, two submissions could both pass the check and
            // insert duplicate records.
            await Context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0}, {1})",
                [updateSub.ParticipationId, updateSub.ChallengeId],
                cancellationToken: token);

            var alreadySolved = await Context.FirstSolves
                .AnyAsync(fs => fs.ParticipationId == submission.ParticipationId &&
                                fs.ChallengeId == submission.ChallengeId, token);

            if (alreadySolved)
            {
                await SaveAsync(token);
                await transaction.CommitAsync(token);
                return new(SubmissionType.Normal, updateSub.Status);
            }

            var participation = await Context.Participations
                .IgnoreAutoIncludes().AsNoTracking()
                .Include(p => p.Division)
                .ThenInclude(d => d!.ChallengeConfigs)
                .SingleAsync(p => p.Id == submission.ParticipationId, token);

            var time = await Context.Games.IgnoreAutoIncludes().AsNoTracking()
                .Where(g => g.Id == participation.GameId)
                .Select(g => new { g.StartTimeUtc, g.EndTimeUtc })
                .SingleAsync(token);

            // Check if submission is within game time window
            var withinGameWindow = updateSub.SubmitTimeUtc >= time.StartTimeUtc &&
                                   updateSub.SubmitTimeUtc < time.EndTimeUtc;

            // Check if submission is within challenge deadline (if deadline is set)
            var withinDeadline = !challenge.DeadlineUtc.HasValue ||
                                 updateSub.SubmitTimeUtc <= challenge.DeadlineUtc.Value;

            // Blood bonus is only awarded if submission is within both game window and deadline
            var hasBloodPermission = withinGameWindow && withinDeadline && !challenge.DisableBloodBonus &&
                                     HasPermission(participation.Division, GamePermission.GetBlood,
                                         submission.ChallengeId);

            var submissionType = SubmissionType.Normal;
            if (hasBloodPermission)
            {
                var bloodEligibleCount = await CountBloodEligibleSolves(submission.ChallengeId, time.StartTimeUtc,
                    time.EndTimeUtc, token);

                submissionType = bloodEligibleCount switch
                {
                    0 => SubmissionType.FirstBlood,
                    1 => SubmissionType.SecondBlood,
                    2 => SubmissionType.ThirdBlood,
                    _ => SubmissionType.Normal
                };
            }

            Context.FirstSolves.Add(new FirstSolve
            {
                ParticipationId = submission.ParticipationId,
                ChallengeId = submission.ChallengeId,
                SubmissionId = submission.Id
            });

            await SaveAsync(token);
            await transaction.CommitAsync(token);

            return new(submissionType, updateSub.Status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during answer verification for submission {SubmissionId}.",
                submission.Id);
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    Task<int> CountBloodEligibleSolves(int challengeId, DateTimeOffset start, DateTimeOffset end,
        CancellationToken token)
    {
        // First, get blood-eligible participation IDs for the challenge
        var eligibleParticipationIds =
            from participation in Context.Participations.AsNoTracking()
            join config in Context.Set<DivisionChallengeConfig>().AsNoTracking()
                    .Where(c => c.ChallengeId == challengeId)
                on participation.DivisionId equals config.DivisionId into configJoin
            from cfg in configJoin.DefaultIfEmpty()
            join division in Context.Divisions.AsNoTracking()
                on participation.DivisionId equals division.Id into divisionJoin
            from div in divisionJoin.DefaultIfEmpty()
            where participation.Status == ParticipationStatus.Accepted
                  && (cfg != null
                      ? cfg.Permissions.HasFlag(GamePermission.GetBlood)
                      : div == null || div.DefaultPermissions.HasFlag(GamePermission.GetBlood))
            select participation.Id;

        // Now, count FirstSolves for the challenge with eligible participations and time window
        return (
            from fs in Context.FirstSolves.AsNoTracking()
            join submission in Context.Submissions.AsNoTracking() on fs.SubmissionId equals submission.Id
            where fs.ChallengeId == challengeId
                  && eligibleParticipationIds.Contains(fs.ParticipationId)
                  && submission.SubmitTimeUtc >= start
                  && submission.SubmitTimeUtc < end
            orderby submission.SubmitTimeUtc
            select fs.ParticipationId
        ).Take(4).CountAsync(token);
    }

    static bool HasPermission(Division? division, GamePermission permission, int challengeId)
    {
        if (division is null)
            return true;

        var specific = division.ChallengeConfigs.FirstOrDefault(c => c.ChallengeId == challengeId);
        var permissions = specific?.Permissions ?? division.DefaultPermissions;
        return permissions.HasFlag(permission);
    }
}
