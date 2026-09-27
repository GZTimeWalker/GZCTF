using GZCTF.Features.SkillTrees.Migration;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Decommission;

[Collection(nameof(IntegrationTestCollection))]
public sealed class LegacyTableRetentionTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Legacy_entity_sets_remain_available_for_migration_audit()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotNull(db.Games);
        Assert.NotNull(db.Teams);
        Assert.NotNull(db.GameChallenges);
        Assert.NotNull(db.Submissions);
        Assert.NotNull(db.Participations);
    }

    [Fact]
    public async Task Backfill_keeps_every_source_row_count_unchanged()
    {
        var before = await CountSourceRowsAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SkillTreeBackfillService>().RunAsync();

        var after = await CountSourceRowsAsync();

        Assert.Equal(before.Count, after.Count);
        foreach (var (table, count) in before)
            Assert.False(after[table] < count,
                $"Source table {table} lost rows during backfill: {count} -> {after[table]}");
    }

    private async Task<Dictionary<string, long>> CountSourceRowsAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return new Dictionary<string, long>
        {
            [nameof(db.LearningPaths)] = await db.LearningPaths.LongCountAsync(),
            [nameof(db.LearningPathRevisions)] = await db.LearningPathRevisions.LongCountAsync(),
            [nameof(db.LearningModules)] = await db.LearningModules.LongCountAsync(),
            [nameof(db.ModuleItems)] = await db.ModuleItems.LongCountAsync(),
            [nameof(db.Enrollments)] = await db.Enrollments.LongCountAsync(),
            [nameof(db.Games)] = await db.Games.LongCountAsync(),
            [nameof(db.GameChallenges)] = await db.GameChallenges.LongCountAsync(),
            [nameof(db.Submissions)] = await db.Submissions.LongCountAsync(),
            [nameof(db.Participations)] = await db.Participations.LongCountAsync(),
            [nameof(db.Challenges)] = await db.Challenges.LongCountAsync(),
            [nameof(db.Lessons)] = await db.Lessons.LongCountAsync(),
            [nameof(db.ChallengeProgress)] = await db.ChallengeProgress.LongCountAsync(),
            [nameof(db.LessonProgress)] = await db.LessonProgress.LongCountAsync()
        };
    }
}
