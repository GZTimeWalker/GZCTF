using CTFServer.Models;
using CTFServer.Models.Request.Game;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

namespace CTFServer.Repositories;

public class GameRepository : RepositoryBase, IGameRepository
{
    private readonly IMemoryCache cache;
    
    public GameRepository(IMemoryCache memoryCache, AppDbContext _context) : base(_context)
    {
        cache = memoryCache;
    }

    public async Task<Game?> CreateGame(Game game, CancellationToken token = default)
    {
        await context.AddAsync(game, token);
        await context.SaveChangesAsync(token);
        return game;
    }
    
    public Task<Game?> GetGameById(int id, CancellationToken token = default)
        => context.Games.FirstOrDefaultAsync(x => x.Id == id, token);

    public Task<List<Game>> GetGames(int count = 10, int skip = 0, CancellationToken token = default)
        => context.Games.OrderByDescending(g => g.StartTimeUTC).Skip(skip).Take(count).ToListAsync(token);

    public Task<Scoreboard> GetScoreboard(Game game, CancellationToken token = default)
        => cache.GetOrCreateAsync(CacheKey.ScoreBoard(game.Id), entry => GenScoreboard(game, token));

    public Task<int> UpdateGame(Game game, CancellationToken token = default)
    {
        context.Update(game);
        return context.SaveChangesAsync(token);
    }
    

    private record Data(Instance Instance, Submission? Submission);

    private async Task<Scoreboard> GenScoreboard(Game game, CancellationToken token = default)
    {
        var data = await FetchData(game, token);
        return new()
        {
            Challenges = GenChallenges(data),
            Items = GenScoreboardItems(data),
            TimeLine = GenTopTimeLines(data)
        };
    }

    // By xfoxfu & GZTimeWalker @ 2022/04/03
    private Task<Data[]> FetchData(Game game, CancellationToken token = default)
        => context.Instances
            .Where(i => i.Game == game)
            .Include(i => i.Challenge)
            .Include(i => i.Participation)
                .ThenInclude(t => t.Team)
            .GroupJoin(
                context.Submissions.Where(s => s.Status == AnswerResult.Accepted),
                i => new { i.ChallengeId, i.ParticipationId },
                s => new { s.ChallengeId, s.ParticipationId },
                (i, s) => new { Instance = i, Submissions = s}
            ).SelectMany(j => j.Submissions.DefaultIfEmpty(), 
                (j, s) => new Data(j.Instance, s)
            ).ToArrayAsync(token);

    private static IDictionary<string, IEnumerable<ChallengeInfo>> GenChallenges(Data[] data)
        => data.GroupBy(g => g.Instance.Challenge)
            .Select(c => new ChallengeInfo
            {
                Id = c.Key.Id,
                Title = c.Key.Title,
                Tag = c.Key.Tag,
                Score = c.Key.CurrentScore,
            }).GroupBy(c => c.Tag)
            .ToDictionary(c => c.Key.ToString(), c => c.AsEnumerable());

    private static IEnumerable<ScoreboardItem> GenScoreboardItems(Data[] data)
    {
        var bloods = data.GroupBy(j => j.Instance.Challenge)
            .Select(g => new
            {
                Key = g.Key,
                Value = g.Select(a => a.Submission?.SubmitTimeUTC ?? DateTimeOffset.UtcNow).OrderBy(t => t).Take(3).ToArray(),
            }).ToDictionary(a => a.Key, a => a.Value);

        return data.GroupBy(j => j.Instance.Participation)
            .Select(j =>
            {
                var team = j.Key;

                return new
                {
                    Item = new ScoreboardItem
                    {
                        Id = team.Team.Id,
                        Name = team.Team.Name,
                        Rank = 0,
                        SolvedCount = j.Count(s => s.Submission is not null),
                        Challenges = j.Select(s =>
                        {
                            SubmissionType status = SubmissionType.Normal;

                            if (s.Submission is null)
                                status = SubmissionType.Unaccepted;
                            else if (s.Submission.SubmitTimeUTC <= bloods[s.Instance.Challenge][0])
                                status = SubmissionType.FirstBlood;
                            else if (s.Submission.SubmitTimeUTC <= bloods[s.Instance.Challenge][1])
                                status = SubmissionType.SecondBlood;
                            else if (s.Submission.SubmitTimeUTC <= bloods[s.Instance.Challenge][2])
                                status = SubmissionType.ThirdBlood;

                            return new ChallengeItem
                            {
                                Id = s.Instance.Challenge.Id,
                                Type = status,
                                Score = status switch
                                {
                                    SubmissionType.Unaccepted => 0,
                                    SubmissionType.FirstBlood => Convert.ToInt32(s.Instance.Challenge.CurrentScore * 1.05),
                                    SubmissionType.SecondBlood => Convert.ToInt32(s.Instance.Challenge.CurrentScore * 1.03),
                                    SubmissionType.ThirdBlood => Convert.ToInt32(s.Instance.Challenge.CurrentScore * 1.01),
                                    SubmissionType.Normal => s.Instance.Challenge.CurrentScore,
                                    _ => throw new ArgumentException(nameof(status))
                                }
                            };
                        })
                        .ToList()
                    },
                    LastSubmissionTime = j.Select(s => s.Submission?.SubmitTimeUTC ?? DateTimeOffset.UtcNow),
                };
            }).OrderBy(j => (-j.Item.Score, j.LastSubmissionTime)) //成绩倒序，最后提交时间正序
            .Select((j, i) =>
            {
                j.Item.Rank = i + 1;
                return j.Item;
            }).ToArray();
    }

    private static IEnumerable<TopTimeLine> GenTopTimeLines(Data[] data)
        => data.GroupBy(g => g.Instance.Participation)
            .Select(c => new TopTimeLine
            {
                 Id = c.Key.Team.Id,
                 Name = c.Key.Team.Name,
                 Items = GetTimeLine(c)
            }).ToArray();

    private static IEnumerable<TimeLine> GetTimeLine(IEnumerable<Data> data)
    {
        var score = 0;
        List<TimeLine> timeline = new();
        foreach(var item in data.OrderBy(s => s.Submission!.SubmitTimeUTC))
        {
            score += item.Instance.Challenge.CurrentScore;
            timeline.Add(new()
            {
                Time = item.Submission!.SubmitTimeUTC,
                Score = score
            });
        }
        return timeline.ToArray();
    }
}
