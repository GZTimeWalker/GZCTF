using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.Dashboard.Application;
using GZCTF.Features.Dashboard.Domain;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.DashboardProjection;

[Collection(nameof(IntegrationTestCollection))]
public sealed class DailySolveProjectionTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Failed_rebuild_keeps_the_previous_projection()
    {
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "Dashboard!Projection1");
        var challengeId = Guid.CreateVersion7();
        var existingDate = new DateOnly(2026, 9, 19);
        var triggerName = $"fail_daily_insert_{Guid.NewGuid():N}";
        var functionName = $"fail_daily_insert_fn_{Guid.NewGuid():N}";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Challenges.Add(new Challenge
            {
                Id = challengeId,
                Type = ChallengeType.StaticAttachment,
                PublicationState = ChallengePublicationState.Published,
                SourceType = "daily-projection-test",
                SourceId = challengeId.ToString("N")
            });
            db.ChallengeProgress.Add(new ChallengeProgress
            {
                UserId = user.Id,
                ChallengeId = challengeId,
                SolvedAtUtc = new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero)
            });
            db.LearnerDailySolveStats.Add(new LearnerDailySolveStat
            {
                UserId = user.Id,
                Date = existingDate,
                SolveCount = 7
            });
            await db.SaveChangesAsync();

#pragma warning disable EF1002 // Identifiers are generated GUIDs and cannot be SQL parameters.
            await db.Database.ExecuteSqlRawAsync($$"""
                CREATE FUNCTION "{{functionName}}"() RETURNS trigger AS $trigger$
                BEGIN
                    RAISE EXCEPTION 'forced projection failure';
                END;
                $trigger$ LANGUAGE plpgsql;
                CREATE TRIGGER "{{triggerName}}"
                BEFORE INSERT ON "LearnerDailySolveStats"
                FOR EACH ROW EXECUTE FUNCTION "{{functionName}}"();
                """);
#pragma warning restore EF1002

            var projection = scope.ServiceProvider.GetRequiredService<DailySolveProjection>();
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => projection.RebuildAsync());
        }

        await using (var cleanupScope = factory.Services.CreateAsyncScope())
        {
            var db = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existing = await db.LearnerDailySolveStats.AsNoTracking()
                .SingleAsync(item => item.UserId == user.Id);
            Assert.Equal(existingDate, existing.Date);
            Assert.Equal(7, existing.SolveCount);

#pragma warning disable EF1002 // Identifiers are generated GUIDs and cannot be SQL parameters.
            await db.Database.ExecuteSqlRawAsync($$"""
                DROP TRIGGER IF EXISTS "{{triggerName}}" ON "LearnerDailySolveStats";
                DROP FUNCTION IF EXISTS "{{functionName}}"();
                """);
#pragma warning restore EF1002
        }
    }
}
