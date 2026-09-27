using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.Dashboard.Domain;
using GZCTF.Features.Imports.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Integration.Test.Tests.Learning;

[Collection(nameof(IntegrationTestCollection))]
public class LearningSchemaTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task LearningPath_SlugIsUnique()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            var slug = UniqueSlug();
            db.LearningPaths.AddRange(
                new LearningPath { Slug = slug },
                new LearningPath { Slug = slug });

            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task LocalizedContent_IsUniquePerOwnerAndLocale()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            var challenge = new CanonicalChallenge { Type = ChallengeType.StaticAttachment };
            challenge.Localizations.Add(new ChallengeLocalization
            {
                Locale = "en",
                Title = "Challenge",
                Summary = "Summary",
                Body = "Body"
            });
            challenge.Localizations.Add(new ChallengeLocalization
            {
                Locale = "en",
                Title = "Duplicate",
                Summary = "Summary",
                Body = "Body"
            });
            db.Challenges.Add(challenge);

            await db.SaveChangesAsync();
        });

        await AssertConstraintViolationAsync(async db =>
        {
            var lesson = new Lesson();
            lesson.Localizations.Add(new LessonLocalization { Locale = "zh-CN", Title = "课节", Body = "正文" });
            lesson.Localizations.Add(new LessonLocalization { Locale = "zh-CN", Title = "重复", Body = "正文" });
            db.Lessons.Add(lesson);

            await db.SaveChangesAsync();
        });

        await AssertConstraintViolationAsync(async db =>
        {
            var revision = new LearningPathRevision
            {
                Path = new LearningPath { Slug = UniqueSlug() },
                Status = LearningPathRevisionStatus.Draft
            };
            var module = new LearningModule { Revision = revision, SortOrder = 0 };
            module.Localizations.Add(new LearningModuleLocalization { Locale = "en", Title = "Module", Summary = "Summary" });
            module.Localizations.Add(new LearningModuleLocalization { Locale = "en", Title = "Duplicate", Summary = "Summary" });
            revision.Modules.Add(module);
            db.LearningPathRevisions.Add(revision);

            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task ModuleItem_MustReferenceExactlyOneContentType()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            var challenge = new CanonicalChallenge { Type = ChallengeType.StaticAttachment };
            var lesson = new Lesson();
            var revision = new LearningPathRevision
            {
                Path = new LearningPath { Slug = UniqueSlug() },
                Status = LearningPathRevisionStatus.Draft
            };
            var module = new LearningModule { Revision = revision, SortOrder = 0 };
            module.Items.Add(new ModuleItem
            {
                SortOrder = 0,
                Challenge = challenge,
                Lesson = lesson
            });
            revision.Modules.Add(module);
            db.LearningPathRevisions.Add(revision);

            await db.SaveChangesAsync();
        });

        await AssertConstraintViolationAsync(async db =>
        {
            var revision = new LearningPathRevision
            {
                Path = new LearningPath { Slug = UniqueSlug() },
                Status = LearningPathRevisionStatus.Draft
            };
            var module = new LearningModule { Revision = revision, SortOrder = 0 };
            module.Items.Add(new ModuleItem { SortOrder = 0 });
            revision.Modules.Add(module);
            db.LearningPathRevisions.Add(revision);

            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task UserScopedProgressAndEnrollmentKeysAreUnique()
    {
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");

        await AssertConstraintViolationAsync(async db =>
        {
            var challenge = new CanonicalChallenge { Type = ChallengeType.StaticAttachment };
            db.ChallengeProgress.AddRange(
                new ChallengeProgress { UserId = user.Id, Challenge = challenge, SolvedAtUtc = DateTimeOffset.UtcNow },
                new ChallengeProgress { UserId = user.Id, Challenge = challenge, SolvedAtUtc = DateTimeOffset.UtcNow });

            await db.SaveChangesAsync();
        });

        await AssertConstraintViolationAsync(async db =>
        {
            var lesson = new Lesson();
            db.LessonProgress.AddRange(
                new LessonProgress { UserId = user.Id, Lesson = lesson, CompletedAtUtc = DateTimeOffset.UtcNow },
                new LessonProgress { UserId = user.Id, Lesson = lesson, CompletedAtUtc = DateTimeOffset.UtcNow });

            await db.SaveChangesAsync();
        });

        await AssertConstraintViolationAsync(async db =>
        {
            var path = new LearningPath { Slug = UniqueSlug() };
            db.Enrollments.AddRange(
                new Enrollment { UserId = user.Id, Path = path },
                new Enrollment { UserId = user.Id, Path = path });

            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task PathHasAtMostOneDraftRevision()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            var path = new LearningPath { Slug = UniqueSlug() };
            db.LearningPathRevisions.AddRange(
                new LearningPathRevision { Path = path, Status = LearningPathRevisionStatus.Draft },
                new LearningPathRevision { Path = path, Status = LearningPathRevisionStatus.Draft });

            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task UserHasAtMostOneCurrentEnrollment()
    {
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");

        await AssertConstraintViolationAsync(async db =>
        {
            db.Enrollments.AddRange(
                new Enrollment
                {
                    UserId = user.Id,
                    Path = new LearningPath { Slug = UniqueSlug() },
                    IsCurrent = true
                },
                new Enrollment
                {
                    UserId = user.Id,
                    Path = new LearningPath { Slug = UniqueSlug() },
                    IsCurrent = true
                });

            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Cohort_NameIsUnique()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            db.Cohorts.AddRange(
                new Cohort { Name = "2026" },
                new Cohort { Name = "2026" });

            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task LearnerDailySolveStat_UserAndDateAreUnique()
    {
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");

        await AssertConstraintViolationAsync(async db =>
        {
            db.LearnerDailySolveStats.AddRange(
                new LearnerDailySolveStat { UserId = user.Id, Date = new DateOnly(2026, 9, 20) },
                new LearnerDailySolveStat { UserId = user.Id, Date = new DateOnly(2026, 9, 20) });

            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task DashboardToken_HashIsUnique()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            var dashboard = new Dashboard { Name = "Learning" };
            db.DashboardTokens.AddRange(
                new DashboardToken { Dashboard = dashboard, TokenHash = "hash-1" },
                new DashboardToken { Dashboard = dashboard, TokenHash = "hash-1" });

            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task LegacySourceMappings_AreUnique()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            var challenge = new CanonicalChallenge { Type = ChallengeType.StaticAttachment };
            db.LegacyChallengeMaps.AddRange(
                new LegacyChallengeMap
                {
                    SourceType = "game-challenge",
                    SourceId = "42",
                    Challenge = challenge
                },
                new LegacyChallengeMap
                {
                    SourceType = "game-challenge",
                    SourceId = "42",
                    Challenge = challenge
                });

            await db.SaveChangesAsync();
        });

        await AssertConstraintViolationAsync(async db =>
        {
            var path = new LearningPath { Slug = UniqueSlug() };
            db.LegacyPathMaps.AddRange(
                new LegacyPathMap { SourceType = "game", SourceId = "42", Path = path },
                new LegacyPathMap { SourceType = "game", SourceId = "42", Path = path });

            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task MigrationBatch_PackageFingerprintIsUnique()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            db.MigrationBatches.AddRange(
                new MigrationBatch
                {
                    SourceType = "legacy-database",
                    PackageFingerprintSha256 = "aabbcc"
                },
                new MigrationBatch
                {
                    SourceType = "legacy-database",
                    PackageFingerprintSha256 = "aabbcc"
                });

            await db.SaveChangesAsync();
        });
    }

    private async Task AssertConstraintViolationAsync(Func<AppDbContext, Task> arrangeAndSave)
    {
        using var scope = factory.Services.CreateScope();
        await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => arrangeAndSave(db));
        await transaction.RollbackAsync();
    }

    private static string UniqueSlug() => $"learning-{Guid.NewGuid():N}";
}
