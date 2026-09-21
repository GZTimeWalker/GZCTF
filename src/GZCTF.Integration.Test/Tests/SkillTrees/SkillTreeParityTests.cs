using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.Imports.Application;
using GZCTF.Features.Imports.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Features.SkillTrees.Migration;
using GZCTF.Integration.Test.Base;
using GZCTF.Integration.Test.Fixtures.Challenges;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Integration.Test.Tests.SkillTrees;

/// <summary>
/// ST27 release gates. Proves that an upgraded pre-skill-tree database backfills
/// idempotently, that legacy imports do not duplicate shared structure, and that
/// the public detail query stays bounded as the shared category graph grows.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class SkillTreeParityTests(GZCTFApplicationFactory factory)
{
    private const string DuplicatedModuleName = "Web Basics";

    [Fact]
    public async Task Upgraded_database_backfill_is_idempotent_and_preserves_same_name_modules()
    {
        var seed = await SeedLegacyLearningGraphAsync();
        var sourceChallengeProgressCount = await CountChallengeProgressAsync();
        var sourceLessonProgressCount = await CountLessonProgressAsync();

        await RunBackfillAsync();
        await RunBackfillAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(seed.PathIds.Count, await db.SkillTrees.CountAsync(x => seed.PathIds.Contains(x.Id)));
        Assert.Equal(seed.ModuleIds.Count,
            await db.SkillCategories.CountAsync(x => seed.ModuleIds.Contains(x.Id)));
        Assert.Equal(seed.ItemIds.Count,
            await db.CategoryContents.CountAsync(x => seed.ItemIds.Contains(x.Id)));
        Assert.Equal(seed.EnrollmentIds.Count,
            await db.SkillTreeEnrollments.CountAsync(x => seed.PathIds.Contains(x.SkillTreeId)));
        Assert.Equal(sourceChallengeProgressCount, await db.ChallengeProgress.CountAsync());
        Assert.Equal(sourceLessonProgressCount, await db.LessonProgress.CountAsync());
        Assert.Equal(seed.ModuleIds.Count,
            await db.SkillCategories.CountAsync(x => x.Name == DuplicatedModuleName));

        // Every source challenge mode survives the copy and stays published.
        var copiedTypes = await db.CategoryContents
            .Where(x => seed.ItemIds.Contains(x.Id) && x.ChallengeId != null)
            .Select(x => x.Challenge!.Type)
            .Distinct()
            .ToListAsync();
        Assert.Equal(4, copiedTypes.Count);
        Assert.All(copiedTypes, type => Assert.True(Enum.IsDefined(type)));

        // The current-selection pointer still resolves to a published revision.
        var currentTree = await db.SkillTrees.SingleAsync(x => x.Id == seed.CurrentPathId);
        Assert.NotNull(currentTree.CurrentPublishedRevisionId);
        Assert.Equal(SkillTreeRevisionStatus.Published,
            await db.SkillTreeRevisions.Where(x => x.Id == currentTree.CurrentPublishedRevisionId)
                .Select(x => x.Status).SingleAsync());
    }

    [Fact]
    public async Task Legacy_import_run_twice_creates_one_draft_tree_per_game_without_duplicates()
    {
        var fingerprint = $"s27-{Guid.NewGuid():N}";
        var source = BuildImportSource(fingerprint);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var importer = scope.ServiceProvider.GetRequiredService<CanonicalImportService>();
            var first = await importer.ImportAsync(source);
            var second = await importer.ImportAsync(source);
            Assert.Equal(first.BatchId, second.BatchId);
            Assert.Equal(2, first.PathCount);
        }

        await RunBackfillAsync();
        await RunBackfillAsync();

        await using var verify = factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();

        var sourceIds = source.Paths.Select(path => path.SourceId).ToArray();
        var legacyPathIds = await db.LegacyPathMaps
            .Where(item => item.MigrationBatch.PackageFingerprintSha256 == fingerprint)
            .Select(item => item.PathId).ToListAsync();
        Assert.Equal(2, legacyPathIds.Count);
        Assert.All(legacyPathIds, pathId => Assert.NotEqual(Guid.Empty, pathId));

        // Imported games become one draft revision each; nothing duplicates on a second run.
        var moduleIds = await db.LearningModules
            .Where(item => legacyPathIds.Contains(item.Revision.PathId))
            .Select(item => item.Id).ToListAsync();
        Assert.Equal(2, moduleIds.Count);

        foreach (var pathId in legacyPathIds)
        {
            var tree = await db.SkillTrees.SingleAsync(x => x.Id == pathId);
            var revisions = await db.SkillTreeRevisions.Where(x => x.SkillTreeId == tree.Id).ToListAsync();
            Assert.Single(revisions);
            Assert.Equal(SkillTreeRevisionStatus.Draft, revisions[0].Status);

            var categoryCount = await db.SkillTreeCategoryRefs.CountAsync(x => x.RevisionId == revisions[0].Id);
            Assert.Equal(1, categoryCount);
        }

        // Each source module becomes an independent category even when titles repeat.
        Assert.Equal(moduleIds.Count, await db.SkillCategories.CountAsync(x => moduleIds.Contains(x.Id)));
        var duplicatedTitles = await db.SkillCategories
            .Where(x => moduleIds.Contains(x.Id)).Select(x => x.Name).ToListAsync();
        Assert.Equal(2, duplicatedTitles.Count(name => name == "Module"));

        // Imported challenges keep their source mapping, runtime, flags and attachment hashes.
        var challengeSourceIds = source.Paths.SelectMany(path => path.Modules)
            .SelectMany(module => module.Challenges).Select(challenge => challenge.SourceId).ToArray();
        var challenges = await db.Challenges
            .Where(item => challengeSourceIds.Contains(item.SourceId!))
            .Include(item => item.Flags)
            .ToListAsync();
        Assert.Equal(challengeSourceIds.Length, challenges.Count);
        Assert.All(challenges, challenge =>
        {
            Assert.False(string.IsNullOrWhiteSpace(challenge.RuntimeConfigurationJson));
            var flag = Assert.Single(challenge.Flags);
            Assert.Equal(ChallengeFlagKind.DynamicAttachment, flag.Kind);
            Assert.Equal(challenge.SourceId, flag.AttachmentPoolKey);
            Assert.Contains("sha256:imported-a", flag.MetadataJson);
            Assert.Contains("sha256:imported-b", flag.MetadataJson);
        });

        // No tree, category or content association duplicates.
        Assert.Equal(legacyPathIds.Count,
            await db.SkillTrees.CountAsync(x => legacyPathIds.Contains(x.Id)));
        Assert.Equal(moduleIds.Count, await db.SkillCategories.CountAsync(x => moduleIds.Contains(x.Id)));
        Assert.Equal(challengeSourceIds.Length,
            await db.CategoryContents.CountAsync(x => moduleIds.Contains(x.CategoryId)));
    }

    [Fact]
    public async Task Skill_tree_detail_query_count_is_bounded_as_categories_grow()
    {
        var baseline = await MeasureDetailCommandsAsync(categoryCount: 1, itemsPerCategory: 2);
        var grown = await MeasureDetailCommandsAsync(categoryCount: 50, itemsPerCategory: 20);

        Assert.True(grown <= baseline + 1,
            $"Detail query count grew from {baseline} to {grown} commands as categories scaled.");
        Assert.True(grown > 0, "The detail endpoint executed no database commands.");
        Assert.True(baseline >= 3,
            $"Expected the detail endpoint to issue several queries, but only {baseline} were recorded.");
    }

    private async Task<int> MeasureDetailCommandsAsync(int categoryCount, int itemsPerCategory)
    {
        var treeId = await SeedScaledTreeAsync(categoryCount, itemsPerCategory);

        CommandCountInterceptor.Reset();
        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/skill-trees/{treeId}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        // Response size grows with content, but no protected field leaks through.
        Assert.DoesNotContain("flagValue", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("writeup", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"flags\"", body, StringComparison.OrdinalIgnoreCase);

        return CommandCountInterceptor.Recorded.Count;
    }

    private async Task<Guid> SeedScaledTreeAsync(int categoryCount, int itemsPerCategory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var treeId = Guid.CreateVersion7();
        var revisionId = Guid.CreateVersion7();
        var revision = new SkillTreeRevision
        {
            Id = revisionId,
            SkillTreeId = treeId,
            Status = SkillTreeRevisionStatus.Published,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            PublishedAtUtc = DateTimeOffset.UtcNow
        };

        for (var categoryIndex = 0; categoryIndex < categoryCount; categoryIndex++)
        {
            var categoryId = Guid.CreateVersion7();
            var category = new SkillCategory
            {
                Id = categoryId,
                Name = $"Scaled category {categoryIndex}",
                Summary = "",
                IconKey = "flag"
            };

            for (var itemIndex = 0; itemIndex < itemsPerCategory; itemIndex++)
            {
                var challengeId = Guid.CreateVersion7();
                category.Contents.Add(new CategoryContent
                {
                    Id = Guid.CreateVersion7(),
                    CategoryId = categoryId,
                    ChallengeId = challengeId,
                    SortOrder = itemIndex,
                    Challenge = new CanonicalChallenge
                    {
                        Id = challengeId,
                        Type = ChallengeType.StaticAttachment,
                        PublicationState = ChallengePublicationState.Published,
                        IsEnabled = true,
                        SourceType = "s27-scale",
                        SourceId = challengeId.ToString("N"),
                        Localizations =
                        [
                            new ChallengeLocalization
                            {
                                Locale = "en", Title = $"Scaled challenge {categoryIndex}-{itemIndex}"
                            }
                        ]
                    }
                });
            }

            revision.Categories.Add(new SkillTreeCategoryRef
            {
                Id = Guid.CreateVersion7(),
                RevisionId = revisionId,
                CategoryId = categoryId,
                Category = category,
                SortOrder = categoryIndex
            });
        }

        db.SkillTrees.Add(new SkillTree
        {
            Id = treeId,
            Name = $"Scaled tree {treeId:N}",
            Summary = "",
            IconKey = "web",
            Revisions = [revision]
        });
        await db.SaveChangesAsync();

        // Point the tree at the published revision after the graph exists to avoid a
        // circular dependency between the new tree and its own revision.
        await db.SkillTrees.Where(item => item.Id == treeId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(item => item.CurrentPublishedRevisionId, revisionId));
        return treeId;
    }

    private static CanonicalChallengeImportBatch BuildImportSource(string fingerprint)
    {
        CanonicalChallengeImport Challenge(string sourceId) => new(
            "legacy", sourceId, ChallengeType.DynamicAttachment,
            [new ImportLocalizedText("en", "Imported challenge", "Summary", "Body")],
            [], null, null,
            [
                new ImportAttachment("imported-a.bin", "sha256:imported-a", "imported-a", "flag{imported-a}"),
                new ImportAttachment("imported-b.bin", "sha256:imported-b", "imported-b", "flag{imported-b}")
            ],
            null, "{\"score\":100}", 0, 0);

        return new CanonicalChallengeImportBatch(
            "legacy", fingerprint,
            [
                new CanonicalPathImport("legacy", $"game-a-{fingerprint}", $"game-a-{fingerprint}",
                    "Imported game A", "Summary",
                    [new CanonicalModuleImport($"module-a-{fingerprint}", "Module", "Summary", 0,
                        [Challenge($"challenge-a-{fingerprint}")])]),
                new CanonicalPathImport("legacy", $"game-b-{fingerprint}", $"game-b-{fingerprint}",
                    "Imported game B", "Summary",
                    [new CanonicalModuleImport($"module-b-{fingerprint}", "Module", "Summary", 0,
                        [Challenge($"challenge-b-{fingerprint}")])])
            ],
            []);
    }

    private async Task<SeedGraph> SeedLegacyLearningGraphAsync()
    {
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "Parity!123");

        var challenges = ChallengeModeFixtures.All.Select(fixture => new CanonicalChallenge
        {
            Id = Guid.CreateVersion7(),
            Type = fixture.Type,
            PublicationState = ChallengePublicationState.Published,
            IsEnabled = true,
            SourceType = "s27-parity",
            SourceId = $"{fixture.Key}-{Guid.NewGuid():N}",
            Localizations = [new ChallengeLocalization { Locale = "en", Title = fixture.Title }]
        }).ToList();

        var lesson = new Lesson
        {
            Id = Guid.CreateVersion7(),
            PublicationState = LessonPublicationState.Published,
            Localizations = [new LessonLocalization { Locale = "en", Title = "Parity lesson", Body = "Body" }]
        };

        var pathIds = new List<Guid>();
        var moduleIds = new List<Guid>();
        var itemIds = new List<Guid>();
        var enrollmentIds = new List<Guid>();
        Guid currentPathId = default;

        await using (var contentScope = factory.Services.CreateAsyncScope())
        {
            var contentDb = contentScope.ServiceProvider.GetRequiredService<AppDbContext>();
            contentDb.Challenges.AddRange(challenges);
            contentDb.Lessons.Add(lesson);
            await contentDb.SaveChangesAsync();
        }

        for (var pathIndex = 0; pathIndex < 2; pathIndex++)
        {
            var pathId = Guid.CreateVersion7();
            pathIds.Add(pathId);
            var path = new LearningPath
            {
                Id = pathId,
                Slug = $"s27-parity-{pathIndex}-{Guid.NewGuid():N}",
                Localizations =
                [
                    new LearningPathLocalization
                    {
                        Locale = "en", Title = $"Parity path {pathIndex}", Summary = "Summary"
                    }
                ]
            };

            var revisionId = Guid.CreateVersion7();
            var revision = new LearningPathRevision
            {
                Id = revisionId,
                Path = path,
                PathId = pathId,
                Status = LearningPathRevisionStatus.Published,
                PublishedAtUtc = DateTimeOffset.UtcNow
            };

            var moduleId = Guid.CreateVersion7();
            moduleIds.Add(moduleId);
            var module = new LearningModule
            {
                Id = moduleId,
                Revision = revision,
                RevisionId = revisionId,
                SortOrder = 0,
                Localizations = [new LearningModuleLocalization { Locale = "en", Title = DuplicatedModuleName }]
            };

            foreach (var challenge in challenges)
            {
                var itemId = Guid.CreateVersion7();
                itemIds.Add(itemId);
                module.Items.Add(new ModuleItem
                {
                    Id = itemId, SortOrder = module.Items.Count, ChallengeId = challenge.Id
                });
            }

            var lessonItemId = Guid.CreateVersion7();
            itemIds.Add(lessonItemId);
            module.Items.Add(new ModuleItem
            {
                Id = lessonItemId, SortOrder = module.Items.Count, LessonId = lesson.Id
            });

            revision.Modules.Add(module);
            path.Revisions.Add(revision);

            var enrollmentId = Guid.CreateVersion7();
            enrollmentIds.Add(enrollmentId);
            path.Enrollments.Add(new Enrollment
            {
                Id = enrollmentId,
                UserId = user.Id,
                IsCurrent = pathIndex == 0,
                EnrolledAtUtc = DateTimeOffset.UtcNow
            });

            if (pathIndex == 0)
                currentPathId = pathId;

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.LearningPaths.Add(path);
            await db.SaveChangesAsync();

            // Set the published pointer only after the revision row exists.
            await db.LearningPaths.Where(item => item.Id == pathId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(item => item.CurrentPublishedRevisionId, revisionId));
        }

        await using (var progressScope = factory.Services.CreateAsyncScope())
        {
            var db = progressScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.LessonProgress.Add(new LessonProgress
            {
                Id = Guid.CreateVersion7(), UserId = user.Id, LessonId = lesson.Id
            });
            db.ChallengeProgress.Add(new ChallengeProgress
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                ChallengeId = challenges[0].Id,
                SolvedAtUtc = DateTimeOffset.UtcNow,
                SolveMode = ChallengeSolveMode.Independent
            });
            await db.SaveChangesAsync();
        }

        return new SeedGraph(pathIds, moduleIds, itemIds, enrollmentIds, currentPathId);
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
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().LessonProgress.CountAsync();
    }

    private async Task<int> CountChallengeProgressAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().ChallengeProgress.CountAsync();
    }

    private sealed record SeedGraph(
        List<Guid> PathIds,
        List<Guid> ModuleIds,
        List<Guid> ItemIds,
        List<Guid> EnrollmentIds,
        Guid CurrentPathId);
}
