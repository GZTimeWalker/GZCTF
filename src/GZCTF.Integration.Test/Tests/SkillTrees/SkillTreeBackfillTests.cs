using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Features.SkillTrees.Migration;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.SkillTrees;

[Collection(nameof(IntegrationTestCollection))]
public class SkillTreeBackfillTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Backfill_copies_counts_order_enrollments_redirects_and_current_selection()
    {
        var seed = await SeedLearningGraphAsync();
        var lessonProgressBefore = await CountLessonProgressAsync();
        var challengeProgressBefore = await CountChallengeProgressAsync();

        await RunBackfillAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(seed.PathIds.Count, await db.SkillTrees.CountAsync(x => seed.PathIds.Contains(x.Id)));
        Assert.Equal(seed.ModuleIds.Count, await db.SkillCategories.CountAsync(x => seed.ModuleIds.Contains(x.Id)));
        Assert.Equal(seed.ItemIds.Count, await db.CategoryContents.CountAsync(x => seed.ModuleIds.Contains(x.CategoryId)));
        Assert.Equal(seed.EnrollmentIds.Count,
            await db.SkillTreeEnrollments.CountAsync(x => seed.PathIds.Contains(x.SkillTreeId)));
        Assert.Equal(seed.PathIds.Count,
            await db.LearningPathRedirects.CountAsync(x => seed.PathIds.Contains(x.LearningPathId)));

        // Tree IDs equal source path IDs and revision IDs are preserved.
        var treeIds = await db.SkillTrees.Where(x => seed.PathIds.Contains(x.Id)).Select(x => x.Id)
            .OrderBy(x => x).ToListAsync();
        Assert.Equal(seed.PathIds.OrderBy(x => x), treeIds);
        Assert.Equal(seed.RevisionIds.OrderBy(x => x),
            await db.SkillTreeRevisions.Where(x => seed.RevisionIds.Contains(x.Id)).Select(x => x.Id)
                .OrderBy(x => x).ToListAsync());

        // The current published revision pointer follows the copied revision.
        var tree = await db.SkillTrees.SingleAsync(x => x.Id == seed.CurrentPathId);
        Assert.Equal(seed.CurrentPublishedRevisionId, tree.CurrentPublishedRevisionId);

        // Category order inside each revision matches the source module order.
        foreach (var revisionId in seed.RevisionIds)
        {
            var sourceModules = await db.LearningModules
                .Where(x => x.RevisionId == revisionId).OrderBy(x => x.SortOrder).Select(x => x.Id).ToListAsync();
            var categoryRefs = await db.SkillTreeCategoryRefs
                .Where(x => x.RevisionId == revisionId).OrderBy(x => x.SortOrder).Select(x => x.CategoryId).ToListAsync();
            Assert.Equal(sourceModules, categoryRefs);
        }

        // Item order and content references inside each category match the source module items.
        foreach (var moduleId in seed.ModuleIds)
        {
            var sourceItems = await db.ModuleItems
                .Where(x => x.ModuleId == moduleId).OrderBy(x => x.SortOrder).ToListAsync();
            var copiedContents = await db.CategoryContents
                .Where(x => x.CategoryId == moduleId).OrderBy(x => x.SortOrder).ToListAsync();
            Assert.Equal(sourceItems.Count, copiedContents.Count);
            for (var i = 0; i < sourceItems.Count; i++)
            {
                Assert.Equal(sourceItems[i].Id, copiedContents[i].Id);
                Assert.Equal(sourceItems[i].SortOrder, copiedContents[i].SortOrder);
                Assert.Equal(sourceItems[i].LessonId, copiedContents[i].LessonId);
                Assert.Equal(sourceItems[i].ChallengeId, copiedContents[i].ChallengeId);
            }
        }

        // Duplicate module names stay as separate categories.
        var duplicateNames = await db.SkillCategories
            .Where(x => seed.ModuleIds.Contains(x.Id) && x.Name == "Web Basics")
            .Select(x => x.Id).ToListAsync();
        Assert.Equal(2, duplicateNames.Count);
        Assert.Equal(2, duplicateNames.Distinct().Count());

        // The current enrollment stays current on the copied tree.
        var currentEnrollment = await db.SkillTreeEnrollments
            .SingleAsync(x => seed.PathIds.Contains(x.SkillTreeId) && x.IsCurrent);
        Assert.Equal(seed.UserId, currentEnrollment.UserId);
        Assert.Equal(seed.CurrentPathId, currentEnrollment.SkillTreeId);
        Assert.Equal(seed.CurrentEnrollmentId, currentEnrollment.Id);

        // Old slug redirects point at the copied trees.
        foreach (var (pathId, slug) in seed.PathSlugs)
        {
            var redirect = await db.LearningPathRedirects.SingleAsync(x => x.LearningPathId == pathId);
            Assert.Equal(slug, redirect.OldSlug);
            Assert.Equal(pathId, redirect.SkillTreeId);
        }

        // Existing progress rows are untouched.
        Assert.Equal(lessonProgressBefore, await CountLessonProgressAsync());
        Assert.Equal(challengeProgressBefore, await CountChallengeProgressAsync());
    }

    [Fact]
    public async Task Backfill_is_idempotent_when_run_twice()
    {
        var seed = await SeedLearningGraphAsync();

        await RunBackfillAsync();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(seed.PathIds.Count, await db.SkillTrees.CountAsync(x => seed.PathIds.Contains(x.Id)));
            Assert.Equal(seed.ModuleIds.Count, await db.SkillCategories.CountAsync(x => seed.ModuleIds.Contains(x.Id)));
            Assert.Equal(seed.ItemIds.Count, await db.CategoryContents.CountAsync(x => seed.ModuleIds.Contains(x.CategoryId)));
            Assert.Equal(seed.EnrollmentIds.Count,
                await db.SkillTreeEnrollments.CountAsync(x => seed.PathIds.Contains(x.SkillTreeId)));
            Assert.Equal(seed.PathIds.Count,
                await db.LearningPathRedirects.CountAsync(x => seed.PathIds.Contains(x.LearningPathId)));
        }

        await RunBackfillAsync();

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(seed.PathIds.Count, await verifyDb.SkillTrees.CountAsync(x => seed.PathIds.Contains(x.Id)));
        Assert.Equal(seed.ModuleIds.Count, await verifyDb.SkillCategories.CountAsync(x => seed.ModuleIds.Contains(x.Id)));
        Assert.Equal(seed.ItemIds.Count, await verifyDb.CategoryContents.CountAsync(x => seed.ModuleIds.Contains(x.CategoryId)));
        Assert.Equal(seed.EnrollmentIds.Count,
            await verifyDb.SkillTreeEnrollments.CountAsync(x => seed.PathIds.Contains(x.SkillTreeId)));
        Assert.Equal(seed.PathIds.Count,
            await verifyDb.LearningPathRedirects.CountAsync(x => seed.PathIds.Contains(x.LearningPathId)));

        // Associations stay stable across runs.
        var currentEnrollment = await verifyDb.SkillTreeEnrollments
            .SingleAsync(x => seed.PathIds.Contains(x.SkillTreeId) && x.IsCurrent);
        Assert.Equal(seed.CurrentPathId, currentEnrollment.SkillTreeId);
        var tree = await verifyDb.SkillTrees.SingleAsync(x => x.Id == seed.CurrentPathId);
        Assert.Equal(seed.CurrentPublishedRevisionId, tree.CurrentPublishedRevisionId);
        foreach (var moduleId in seed.ModuleIds)
        {
            var sourceCount = await verifyDb.ModuleItems.CountAsync(x => x.ModuleId == moduleId);
            var copiedCount = await verifyDb.CategoryContents.CountAsync(x => x.CategoryId == moduleId);
            Assert.Equal(sourceCount, copiedCount);
        }
    }

    private async Task RunBackfillAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<SkillTreeBackfillService>();
        await service.RunAsync();
    }

    private async Task<int> CountLessonProgressAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.LessonProgress.CountAsync();
    }

    private async Task<int> CountChallengeProgressAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ChallengeProgress.CountAsync();
    }

    private async Task<SeedGraph> SeedLearningGraphAsync()
    {
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "Backfill!123");

        var challenge = new Challenge
        {
            Id = Guid.CreateVersion7(),
            Type = ChallengeType.StaticAttachment,
            PublicationState = ChallengePublicationState.Published,
            SourceType = "skill-tree-backfill-test",
            SourceId = Guid.NewGuid().ToString("N")
        };
        var lesson = new Lesson
        {
            Id = Guid.CreateVersion7(),
            PublicationState = LessonPublicationState.Published,
            Localizations =
            [
                new LessonLocalization { Locale = "zh-CN", Title = "基础课节", Body = "正文" },
                new LessonLocalization { Locale = "en", Title = "Foundation lesson", Body = "Body" }
            ]
        };

        var path = new LearningPath
        {
            Id = Guid.CreateVersion7(),
            Slug = $"backfill-{Guid.NewGuid():N}",
            Localizations =
            [
                new LearningPathLocalization { Locale = "zh-CN", Title = "初学者训练营", Summary = "入门" },
                new LearningPathLocalization { Locale = "en", Title = "Beginner camp", Summary = "Start here" }
            ]
        };
        var published = new LearningPathRevision
        {
            Id = Guid.CreateVersion7(),
            Path = path,
            Status = LearningPathRevisionStatus.Published,
            PublishedAtUtc = DateTimeOffset.UtcNow
        };
        var module = new LearningModule
        {
            Id = Guid.CreateVersion7(),
            Revision = published,
            SortOrder = 0,
            ExpectedMinutes = 60,
            Localizations =
            [
                new LearningModuleLocalization { Locale = "zh-CN", Title = "Web Basics", Summary = "Web" }
            ]
        };
        module.Items.Add(new ModuleItem { Id = Guid.CreateVersion7(), SortOrder = 0, Lesson = lesson });
        module.Items.Add(new ModuleItem { Id = Guid.CreateVersion7(), SortOrder = 1, Challenge = challenge });
        published.Modules.Add(module);
        path.Revisions.Add(published);
        path.Revisions.Add(new LearningPathRevision
        {
            Id = Guid.CreateVersion7(),
            Path = path,
            Status = LearningPathRevisionStatus.Draft
        });
        var currentEnrollment = new Enrollment
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            IsCurrent = true,
            EnrolledAtUtc = DateTimeOffset.UtcNow
        };
        path.Enrollments.Add(currentEnrollment);

        var secondPath = new LearningPath
        {
            Id = Guid.CreateVersion7(),
            Slug = $"backfill-{Guid.NewGuid():N}",
            Localizations =
            [
                new LearningPathLocalization { Locale = "zh-CN", Title = "进阶训练营", Summary = "进阶" },
                new LearningPathLocalization { Locale = "en", Title = "Advanced camp", Summary = "Level up" }
            ]
        };
        var secondPublished = new LearningPathRevision
        {
            Id = Guid.CreateVersion7(),
            Path = secondPath,
            Status = LearningPathRevisionStatus.Published,
            PublishedAtUtc = DateTimeOffset.UtcNow
        };
        var secondModule = new LearningModule
        {
            Id = Guid.CreateVersion7(),
            Revision = secondPublished,
            SortOrder = 0,
            ExpectedMinutes = 30,
            Localizations =
            [
                new LearningModuleLocalization { Locale = "zh-CN", Title = "Web Basics", Summary = "Web" }
            ]
        };
        secondModule.Items.Add(new ModuleItem { Id = Guid.CreateVersion7(), SortOrder = 0, Challenge = challenge });
        secondPublished.Modules.Add(secondModule);
        secondPath.Revisions.Add(secondPublished);
        var secondEnrollment = new Enrollment
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            IsCurrent = false,
            EnrolledAtUtc = DateTimeOffset.UtcNow
        };
        secondPath.Enrollments.Add(secondEnrollment);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.LearningPaths.Add(path);
            db.LearningPaths.Add(secondPath);
            db.LessonProgress.Add(new LessonProgress
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                Lesson = lesson,
                CompletedAtUtc = DateTimeOffset.UtcNow
            });
            db.ChallengeProgress.Add(new ChallengeProgress
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                Challenge = challenge,
                SolvedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();

            // Set the published revision pointers in a second save to avoid a circular
            // dependency between LearningPath and LearningPathRevision inserts.
            path.CurrentPublishedRevisionId = published.Id;
            secondPath.CurrentPublishedRevisionId = secondPublished.Id;
            await db.SaveChangesAsync();
        }

        return new SeedGraph(
            UserId: user.Id,
            PathIds: [path.Id, secondPath.Id],
            RevisionIds: [published.Id, secondPublished.Id],
            ModuleIds: [module.Id, secondModule.Id],
            ItemIds: [.. module.Items.Select(x => x.Id), .. secondModule.Items.Select(x => x.Id)],
            EnrollmentIds: [currentEnrollment.Id, secondEnrollment.Id],
            CurrentPathId: path.Id,
            CurrentPublishedRevisionId: published.Id,
            CurrentEnrollmentId: currentEnrollment.Id,
            PathSlugs: [(path.Id, path.Slug), (secondPath.Id, secondPath.Slug)]);
    }

    private sealed record SeedGraph(
        Guid UserId,
        List<Guid> PathIds,
        List<Guid> RevisionIds,
        List<Guid> ModuleIds,
        List<Guid> ItemIds,
        List<Guid> EnrollmentIds,
        Guid CurrentPathId,
        Guid CurrentPublishedRevisionId,
        Guid CurrentEnrollmentId,
        List<(Guid PathId, string Slug)> PathSlugs);
}
