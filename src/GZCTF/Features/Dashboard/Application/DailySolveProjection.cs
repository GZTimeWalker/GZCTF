using System.Data;
using GZCTF.Features.Dashboard.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.Dashboard.Application;

public sealed record DailySolveRebuildResult(int InsertedRows, DateTimeOffset? MaximumSourceTimestamp);

public sealed class DailySolveProjection(AppDbContext db)
{
    public async Task RecordFirstSolveAsync(Guid userId, DateOnly date, CancellationToken token = default)
    {
        var row = await db.LearnerDailySolveStats.SingleOrDefaultAsync(
            item => item.UserId == userId && item.Date == date, token);
        if (row is null)
        {
            db.LearnerDailySolveStats.Add(new LearnerDailySolveStat
            {
                UserId = userId,
                Date = date,
                SolveCount = 1,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        else
        {
            row.SolveCount++;
            row.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    public async Task<DailySolveRebuildResult> RebuildAsync(CancellationToken token = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        var source = await db.ChallengeProgress.AsNoTracking()
            .Select(item => new { item.UserId, item.SolvedAtUtc })
            .ToArrayAsync(token);
        var groups = source.GroupBy(item => new { item.UserId, Date = DateOnly.FromDateTime(item.SolvedAtUtc.UtcDateTime) })
            .Select(group => new LearnerDailySolveStat
            {
                UserId = group.Key.UserId,
                Date = group.Key.Date,
                SolveCount = group.Count(),
                UpdatedAtUtc = DateTimeOffset.UtcNow
            }).ToArray();

        await db.LearnerDailySolveStats.ExecuteDeleteAsync(token);
        await db.LearnerDailySolveStats.AddRangeAsync(groups, token);
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return new DailySolveRebuildResult(groups.Length, source.Select(item => item.SolvedAtUtc).DefaultIfEmpty().Max());
    }
}
