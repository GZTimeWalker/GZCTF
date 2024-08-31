using System.Diagnostics;
using GZCTF.Extensions;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Repositories;

public class GameRepository(
    IDistributedCache cache,
    ITeamRepository teamRepository,
    IGameChallengeRepository challengeRepository,
    IParticipationRepository participationRepository,
    IConfiguration configuration,
    ILogger<GameRepository> logger,
    AppDbContext context) : RepositoryBase(context), IGameRepository
{
    readonly byte[]? _xorKey = configuration["XorKey"]?.ToUTF8Bytes();

    public override Task<int> CountAsync(CancellationToken token = default) => Context.Games.CountAsync(token);

    public async Task<Game?> CreateGame(Game game, CancellationToken token = default)
    {
        game.GenerateKeyPair(_xorKey);

        if (_xorKey is null)
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.GameRepository_XorKeyNotConfigured)],
                TaskStatus.Pending,
                LogLevel.Warning);

        await Context.AddAsync(game, token);
        await SaveAsync(token);
        return game;
    }

    public string GetToken(Game game, Team team) => $"{team.Id}:{game.Sign($"GZCTF_TEAM_{team.Id}", _xorKey)}";

    public Task<Game?> GetGameById(int id, CancellationToken token = default) =>
        Context.Games.FirstOrDefaultAsync(x => x.Id == id, token);

    public Task<int[]> GetUpcomingGames(CancellationToken token = default) =>
        Context.Games.Where(g => g.StartTimeUtc > DateTime.UtcNow
                                 && g.StartTimeUtc - DateTime.UtcNow < TimeSpan.FromMinutes(8))
            .OrderBy(g => g.StartTimeUtc).Select(g => g.Id).ToArrayAsync(token);

    public async Task<BasicGameInfoModel[]> GetBasicGameInfo(int count = 10, int skip = 0,
        CancellationToken token = default) =>
        await cache.GetOrCreateAsync(logger, CacheKey.BasicGameInfo, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
            return Context.Games.Where(g => !g.Hidden)
                .OrderByDescending(g => g.StartTimeUtc).Skip(skip).Take(count)
                .Select(g => BasicGameInfoModel.FromGame(g)).ToArrayAsync(token);
        }, token);

    public Task<ScoreboardModel> GetScoreboard(Game game, CancellationToken token = default) =>
        cache.GetOrCreateAsync(logger, CacheKey.ScoreBoard(game.Id), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7);
            return GenScoreboard(game, token);
        }, token);

    public async Task<ScoreboardModel> GetScoreboardWithMembers(Game game, CancellationToken token = default)
    {
        // In most cases, we can get the scoreboard from the cache
        ScoreboardModel scoreboard = await GetScoreboard(game, token);

        foreach (ScoreboardItem item in scoreboard.Items.Values)
            item.TeamInfo = await teamRepository.GetTeamById(item.Id, token);

        return scoreboard;
    }

    public async Task<TaskStatus> DeleteGame(Game game, CancellationToken token = default)
    {
        IDbContextTransaction trans = await BeginTransactionAsync(token);

        try
        {
            var count = await Context.GameChallenges.Where(i => i.Game == game).CountAsync(token);
            logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.GameRepository_GameDeletionChallenges), game.Title,
                    count], TaskStatus.Pending,
                LogLevel.Debug
            );

            foreach (GameChallenge chal in Context.GameChallenges.Where(c => c.Game == game))
                await challengeRepository.RemoveChallenge(chal, token);

            count = await Context.Participations.Where(i => i.Game == game).CountAsync(token);

            logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.GameRepository_GameDeletionTeams), game.Title, count],
                TaskStatus.Pending, LogLevel.Debug
            );

            foreach (Participation part in Context.Participations.Where(p => p.Game == game))
                await participationRepository.RemoveParticipation(part, token);

            Context.Remove(game);

            await SaveAsync(token);
            await trans.CommitAsync(token);

            await cache.RemoveAsync(CacheKey.BasicGameInfo, token);
            await cache.RemoveAsync(CacheKey.ScoreBoard(game.Id), token);

            return TaskStatus.Success;
        }
        catch (Exception e)
        {
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.Game_DeletionFailed)], TaskStatus.Pending,
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
            Program.StaticLocalizer[nameof(Resources.Program.GameRepository_GameDeletionTeams), game.Title,
                game.Participations.Count],
            TaskStatus.Pending,
            LogLevel.Debug);

        foreach (Participation part in game.Participations)
            await participationRepository.DeleteParticipationWriteUp(part, token);
    }

    public Task<Game[]> GetGames(int count, int skip, CancellationToken token) =>
        Context.Games.OrderByDescending(g => g.Id).Skip(skip).Take(count).ToArrayAsync(token);

    public void FlushGameInfoCache() => cache.Remove(CacheKey.BasicGameInfo);

    #region Generate Scoreboard

    // By xfoxfu & GZTimeWalker @ 2022/04/03
    // Refactored by GZTimeWalker @ 2024/08/31
    public async Task<ScoreboardModel> GenScoreboard(Game game, CancellationToken token = default)
    {
        Dictionary<int, ScoreboardItem> items; // participant id -> scoreboard item
        Dictionary<int, ChallengeInfo> challenges;
        List<ChallengeItem> submissions;

        // 0. Begin transaction
        await using (var trans = await Context.Database.BeginTransactionAsync(token))
        {
            // 1. Fetch all teams with their members from Participations, into ScoreboardItem
            items = await Context.Participations
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .Where(p => p.GameId == game.Id)
                .Include(p => p.Team)
                .Select(p => new ScoreboardItem
                {
                    Id = p.Team.Id,
                    Bio = p.Team.Bio,
                    Name = p.Team.Name,
                    Avatar = p.Team.AvatarUrl,
                    Organization = p.Organization,
                    ParticipantId = p.Id,
                    TeamInfo = p.Team,
                    // pending fields: SolvedChallenges
                    Rank = 0,
                    LastSubmissionTime = DateTimeOffset.MinValue,
                    // update: only store accepted challenges
                }).ToDictionaryAsync(i => i.ParticipantId, token);

            // 2. Fetch all challenges from GameChallenges, into ChallengeInfo
            challenges = await Context.GameChallenges
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .Where(c => c.GameId == game.Id && c.IsEnabled)
                .Select(c => new ChallengeInfo
                {
                    Id = c.Id,
                    Title = c.Title,
                    Tag = c.Tag,
                    Score = c.CurrentScore,
                    SolvedCount = c.AcceptedCount,
                    // pending fields: Bloods
                }).ToDictionaryAsync(c => c.Id, token);

            // 3. fetch all needed submissions into a list of ChallengeItem
            //    **take only the first accepted submission for each challenge & team**
            submissions = await Context.Submissions
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .Include(s => s.User)
                .Include(s => s.GameChallenge)
                .Where(s => s.Status == AnswerResult.Accepted
                            && s.GameId == game.Id
                            && s.GameChallenge.IsEnabled
                            && s.SubmitTimeUtc < game.EndTimeUtc)
                .GroupBy(s => new { s.ChallengeId, s.ParticipationId })
                .Where(g => g.Any())
                .Select(g =>
                    g.OrderBy(s => s.SubmitTimeUtc)
                        .Take(1)
                        .Select(
                            s =>
                                new ChallengeItem
                                {
                                    Id = s.ChallengeId,
                                    UserName = s.User.UserName,
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

        // 4. sort challenge items by submit time, and update the Score and Type fields
        bool noBonus = game.BloodBonus.NoBonus;

        float[] bloodFactors =
        [
            game.BloodBonus.FirstBloodFactor,
            game.BloodBonus.SecondBloodFactor,
            game.BloodBonus.ThirdBloodFactor
        ];

        foreach (var item in submissions)
        {
            var challenge = challenges[item.Id];
            var scoreboardItem = items[item.ParticipantId];

            // 4.1. generate bloods
            if (challenge.Bloods.Count < 3)
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

            // 4.2. update score
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

            // 4.3. update scoreboard item
            scoreboardItem.SolvedChallenges.Add(item);
            scoreboardItem.Score += item.Score;
            scoreboardItem.LastSubmissionTime = item.SubmitTimeUtc;
        }

        // 5. sort scoreboard items by score and last submission time
        items = items.Values
            .OrderByDescending(i => i.Score)
            .ThenBy(i => i.LastSubmissionTime)
            .ToDictionary(i => i.Id); // team id -> scoreboard item

        // 6. update rank and organization rank
        var ranks = new Dictionary<string, int>();
        var currentRank = 1;
        Dictionary<string, HashSet<int>> orgTeams = new() { ["all"] = [] };
        foreach (var item in items.Values)
        {
            item.Rank = currentRank++;

            if (item.Rank <= 10)
                orgTeams["all"].Add(item.Id);

            if (item.Organization is null)
                continue;

            if (ranks.TryGetValue(item.Organization, out var rank))
            {
                item.OrganizationRank = rank + 1;
                ranks[item.Organization]++;
                orgTeams[item.Organization].Add(item.Id);
            }
            else
            {
                item.OrganizationRank = 1;
                ranks[item.Organization] = 1;
                orgTeams[item.Organization] = [item.Id];
            }
        }

        // 7. generate top timelines by solved challenges
        var timelines = orgTeams.ToDictionary(
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
            .GroupBy(c => c.Tag)
            .ToDictionary(c => c.Key, c => c.AsEnumerable());

        return new()
        {
            Challenges = challengesDict,
            Items = items,
            TimeLines = timelines,
            BloodBonusValue = game.BloodBonus.Val
        };
    }

    #endregion Generate Scoreboard
}

public class ScoreboardCacheHandler : ICacheRequestHandler
{
    public string? CacheKey(CacheRequest request)
        => request.Params.Length switch
        {
            1 => Services.Cache.CacheKey.ScoreBoard(request.Params[0]),
            _ => null
        };

    public async Task<byte[]> Handler(AsyncServiceScope scope, CacheRequest request, CancellationToken token = default)
    {
        if (!int.TryParse(request.Params[0], out var id))
            return [];

        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return [];

        try
        {
            ScoreboardModel scoreboard = await gameRepository.GenScoreboard(game, token);
            return MemoryPackSerializer.Serialize(scoreboard);
        }
        catch (Exception e)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ScoreboardCacheHandler>>();
            logger.LogError(e, "{msg}",
                Program.StaticLocalizer[nameof(Resources.Program.Cache_GenerationFailed), CacheKey(request)!]);
            return [];
        }
    }

    public static CacheRequest MakeCacheRequest(int id) =>
        new(Services.Cache.CacheKey.ScoreBoardBase,
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(14) }, id.ToString());
}
