using GZCTF.Extensions;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Repositories;

public class GameRepository(IDistributedCache cache,
    ITeamRepository teamRepository,
    IChallengeRepository challengeRepository,
    IParticipationRepository participationRepository,
    IConfiguration configuration,
    ILogger<GameRepository> logger,
    AppDbContext context) : RepositoryBase(context), IGameRepository
{
    readonly byte[]? _xorkey = configuration["XorKey"]?.ToUTF8Bytes();

    public override Task<int> CountAsync(CancellationToken token = default) => context.Games.CountAsync(token);

    public async Task<Game?> CreateGame(Game game, CancellationToken token = default)
    {
        game.GenerateKeyPair(_xorkey);

        if (_xorkey is null)
            logger.SystemLog("配置文件中的异或密钥未设置，比赛私钥将会被明文存储至数据库。", TaskStatus.Pending, LogLevel.Warning);

        await context.AddAsync(game, token);
        await SaveAsync(token);
        return game;
    }

    public string GetToken(Game game, Team team) => $"{team.Id}:{game.Sign($"GZCTF_TEAM_{team.Id}", _xorkey)}";

    public Task<Game?> GetGameById(int id, CancellationToken token = default) => context.Games.FirstOrDefaultAsync(x => x.Id == id, token);

    public Task<int[]> GetUpcomingGames(CancellationToken token = default) =>
        context.Games.Where(g => g.StartTimeUTC > DateTime.UtcNow
                                 && g.StartTimeUTC - DateTime.UtcNow < TimeSpan.FromMinutes(5))
            .OrderBy(g => g.StartTimeUTC).Select(g => g.Id).ToArrayAsync(token);

    public async Task<BasicGameInfoModel[]> GetBasicGameInfo(int count = 10, int skip = 0,
        CancellationToken token = default) =>
        await cache.GetOrCreateAsync(logger, CacheKey.BasicGameInfo, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
            return context.Games.Where(g => !g.Hidden)
                .OrderByDescending(g => g.StartTimeUTC).Skip(skip).Take(count)
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

        foreach (ScoreboardItem item in scoreboard.Items)
            item.TeamInfo = await teamRepository.GetTeamById(item.Id, token) ??
                            throw new ArgumentNullException("team", "Team not found");

        return scoreboard;
    }


    public async Task<TaskStatus> DeleteGame(Game game, CancellationToken token = default)
    {
        IDbContextTransaction trans = await BeginTransactionAsync(token);

        try
        {
            await context.Entry(game).Collection(g => g.Challenges).LoadAsync(token);

            logger.SystemLog($"正在清理比赛 {game.Title} 的 {game.Challenges.Count} 个题目的相关附件……", TaskStatus.Pending,
                LogLevel.Debug);

            foreach (Challenge chal in game.Challenges)
                await challengeRepository.RemoveChallenge(chal, token);

            await context.Entry(game).Collection(g => g.Participations).LoadAsync(token);

            logger.SystemLog($"正在清理比赛 {game.Title} 的 {game.Participations.Count} 个队伍相关文件……", TaskStatus.Pending,
                LogLevel.Debug);

            foreach (Participation part in game.Participations)
                await participationRepository.RemoveParticipation(part, token);

            context.Remove(game);

            await SaveAsync(token);
            await trans.CommitAsync(token);

            cache.Remove(CacheKey.BasicGameInfo);
            cache.Remove(CacheKey.ScoreBoard(game.Id));

            return TaskStatus.Success;
        }
        catch
        {
            logger.SystemLog("删除比赛失败，相关文件可能已受损，请重新删除", TaskStatus.Pending, LogLevel.Debug);
            await trans.RollbackAsync(token);

            return TaskStatus.Failed;
        }
    }

    public async Task DeleteAllWriteUps(Game game, CancellationToken token = default)
    {
        await context.Entry(game).Collection(g => g.Participations).LoadAsync(token);

        logger.SystemLog($"正在清理比赛 {game.Title} 的 {game.Participations.Count} 个队伍相关文件……", TaskStatus.Pending,
            LogLevel.Debug);

        foreach (Participation part in game.Participations)
            await participationRepository.DeleteParticipationWriteUp(part, token);
    }

    public Task<Game[]> GetGames(int count, int skip, CancellationToken token) =>
        context.Games.OrderByDescending(g => g.Id).Skip(skip).Take(count).ToArrayAsync(token);

    public void FlushGameInfoCache() => cache.Remove(CacheKey.BasicGameInfo);

    #region Generate Scoreboard

    record Data(Instance Instance, Submission? Submission);

    // By xfoxfu & GZTimeWalker @ 2022/04/03
    public async Task<ScoreboardModel> GenScoreboard(Game game, CancellationToken token = default)
    {
        Data[] data = await FetchData(game, token);
        IDictionary<int, Blood?[]> bloods = GenBloods(data);
        IEnumerable<ScoreboardItem> items = GenScoreboardItems(data, game, bloods);
        return new()
        {
            Challenges = GenChallenges(data, bloods),
            Items = items,
            TimeLines = GenTopTimeLines(items, game),
            BloodBonusValue = game.BloodBonus.Val
        };
    }

    Task<Data[]> FetchData(Game game, CancellationToken token = default) =>
        context.Instances
            .Include(i => i.Challenge)
            .Where(i => i.Challenge.Game == game
                        && i.Challenge.IsEnabled
                        && i.Participation.Status == ParticipationStatus.Accepted)
            .Include(i => i.Participation).ThenInclude(p => p.Team).ThenInclude(t => t.Members)
            .GroupJoin(
                context.Submissions.Where(s => s.Status == AnswerResult.Accepted
                                               && s.SubmitTimeUTC < game.EndTimeUTC),
                i => new { i.ChallengeId, i.ParticipationId },
                s => new { s.ChallengeId, s.ParticipationId },
                (i, s) => new { Instance = i, Submissions = s }
            ).SelectMany(j => j.Submissions.DefaultIfEmpty(),
                (j, s) => new Data(j.Instance, s)
            ).AsSplitQuery().ToArrayAsync(token);

    static IDictionary<int, Blood?[]> GenBloods(Data[] data) =>
        data.GroupBy(j => j.Instance.Challenge).Select(g => new
        {
            g.Key,
            Value = g.GroupBy(c => c.Submission?.TeamId)
                .Where(t => t.Key is not null)
                .Select(c =>
                {
                    Data? s = c.OrderBy(t => t.Submission?.SubmitTimeUTC ?? DateTimeOffset.UtcNow)
                        .FirstOrDefault(s => s.Submission?.Status == AnswerResult.Accepted);

                    return s is null
                        ? null
                        : new Blood
                        {
                            Id = s.Instance.Participation.TeamId,
                            Avatar = s.Instance.Participation.Team.AvatarUrl,
                            Name = s.Instance.Participation.Team.Name,
                            SubmitTimeUTC = s.Submission?.SubmitTimeUTC
                        };
                })
                .Where(t => t is not null)
                .OrderBy(t => t?.SubmitTimeUTC ?? DateTimeOffset.UtcNow)
                .Take(3).ToArray()
        }).ToDictionary(a => a.Key.Id, a => a.Value);

    static Dictionary<ChallengeTag, IEnumerable<ChallengeInfo>> GenChallenges(Data[] data,
        IDictionary<int, Blood?[]> bloods) =>
        data.GroupBy(g => g.Instance.Challenge)
            .Select(c => new ChallengeInfo
            {
                Id = c.Key.Id,
                Title = c.Key.Title,
                Tag = c.Key.Tag,
                Score = c.Key.CurrentScore,
                SolvedCount = c.Key.AcceptedCount, // Concurrence!
                Bloods = bloods[c.Key.Id]
            }).GroupBy(c => c.Tag)
            .OrderBy(i => i.Key)
            .ToDictionary(c => c.Key, c => c.AsEnumerable());

    static IEnumerable<ScoreboardItem> GenScoreboardItems(Data[] data, Game game, IDictionary<int, Blood?[]> bloods)
    {
        Dictionary<string, int> Ranks = new();
        return data.GroupBy(j => j.Instance.Participation)
            .Select(j =>
            {
                IEnumerable<IGrouping<int, Data>> challengeGroup = j.GroupBy(s => s.Instance.ChallengeId);

                return new ScoreboardItem
                {
                    Id = j.Key.Team.Id,
                    Bio = j.Key.Team.Bio,
                    Name = j.Key.Team.Name,
                    Avatar = j.Key.Team.AvatarUrl,
                    Organization = j.Key.Organization,
                    Rank = 0,
                    LastSubmissionTime = j
                        .Where(s => s.Submission?.SubmitTimeUTC < game.EndTimeUTC)
                        .Select(s => s.Submission?.SubmitTimeUTC ?? DateTimeOffset.UtcNow)
                        .OrderBy(t => t).LastOrDefault(game.StartTimeUTC),
                    SolvedCount = challengeGroup.Count(c => c.Any(
                        s => s.Submission?.Status == AnswerResult.Accepted
                             && s.Submission.SubmitTimeUTC < game.EndTimeUTC)),
                    Challenges = challengeGroup
                        .Select(c =>
                        {
                            var cid = c.Key;
                            Data? s = c.OrderBy(s => s.Submission?.SubmitTimeUTC ?? DateTimeOffset.UtcNow)
                                .FirstOrDefault(s => s.Submission?.Status == AnswerResult.Accepted);

                            var status = SubmissionType.Normal;

                            if (s?.Submission is null)
                                status = SubmissionType.Unaccepted;
                            else if (bloods[cid][0] is not null &&
                                     s.Submission.SubmitTimeUTC <= bloods[cid][0]!.SubmitTimeUTC)
                                status = SubmissionType.FirstBlood;
                            else if (bloods[cid][1] is not null &&
                                     s.Submission.SubmitTimeUTC <= bloods[cid][1]!.SubmitTimeUTC)
                                status = SubmissionType.SecondBlood;
                            else if (bloods[cid][2] is not null &&
                                     s.Submission.SubmitTimeUTC <= bloods[cid][2]!.SubmitTimeUTC)
                                status = SubmissionType.ThirdBlood;

                            return new ChallengeItem
                            {
                                Id = cid,
                                Type = status,
                                UserName = s?.Submission?.UserName,
                                SubmitTimeUTC = s?.Submission?.SubmitTimeUTC,
                                Score = s is null ? 0 :
                                    game.BloodBonus.NoBonus ? status switch
                                    {
                                        SubmissionType.Unaccepted => 0,
                                        _ => s.Instance.Challenge.CurrentScore
                                    } : status switch
                                    {
                                        SubmissionType.Unaccepted => 0,
                                        SubmissionType.FirstBlood => Convert.ToInt32(s.Instance.Challenge.CurrentScore *
                                                                                     game.BloodBonus.FirstBloodFactor),
                                        SubmissionType.SecondBlood => Convert.ToInt32(
                                            s.Instance.Challenge.CurrentScore *
                                            game.BloodBonus.SecondBloodFactor),
                                        SubmissionType.ThirdBlood => Convert.ToInt32(s.Instance.Challenge.CurrentScore *
                                                                                     game.BloodBonus.ThirdBloodFactor),
                                        SubmissionType.Normal => s.Instance.Challenge.CurrentScore,
                                        _ => throw new ArgumentException(nameof(status))
                                    }
                            };
                        }).ToList()
                };
            }).OrderByDescending(j => j.Score).ThenBy(j => j.LastSubmissionTime) //成绩倒序，最后提交时间正序
            .Select((j, i) =>
            {
                j.Rank = i + 1;

                if (j.Organization is not null)
                {
                    if (Ranks.TryGetValue(j.Organization, out var rank))
                    {
                        j.OrganizationRank = rank + 1;
                        Ranks[j.Organization]++;
                    }
                    else
                    {
                        j.OrganizationRank = 1;
                        Ranks[j.Organization] = 1;
                    }
                }

                return j;
            }).ToArray();
    }

    static Dictionary<string, IEnumerable<TopTimeLine>> GenTopTimeLines(IEnumerable<ScoreboardItem> items, Game game)
    {
        Dictionary<string, IEnumerable<TopTimeLine>> timelines = game.Organizations is { Count: > 0 }
            ? items
                .GroupBy(i => i.Organization ?? "all")
                .ToDictionary(i => i.Key, i => i.Take(10)
                    .Select(team => new TopTimeLine { Id = team.Id, Name = team.Name, Items = GenTimeLine(team.Challenges) }).ToArray()
                    .AsEnumerable())
            : new();

        timelines["all"] = items.Take(10).Select(team => new TopTimeLine { Id = team.Id, Name = team.Name, Items = GenTimeLine(team.Challenges) })
            .ToArray();

        return timelines;
    }

    static IEnumerable<TimeLine> GenTimeLine(IEnumerable<ChallengeItem> items)
    {
        var score = 0;
        return items.Where(i => i.SubmitTimeUTC is not null)
            .OrderBy(i => i.SubmitTimeUTC)
            .Select(i =>
            {
                score += i.Score;
                return new TimeLine
                {
                    Score = score,
                    Time = i.SubmitTimeUTC!.Value // 此处不为 null
                };
            });
    }

    #endregion Generate Scoreboard
}

public class ScoreboardCacheHandler : ICacheRequestHandler
{
    public string? CacheKey(CacheRequest request)
    {
        if (request.Params.Length != 1)
            return null;

        return Services.Cache.CacheKey.ScoreBoard(request.Params[0]);
    }

    public async Task<byte[]> Handler(AsyncServiceScope scope, CacheRequest request, CancellationToken token = default)
    {
        if (!int.TryParse(request.Params[0], out var id))
            return Array.Empty<byte>();

        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return Array.Empty<byte>();

        ScoreboardModel scoreboard = await gameRepository.GenScoreboard(game, token);
        return MemoryPackSerializer.Serialize(scoreboard);
    }

    public static CacheRequest MakeCacheRequest(int id) =>
        new(Services.Cache.CacheKey.ScoreBoardBase,
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(14) }, id.ToString());
}