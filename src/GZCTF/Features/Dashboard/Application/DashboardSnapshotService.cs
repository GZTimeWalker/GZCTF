using GZCTF.Features.Dashboard.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.Dashboard.Application;

public sealed record DashboardSnapshotResponse(
    Guid DashboardId,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<DashboardCohortChoice> Cohorts,
    IReadOnlyList<DashboardMemberSeries> Members,
    IReadOnlyList<DashboardLeaderboardEntry> Leaderboard);

public sealed record DashboardCohortChoice(Guid Id, string Name);
public sealed record DashboardMemberSeries(string UserName, Guid? CohortId, IReadOnlyList<DashboardPoint> Points, int UniqueSolvedCount);
public sealed record DashboardPoint(DateOnly Date, int Value);
public sealed record DashboardLeaderboardEntry(string UserName, int UniqueSolvedCount, int Rank);

public sealed class DashboardSnapshotService(AppDbContext db, DashboardCache cache)
{
    public async Task<DashboardSnapshotResponse?> GetAsync(
        Guid dashboardId, Guid? cohortId, string? search, CancellationToken token = default)
    {
        var key = $"dashboard:{dashboardId}:{cohortId}:{search?.Trim().ToUpperInvariant()}";
        if (cache.TryGet(key, out DashboardSnapshotResponse? cached)) return cached;

        var dashboard = await db.Dashboards.AsNoTracking().SingleOrDefaultAsync(item => item.Id == dashboardId, token);
        if (dashboard is null || !dashboard.IsEnabled) return null;
        var cohorts = await db.Cohorts.AsNoTracking().Where(item => item.IsActive)
            .OrderBy(item => item.Name).Select(item => new DashboardCohortChoice(item.Id, item.Name)).ToArrayAsync(token);
        var usersQuery = db.Users.AsNoTracking().Where(item => item.UserName != null);
        if (cohortId is not null) usersQuery = usersQuery.Where(item => item.CohortId == cohortId);
        if (!string.IsNullOrWhiteSpace(search)) usersQuery = usersQuery.Where(item => EF.Functions.ILike(item.UserName!, $"%{search.Trim()}%"));
        var users = await usersQuery.OrderBy(item => item.UserName).Select(item => new { item.Id, item.UserName, item.CohortId }).ToArrayAsync(token);
        var ids = users.Select(item => item.Id).ToArray();
        var progress = await db.ChallengeProgress.AsNoTracking().Where(item => ids.Contains(item.UserId))
            .Select(item => new { item.UserId, item.SolvedAtUtc }).ToArrayAsync(token);
        var daily = await db.LearnerDailySolveStats.AsNoTracking().Where(item => ids.Contains(item.UserId))
            .Select(item => new { item.UserId, item.Date, item.SolveCount }).ToArrayAsync(token);

        var members = users.Select(user =>
        {
            var userDaily = daily.Where(item => item.UserId == user.Id).OrderBy(item => item.Date).ToArray();
            var points = Cumulative(userDaily.Select(item => (item.Date, item.SolveCount)));
            var count = progress.Count(item => item.UserId == user.Id);
            return new DashboardMemberSeries(user.UserName!, user.CohortId, points, count);
        }).ToArray();
        var firstSolve = progress.GroupBy(item => item.UserId).ToDictionary(group => group.Key, group => group.Min(item => item.SolvedAtUtc));
        var ranking = users.Select(user => new { User = user, Count = progress.Count(item => item.UserId == user.Id), First = firstSolve.GetValueOrDefault(user.Id, DateTimeOffset.MaxValue) })
            .OrderByDescending(item => item.Count).ThenBy(item => item.First).ThenBy(item => item.User.UserName, StringComparer.Ordinal)
            .Take(dashboard.TopCount is 20 ? 20 : 10).Select((item, index) => new DashboardLeaderboardEntry(item.User.UserName!, item.Count, index + 1)).ToArray();
        var response = new DashboardSnapshotResponse(dashboardId, DateTimeOffset.UtcNow, cohorts, members, ranking);
        cache.Set(key, response);
        return response;
    }

    private static IReadOnlyList<DashboardPoint> Cumulative(IEnumerable<(DateOnly Date, int Count)> values)
    {
        var result = new List<DashboardPoint>();
        var cumulative = 0;
        foreach (var value in values)
        {
            cumulative += value.Count;
            result.Add(new DashboardPoint(value.Date, cumulative));
        }
        if (result.Count <= 120) return result;
        var sampled = new List<DashboardPoint>();
        for (var index = 0; index < result.Count; index += 7) sampled.Add(result[index]);
        if (sampled[^1].Date != result[^1].Date) sampled.Add(result[^1]);
        return sampled.TakeLast(120).ToArray();
    }
}
