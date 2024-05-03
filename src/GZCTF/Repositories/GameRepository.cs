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
                                 && g.StartTimeUtc - DateTime.UtcNow < TimeSpan.FromMinutes(5))
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

    record Data(GameInstance GameInstance, Submission? Submission);

    // By xfoxfu & GZTimeWalker @ 2022/04/03
    public async Task<ScoreboardModel> GenScoreboard(Game game, CancellationToken token = default)
    {
        Data[] data = await FetchData(game, token);
        Dictionary<int, Blood?[]> bloods = GenBloods(data);
        Dictionary<int, ScoreboardItem> items = GenScoreboardItems(data, game, bloods);
        return new()
        {
            Challenges = GenChallenges(data, bloods),
            Items = items,
            TimeLines = GenTopTimeLines(items.Values, game),
            BloodBonusValue = game.BloodBonus.Val
        };
    }

    Task<Data[]> FetchData(Game game, CancellationToken token = default) =>
        Context.GameInstances
            .Include(i => i.Challenge)
            .Where(i => i.Challenge.Game == game
                        && i.Challenge.IsEnabled
                        && i.Participation.Status == ParticipationStatus.Accepted)
            .Include(i => i.Participation)
            .ThenInclude(p => p.Team).ThenInclude(t => t.Members)
            .GroupJoin(
                Context.Submissions.Where(s => s.Status == AnswerResult.Accepted
                                               && s.SubmitTimeUtc < game.EndTimeUtc),
                i => new { i.ChallengeId, i.ParticipationId },
                s => new { s.ChallengeId, s.ParticipationId },
                (i, s) => new { Instance = i, Submissions = s }
            ).SelectMany(j => j.Submissions.DefaultIfEmpty(),
                (j, s) => new Data(j.Instance, s)
            ).AsSplitQuery().ToArrayAsync(token);

    static Dictionary<int, Blood?[]> GenBloods(Data[] data) =>
        data.GroupBy(j => j.GameInstance.Challenge).Select(g => new
        {
            g.Key,
            Value = g.GroupBy(c => c.Submission?.TeamId)
                .Where(t => t.Key is not null)
                .Select(c =>
                {
                    Data? s = c.OrderBy(t => t.Submission?.SubmitTimeUtc ?? DateTimeOffset.UtcNow)
                        .FirstOrDefault(s => s.Submission?.Status == AnswerResult.Accepted);

                    return s is null
                        ? null
                        : new Blood
                        {
                            Id = s.GameInstance.Participation.TeamId,
                            Avatar = s.GameInstance.Participation.Team.AvatarUrl,
                            Name = s.GameInstance.Participation.Team.Name,
                            SubmitTimeUtc = s.Submission?.SubmitTimeUtc
                        };
                })
                .Where(t => t is not null)
                .OrderBy(t => t?.SubmitTimeUtc ?? DateTimeOffset.UtcNow)
                .Take(3).ToArray()
        }).ToDictionary(a => a.Key.Id, a => a.Value);

    static Dictionary<ChallengeTag, IEnumerable<ChallengeInfo>> GenChallenges(Data[] data,
        Dictionary<int, Blood?[]> bloods) =>
        data.GroupBy(g => g.GameInstance.Challenge)
            .Select(c => new ChallengeInfo
            {
                Id = c.Key.Id,
                Title = c.Key.Title,
                Tag = c.Key.Tag,
                Score = c.Key.CurrentScore,
                SolvedCount = c.Key.AcceptedCount,
                Bloods = bloods[c.Key.Id]
            }).GroupBy(c => c.Tag)
            .OrderBy(i => i.Key)
            .ToDictionary(c => c.Key, c => c.AsEnumerable());

    static Dictionary<int, ScoreboardItem> GenScoreboardItems(Data[] data, Game game, Dictionary<int, Blood?[]> bloods)
    {
        Dictionary<string, int> ranks = [];
        return data.GroupBy(j => j.GameInstance.Participation)
            .Select(j =>
            {
                IEnumerable<IGrouping<int, Data>> challengeGroup = j.GroupBy(s => s.GameInstance.ChallengeId);

                return new ScoreboardItem
                {
                    Id = j.Key.Team.Id,
                    Bio = j.Key.Team.Bio,
                    Name = j.Key.Team.Name,
                    Avatar = j.Key.Team.AvatarUrl,
                    Organization = j.Key.Organization,
                    Rank = 0,
                    LastSubmissionTime = j
                        .Where(s => s.Submission?.SubmitTimeUtc < game.EndTimeUtc)
                        .Select(s => s.Submission?.SubmitTimeUtc ?? DateTimeOffset.UtcNow)
                        .OrderBy(t => t).LastOrDefault(game.StartTimeUtc),
                    SolvedCount = challengeGroup.Count(c => c.Any(
                        s => s.Submission?.Status == AnswerResult.Accepted
                             && s.Submission.SubmitTimeUtc < game.EndTimeUtc)),
                    Challenges = challengeGroup
                        .Select(c =>
                        {
                            var cid = c.Key;
                            Data? s = c.OrderBy(s => s.Submission?.SubmitTimeUtc ?? DateTimeOffset.UtcNow)
                                .FirstOrDefault(s => s.Submission?.Status == AnswerResult.Accepted);

                            var status = SubmissionType.Normal;

                            if (s?.Submission is null)
                                status = SubmissionType.Unaccepted;
                            else if (bloods[cid][0] is not null &&
                                     s.Submission.SubmitTimeUtc <= bloods[cid][0]!.SubmitTimeUtc)
                                status = SubmissionType.FirstBlood;
                            else if (bloods[cid][1] is not null &&
                                     s.Submission.SubmitTimeUtc <= bloods[cid][1]!.SubmitTimeUtc)
                                status = SubmissionType.SecondBlood;
                            else if (bloods[cid][2] is not null &&
                                     s.Submission.SubmitTimeUtc <= bloods[cid][2]!.SubmitTimeUtc)
                                status = SubmissionType.ThirdBlood;

                            return new ChallengeItem
                            {
                                Id = cid,
                                Type = status,
                                UserName = s?.Submission?.UserName,
                                SubmitTimeUtc = s?.Submission?.SubmitTimeUtc,
                                Score = s is null ? 0 :
                                    game.BloodBonus.NoBonus ? status switch
                                    {
                                        SubmissionType.Unaccepted => 0,
                                        _ => s.GameInstance.Challenge.CurrentScore
                                    } : status switch
                                    {
                                        SubmissionType.Unaccepted => 0,
                                        SubmissionType.FirstBlood => Convert.ToInt32(
                                            s.GameInstance.Challenge.CurrentScore *
                                            game.BloodBonus.FirstBloodFactor),
                                        SubmissionType.SecondBlood => Convert.ToInt32(
                                            s.GameInstance.Challenge.CurrentScore *
                                            game.BloodBonus.SecondBloodFactor),
                                        SubmissionType.ThirdBlood => Convert.ToInt32(
                                            s.GameInstance.Challenge.CurrentScore *
                                            game.BloodBonus.ThirdBloodFactor),
                                        SubmissionType.Normal => s.GameInstance.Challenge.CurrentScore,
                                        _ => throw new ArgumentException(nameof(status))
                                    }
                            };
                        }).ToList()
                };
            }).OrderByDescending(j => j.Score).ThenBy(j => j.LastSubmissionTime) //成绩倒序，最后提交时间正序
            .Select((j, i) =>
            {
                j.Rank = i + 1;

                if (j.Organization is null)
                    return j;

                if (ranks.TryGetValue(j.Organization, out var rank))
                {
                    j.OrganizationRank = rank + 1;
                    ranks[j.Organization]++;
                }
                else
                {
                    j.OrganizationRank = 1;
                    ranks[j.Organization] = 1;
                }

                return j;
            }).ToDictionary(d => d.Id);
    }

    static Dictionary<string, IEnumerable<TopTimeLine>> GenTopTimeLines(IEnumerable<ScoreboardItem> items, Game game)
    {
        Dictionary<string, IEnumerable<TopTimeLine>> timelines = game.Organizations is { Count: > 0 }
            ? items
                .GroupBy(i => i.Organization ?? "all")
                .ToDictionary(i => i.Key, i => i.Take(10)
                    .Select(team =>
                        new TopTimeLine { Id = team.Id, Name = team.Name, Items = GenTimeLine(team.Challenges) })
                    .ToArray()
                    .AsEnumerable())
            : [];

        timelines["all"] = items.Take(10)
            .Select(team => new TopTimeLine { Id = team.Id, Name = team.Name, Items = GenTimeLine(team.Challenges) })
            .ToArray();

        return timelines;
    }

    static IEnumerable<TimeLine> GenTimeLine(IEnumerable<ChallengeItem> items)
    {
        var score = 0;
        return items.Where(i => i.SubmitTimeUtc is not null)
            .OrderBy(i => i.SubmitTimeUtc)
            .Select(i =>
            {
                score += i.Score;
                return new TimeLine
                {
                    Score = score,
                    Time = i.SubmitTimeUtc!.Value // 此处不为 null
                };
            });
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

        ScoreboardModel scoreboard = await gameRepository.GenScoreboard(game, token);
        return MemoryPackSerializer.Serialize(scoreboard);
    }

    public static CacheRequest MakeCacheRequest(int id) =>
        new(Services.Cache.CacheKey.ScoreBoardBase,
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(14) }, id.ToString());
}
