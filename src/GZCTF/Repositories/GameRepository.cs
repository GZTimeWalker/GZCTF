using System.Diagnostics;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Config;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class GameRepository(
    ILogger<GameRepository> logger,
    CacheHelper cacheHelper,
    IGameChallengeRepository challengeRepository,
    IParticipationRepository participationRepository,
    IConfigService configService,
    AppDbContext context) : RepositoryBase(context), IGameRepository
{
    readonly byte[] _xorKey = configService.GetXorKey();

    public override Task<int> CountAsync(CancellationToken token = default) => Context.Games.CountAsync(token);

    public Task<bool> HasGameAsync(int id, CancellationToken token = default)
        => Context.Games.AnyAsync(g => g.Id == id, token);

    public async Task<Game?> CreateGame(Game game, CancellationToken token = default)
    {
        game.GenerateKeyPair(_xorKey);

        if (_xorKey.Length == 0)
            logger.SystemLog(StaticLocalizer[nameof(Resources.Program.GameRepository_XorKeyNotConfigured)],
                TaskStatus.Pending,
                LogLevel.Warning);

        await Context.AddAsync(game, token);
        await SaveAsync(token);

        await cacheHelper.FlushGameListCache(token);
        await cacheHelper.FlushRecentGamesCache(token);

        return game;
    }

    public async Task UpdateGame(Game game, CancellationToken token = default)
    {
        await SaveAsync(token);

        await cacheHelper.FlushGameListCache(token);
        await cacheHelper.FlushRecentGamesCache(token);
        await cacheHelper.FlushScoreboardCache(game.Id, token);
    }

    public string GetToken(Game game, Team team) => $"{team.Id}:{game.Sign($"GZCTF_TEAM_{team.Id}", _xorKey)}";

    public Task<Game?> GetGameById(int id, CancellationToken token = default) =>
        Context.Games.FirstOrDefaultAsync(x => x.Id == id, token);

    public Task<int[]> GetUpcomingGames(CancellationToken token = default) =>
        Context.Games.Where(g => g.StartTimeUtc > DateTime.UtcNow
                                 && g.StartTimeUtc - DateTime.UtcNow < TimeSpan.FromMinutes(15))
            .OrderBy(g => g.StartTimeUtc).Select(g => g.Id).ToArrayAsync(token);

    public Task<BasicGameInfoModel[]> FetchGameList(int count, int skip, CancellationToken token) =>
        Context.Games.Where(g => !g.Hidden)
            .OrderByDescending(g => g.StartTimeUtc).Skip(skip).Take(count)
            .Select(game => new BasicGameInfoModel
            {
                Id = game.Id,
                Title = game.Title,
                Summary = game.Summary,
                PosterHash = game.PosterHash,
                StartTimeUtc = game.StartTimeUtc,
                EndTimeUtc = game.EndTimeUtc,
                TeamMemberCountLimit = game.TeamMemberCountLimit
            }).ToArrayAsync(token);


    public async Task<ArrayResponse<BasicGameInfoModel>> GetGameInfo(int count = 20, int skip = 0,
        CancellationToken token = default)
    {
        var total = await Context.Games.CountAsync(game => !game.Hidden, token);
        if (skip >= total)
            return new([], total);

        if (skip + count > 100)
            return new(await FetchGameList(count, skip, token), total);

        var games = await cacheHelper.GetOrCreateAsync(logger, CacheKey.GameList,
            entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(2);
                return FetchGameList(100, 0, token);
            }, token: token);

        return new(games.Skip(skip).Take(count).ToArray(), total);
    }

    public Task<DataWithModifiedTime<BasicGameInfoModel[]>> GetRecentGames(CancellationToken token = default)
        => cacheHelper.GetOrCreateAsync(logger, CacheKey.RecentGames,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                var games = await GenRecentGames(token);
                return new DataWithModifiedTime<BasicGameInfoModel[]>(games, DateTimeOffset.UtcNow);
            }, token: token);

    public Task<BasicGameInfoModel[]> GenRecentGames(CancellationToken token = default) =>
        // sort by following rules:
        // 1. ongoing games > upcoming games > ended games
        // 2. ongoing games: by end time, ascending
        // 3. upcoming games: by start time, ascending
        // 4. ended games: by end time, descending
        Context.Games
            .Where(g => !g.Hidden)
            .OrderBy(g =>
                g.EndTimeUtc <= DateTimeOffset.UtcNow
                    ? DateTimeOffset.UtcNow - g.EndTimeUtc // ended games
                    : g.StartTimeUtc >= DateTimeOffset.UtcNow
                        ? g.StartTimeUtc - DateTimeOffset.UtcNow // upcoming games
                        : DateTimeOffset.UtcNow - g.StartTimeUtc < g.EndTimeUtc - DateTimeOffset.UtcNow
                            ? DateTimeOffset.UtcNow - g.StartTimeUtc
                            : g.EndTimeUtc - DateTimeOffset.UtcNow)
            .Take(50) // limit to 50 games
            .Select(game => new BasicGameInfoModel
            {
                Id = game.Id,
                Title = game.Title,
                Summary = game.Summary,
                PosterHash = game.PosterHash,
                StartTimeUtc = game.StartTimeUtc,
                EndTimeUtc = game.EndTimeUtc,
                TeamMemberCountLimit = game.TeamMemberCountLimit
            })
            .ToArrayAsync(token);

    public Task<ScoreboardModel> GetScoreboard(Game game, CancellationToken token = default)
        => cacheHelper.GetOrCreateAsync(logger, CacheKey.ScoreBoard(game.Id),
            entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(7);
                return GenScoreboard(game, token);
            }, token: token);

    public Task<ScoreboardModel?> TryGetScoreboard(int gameId, CancellationToken token = default)
        => cacheHelper.GetAsync<ScoreboardModel>(CacheKey.ScoreBoard(gameId), token);

    public Task<bool> IsGameClosed(int gameId, CancellationToken token = default)
        => Context.Games.AnyAsync(game =>
            game.Id == gameId && game.EndTimeUtc < DateTimeOffset.UtcNow && !game.PracticeMode, token);

    public async Task<ScoreboardModel> GetScoreboardWithMembers(Game game, CancellationToken token = default)
    {
        // In most cases, we can get the scoreboard from the cache
        var scoreboard = await GetScoreboard(game, token);

        // load team info & participants
        var ids = scoreboard.Items.Values.Select(i => i.Id).ToArray();
        var teams = await Context.Teams
            .IgnoreAutoIncludes().Include(t => t.Captain)
            .Where(t => ids.Contains(t.Id))
            .Include(t => t.Members).ToHashSetAsync(token);

        // load participants with team id and game id, select all UserInfos
        var participants = await Context.UserParticipations
            .Where(p => ids.Contains(p.TeamId) && p.GameId == game.Id)
            .Include(p => p.User)
            .Select(p => new { p.TeamId, p.User })
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(g => g.Key,
                g => g.Select(p => p.User).ToHashSet(), token);

        // update scoreboard items
        foreach (var item in scoreboard.Items.Values)
        {
            if (teams.FirstOrDefault(t => t.Id == item.Id) is { } team)
                item.TeamInfo = team;

            if (participants.TryGetValue(item.Id, out var users))
                item.Participants = users;
        }

        return scoreboard;
    }

    public async Task<TaskStatus> DeleteGame(Game game, CancellationToken token = default)
    {
        var trans = await BeginTransactionAsync(token);

        try
        {
            var count = await Context.GameChallenges.Where(i => i.Game == game).CountAsync(token);
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.GameRepository_GameDeletionChallenges), game.Title,
                    count], TaskStatus.Pending,
                LogLevel.Debug
            );

            foreach (var chal in await Context.GameChallenges.Where(c => c.Game == game)
                         .ToArrayAsync(token))
                await challengeRepository.RemoveChallenge(chal, false, token);

            count = await Context.Participations.Where(i => i.Game == game).CountAsync(token);

            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.GameRepository_GameDeletionTeams), game.Title, count],
                TaskStatus.Pending, LogLevel.Debug
            );

            foreach (var part in await Context.Participations.Where(p => p.Game == game).ToArrayAsync(token))
                await participationRepository.RemoveParticipation(part, false, token);

            Context.Remove(game);

            await SaveAsync(token);
            await trans.CommitAsync(token);

            await cacheHelper.FlushGameListCache(token);
            await cacheHelper.FlushRecentGamesCache(token);

            await cacheHelper.RemoveAsync(CacheKey.ScoreBoard(game.Id), token);

            return TaskStatus.Success;
        }
        catch (Exception e)
        {
            logger.SystemLog(StaticLocalizer[nameof(Resources.Program.Game_DeletionFailed)], TaskStatus.Pending,
                LogLevel.Debug);
            logger.SystemLog(e.Message, TaskStatus.Failed, LogLevel.Warning);
            await trans.RollbackAsync(token);

            return TaskStatus.Failed;
        }
    }

    public async Task DeleteAllWriteUps(Game game, CancellationToken token = default)
    {
        await Context.Entry(game).Collection(g => g.Participations).LoadAsync(token);

        logger.SystemLog(
            StaticLocalizer[nameof(Resources.Program.GameRepository_GameDeletionTeams), game.Title,
                game.Participations.Count],
            TaskStatus.Pending,
            LogLevel.Debug);

        foreach (var part in game.Participations)
            await participationRepository.DeleteParticipationWriteUp(part, token);
    }

    public Task<Game[]> GetGames(int count, int skip, CancellationToken token) =>
        Context.Games.OrderByDescending(g => g.Id).Skip(skip).Take(count).ToArrayAsync(token);

    // By xfoxfu & GZTimeWalker @ 2022/04/03
    // Refactored by GZTimeWalker @ 2024/08/31
    public async Task<ScoreboardModel> GenScoreboard(Game game, CancellationToken token = default)
    {
        Dictionary<int, ScoreboardItem> items; // participant id -> scoreboard item
        Dictionary<int, ChallengeInfo> challenges;
        Dictionary<int, DivisionItem> divisions;
        List<ChallengeItem> submissions;

        // 0. Begin transaction
        await using (var trans = await Context.Database.BeginTransactionAsync(token))
        {
            // 1. Fetch all divisions for this game
            divisions = await Context.Divisions.AsNoTracking().IgnoreAutoIncludes()
                .Where(d => d.GameId == game.Id)
                .Include(d => d.ChallengeConfigs)
                .Select(d => new DivisionItem
                {
                    Id = d.Id,
                    Name = d.Name,
                    DefaultPermissions = d.DefaultPermissions,
                    ChallengeConfigs = d.ChallengeConfigs.Select(c => new
                        DivisionChallengeItem
                    { Permissions = c.Permissions }
                    ).ToDictionary(c => c.ChallengeId)
                })
                .ToDictionaryAsync(d => d.Id, token);

            // 2. Fetch all teams with their members from Participations, into ScoreboardItem
            items = await Context.Participations
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .Where(p => p.GameId == game.Id && p.Status == ParticipationStatus.Accepted)
                .Include(p => p.Team)
                .Select(p => new ScoreboardItem
                {
                    Id = p.Team.Id,
                    Bio = p.Team.Bio,
                    Name = p.Team.Name,
                    Avatar = p.Team.AvatarUrl,
                    DivisionId = p.DivisionId,
                    ParticipantId = p.Id,
                    TeamInfo = p.Team,
                    // pending fields: SolvedChallenges
                    Rank = 0,
                    LastSubmissionTime = DateTimeOffset.MinValue
                    // update: only store accepted challenges
                }).ToDictionaryAsync(i => i.ParticipantId, token);

            // 3. Fetch all challenges from GameChallenges, into ChallengeInfo
            challenges = await Context.GameChallenges
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .Where(c => c.GameId == game.Id && c.IsEnabled)
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Title)
                .Select(c => new ChallengeInfo
                {
                    Id = c.Id,
                    Title = c.Title,
                    Category = c.Category,
                    Score = c.CurrentScore,
                    SolvedCount = c.AcceptedCount,
                    DisableBloodBonus = c.DisableBloodBonus
                    // pending fields: Bloods
                }).ToDictionaryAsync(c => c.Id, token);

            // 4. fetch all needed submissions into a list of ChallengeItem
            //    **take only the first accepted submission for each challenge & team**
            submissions = await Context.Submissions
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .Include(s => s.User)
                .Include(s => s.GameChallenge)
                .Where(s => s.Status == AnswerResult.Accepted
                            && s.GameId == game.Id
                            && s.GameChallenge != null
                            && s.GameChallenge.IsEnabled
                            && s.SubmitTimeUtc < game.EndTimeUtc)
                .GroupBy(s => new { s.ChallengeId, s.ParticipationId })
                .Where(g => g.Any())
                .Select(g =>
                    g.OrderBy(s => s.SubmitTimeUtc)
                        .Take(1)
                        .Select(s =>
                            new ChallengeItem
                            {
                                Id = s.ChallengeId,
                                UserName = s.UserName,
                                SubmitTimeUtc = s.SubmitTimeUtc,
                                ParticipantId = s.ParticipationId,
                                // pending fields
                                Score = 0,
                                Type = SubmissionType.Normal
                            }
                        )
                        .First()
                )
                .ToListAsync(token);

            await trans.CommitAsync(token);
        }

        // 5. sort challenge items by submit time, and update the Score and Type fields
        var noBonus = game.BloodBonus.NoBonus;

        float[] bloodFactors =
        [
            game.BloodBonus.FirstBloodFactor,
            game.BloodBonus.SecondBloodFactor,
            game.BloodBonus.ThirdBloodFactor
        ];

        foreach (var item in submissions.OrderBy(s => s.SubmitTimeUtc))
        {
            // skip if the team is not in the scoreboard
            if (!items.TryGetValue(item.ParticipantId, out var scoreboardItem))
                continue;

            var challenge = challenges[item.Id];

            DivisionItem? division = scoreboardItem.DivisionId is { } div ? divisions.GetValueOrDefault(div) : null;

            // 5.1. generate bloods
            if (challenge is { DisableBloodBonus: false, Bloods.Count: < 3 } &&
                IsDivisionAllowed(division, GamePermission.GetBlood, item.Id))
            {
                item.Type = challenge.Bloods.Count switch
                {
                    0 => SubmissionType.FirstBlood,
                    1 => SubmissionType.SecondBlood,
                    2 => SubmissionType.ThirdBlood,
                    _ => throw new UnreachableException()
                };
                challenge.Bloods.Add(new Blood
                {
                    Id = scoreboardItem.Id,
                    Avatar = scoreboardItem.Avatar,
                    Name = scoreboardItem.Name,
                    SubmitTimeUtc = item.SubmitTimeUtc
                });
            }

            // 5.2. update score
            if (IsDivisionAllowed(division, GamePermission.GetScore, item.Id))
            {
                item.Score = noBonus
                    ? item.Type switch
                    {
                        SubmissionType.Unaccepted => throw new UnreachableException(),
                        _ => challenge.Score
                    }
                    : item.Type switch
                    {
                        SubmissionType.Unaccepted => throw new UnreachableException(),
                        SubmissionType.FirstBlood => Convert.ToInt32(challenge.Score * bloodFactors[0]),
                        SubmissionType.SecondBlood => Convert.ToInt32(challenge.Score * bloodFactors[1]),
                        SubmissionType.ThirdBlood => Convert.ToInt32(challenge.Score * bloodFactors[2]),
                        SubmissionType.Normal => challenge.Score,
                        _ => throw new ArgumentException(nameof(item.Type))
                    };
            }
            else
            {
                item.Score = 0;
            }

            // 5.3. update scoreboard item
            scoreboardItem.SolvedChallenges.Add(item);
            scoreboardItem.Score += item.Score;
            scoreboardItem.LastSubmissionTime = item.SubmitTimeUtc;
        }

        // 6. sort scoreboard items by score and last submission time
        items = items.Values
            .OrderByDescending(i => i.Score)
            .ThenBy(i => i.LastSubmissionTime)
            .ToDictionary(i => i.Id); // team id -> scoreboard item

        // 7. update rank and organization rank
        var currentRank = 1;
        Dictionary<int, int> ranks = [];
        Dictionary<string, HashSet<int>> topTeams = new() { ["all"] = [] }; // division id -> list of team ids

        foreach (var item in items.Values)
        {
            DivisionItem? division = item.DivisionId is { } div ? divisions.GetValueOrDefault(div) : null;

            if (division is null || IsDivisionAllowed(division, GamePermission.RankOverall))
            {
                item.Rank = currentRank++;

                if (item.Rank <= 10)
                    topTeams["all"].Add(item.Id);
            }

            if (division is null)
                continue;

            if (ranks.TryGetValue(division.Id, out var rank))
            {
                item.DivisionRank = rank + 1;
                ranks[division.Id]++;
                if (item.DivisionRank <= 10)
                    topTeams[division.Name].Add(item.Id);
            }
            else
            {
                item.DivisionRank = 1;
                ranks[division.Id] = 1;
                topTeams[division.Name] = [item.Id];
            }
        }

        // 7. generate top timelines by solved challenges
        var timelines = topTeams.ToDictionary(
            i => i.Key,
            i => i.Value.Select(tid =>
                {
                    var item = items[tid];
                    return new TopTimeLine
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Items = item.SolvedChallenges
                            .OrderBy(c => c.SubmitTimeUtc)
                            .Aggregate(new List<TimeLine>(), (acc, c) =>
                            {
                                var last = acc.LastOrDefault();
                                acc.Add(new TimeLine { Score = (last?.Score ?? 0) + c.Score, Time = c.SubmitTimeUtc });
                                return acc;
                            })
                    };
                }
            )
        );

        // 8. construct the final scoreboard model
        var challengesDict = challenges
            .Values
            .GroupBy(c => c.Category)
            .ToDictionary(c => c.Key, c => c.AsEnumerable());

        return new()
        {
            Challenges = challengesDict,
            Items = items,
            Divisions = divisions,
            TimeLines = timelines,
            BloodBonusValue = game.BloodBonus.Val
        };
    }

    static bool IsDivisionAllowed(DivisionItem? division, GamePermission permission, int? challengeId = null)
    {
        if (division is null)
            return true;

        return challengeId is { } id && division.ChallengeConfigs.TryGetValue(id, out var config)
            ? config.Permissions.HasFlag(permission)
            : division.DefaultPermissions.HasFlag(permission);
    }
}
