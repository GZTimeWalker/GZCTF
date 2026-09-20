using System.Net.Http.Json;
using System.Text.Json.Nodes;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Integration.Test.Tests.Learning;

[Collection(nameof(IntegrationTestCollection))]
public class LearningRecordTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task MyLearningReturnsRouteAndModuleAggregates_RecentActivity_AndSolveHelp()
    {
        var lessonOne = await SeedLessonAsync("Lesson one");
        var lessonTwo = await SeedLessonAsync("Lesson two");
        var challenge = await SeedChallengeAsync();
        var route = await SeedPathAsync("aggregate", [
            [new LearningItemSeed(lessonOne.LessonId, null), new LearningItemSeed(null, challenge.ChallengeId)],
            [new LearningItemSeed(lessonTwo.LessonId, null)]
        ]);
        var user = await CreateUserAsync();
        await SeedEnrollmentAndProgressAsync(user.Id, route.PathId, lessonOne.LessonId, challenge.ChallengeId,
            ChallengeSolveMode.AfterHint);
        using var client = await LoginAsync(user);

        var response = await client.GetAsync("/api/my-learning");
        response.EnsureSuccessStatusCode();
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        var routeJson = Assert.Single(json["routes"]!.AsArray());
        Assert.Equal(route.PathId.ToString(), routeJson!["pathId"]!.GetValue<string>());
        Assert.Equal(66.67, routeJson["progressPercent"]!.GetValue<double>(), 2);
        Assert.Equal(1, routeJson["completedModules"]!.GetValue<int>());
        Assert.Equal(1, routeJson["completedLessons"]!.GetValue<int>());
        Assert.Equal(route.PathId.ToString(), json["currentPathId"]!.GetValue<string>());
        Assert.Equal(1, json["solvedChallengeCount"]!.GetValue<int>());
        Assert.Contains(lessonOne.LessonId.ToString(), json["completedLessonIds"]!.AsArray()
            .Select(item => item!.GetValue<string>()));
        Assert.Equal("AfterHint", json["solvedChallenges"]![0]! ["solveMode"]!.GetValue<string>());

        var modules = routeJson["modules"]!.AsArray();
        Assert.Equal(2, modules.Count);
        Assert.True(modules[0]!["isComplete"]!.GetValue<bool>());
        Assert.False(modules[1]!["isComplete"]!.GetValue<bool>());
        Assert.InRange(json["recentActivity"]!.AsArray().Count, 2, 20);

        using var anonymous = factory.CreateClient();
        var list = await anonymous.GetAsync("/api/learning-paths");
        list.EnsureSuccessStatusCode();
        var listText = await list.Content.ReadAsStringAsync();
        Assert.DoesNotContain("progressPercent", listText);
        Assert.DoesNotContain("completedModules", listText);
        Assert.DoesNotContain("completedLessons", listText);
        var preview = await anonymous.GetAsync($"/api/learning-paths/{route.Slug}/preview");
        preview.EnsureSuccessStatusCode();
        var previewText = await preview.Content.ReadAsStringAsync();
        Assert.DoesNotContain("progressPercent", previewText);
        Assert.DoesNotContain("completedModules", previewText);
        Assert.DoesNotContain("completedLessons", previewText);
    }

    [Fact]
    public async Task ReusedChallengeCountsOnceGlobally_AndInEveryRouteThatReferencesIt()
    {
        var challenge = await SeedChallengeAsync();
        var first = await SeedPathAsync("reuse-a", [[new LearningItemSeed(null, challenge.ChallengeId)]]);
        var second = await SeedPathAsync("reuse-b", [[new LearningItemSeed(null, challenge.ChallengeId)]]);
        var user = await CreateUserAsync();
        await SeedEnrollmentAndProgressAsync(user.Id, first.PathId, null, challenge.ChallengeId,
            ChallengeSolveMode.AfterWriteup);
        await SeedEnrollmentAsync(user.Id, second.PathId, false);
        using var client = await LoginAsync(user);

        var json = JsonNode.Parse(await (await client.GetAsync("/api/my-learning"))
            .Content.ReadAsStringAsync())!;
        Assert.Equal(1, json["solvedChallengeCount"]!.GetValue<int>());
        var routes = json["routes"]!.AsArray()
            .Where(item => item!["pathId"]!.GetValue<string>() == first.PathId.ToString() ||
                           item["pathId"]!.GetValue<string>() == second.PathId.ToString())
            .ToArray();
        Assert.Equal(2, routes.Length);
        Assert.All(routes, item => Assert.Equal(100, item!["progressPercent"]!.GetValue<double>()));
    }

    [Fact]
    public async Task RecentActivityIsBoundedAndSortedNewestFirst()
    {
        var route = await SeedPathAsync("activity", [[new LearningItemSeed(null, null)]]);
        var user = await CreateUserAsync();
        await SeedEnrollmentAsync(user.Id, route.PathId, true);
        var challengeIds = new List<Guid>();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            for (var index = 0; index < 25; index++)
            {
                var challenge = new CanonicalChallenge
                {
                    Type = ChallengeType.StaticAttachment,
                    PublicationState = ChallengePublicationState.Published,
                    SourceType = "native",
                    SourceId = $"activity-{Guid.NewGuid():N}",
                    Localizations = [new ChallengeLocalization { Locale = "en", Title = $"Activity {index}" }]
                };
                challengeIds.Add(challenge.Id);
                db.Challenges.Add(challenge);
                db.ChallengeProgress.Add(new ChallengeProgress
                {
                    UserId = user.Id,
                    Challenge = challenge,
                    SolvedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-index)
                });
            }
            await db.SaveChangesAsync();
        }

        using var client = await LoginAsync(user);
        var json = JsonNode.Parse(await (await client.GetAsync("/api/my-learning"))
            .Content.ReadAsStringAsync())!;
        var activity = json["recentActivity"]!.AsArray();
        Assert.Equal(20, activity.Count);
        var dates = activity.Select(item =>
            DateTimeOffset.FromUnixTimeMilliseconds(item!["completedAtUtc"]!.GetValue<long>())).ToArray();
        Assert.True(dates.Zip(dates.Skip(1)).All(pair => pair.First >= pair.Second));
        Assert.Equal(25, json["solvedChallengeCount"]!.GetValue<int>());
        Assert.Equal(25, challengeIds.Count);
    }

    [Fact]
    public async Task ExistingProgressRowsSurviveNewPublishedRevision_AndCurrentRevisionIsTheDenominator()
    {
        var lesson = await SeedLessonAsync("Persistent lesson");
        var challenge = await SeedChallengeAsync();
        var route = await SeedPathAsync("revision-record", [[
            new LearningItemSeed(lesson.LessonId, null), new LearningItemSeed(null, challenge.ChallengeId)
        ]]);
        var user = await CreateUserAsync();
        await SeedEnrollmentAndProgressAsync(user.Id, route.PathId, lesson.LessonId, challenge.ChallengeId,
            ChallengeSolveMode.Independent);
        await PublishReplacementRevisionAsync(route.PathId, lesson.LessonId, challenge.ChallengeId);
        using var client = await LoginAsync(user);

        var json = JsonNode.Parse(await (await client.GetAsync("/api/my-learning"))
            .Content.ReadAsStringAsync())!;
        var routeJson = Assert.Single(json["routes"]!.AsArray());
        Assert.Equal(100, routeJson!["progressPercent"]!.GetValue<double>());
        Assert.Equal(1, routeJson["completedLessons"]!.GetValue<int>());

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.LessonProgress.CountAsync(item => item.UserId == user.Id));
        Assert.Equal(1, await db.ChallengeProgress.CountAsync(item => item.UserId == user.Id));
    }

    private async Task<TestDataSeeder.SeededUser> CreateUserAsync() =>
        await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "S07!LearnerPassword");

    private async Task<HttpClient> LoginAsync(TestDataSeeder.SeededUser user)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = user.Password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    private async Task<SeededLesson> SeedLessonAsync(string title)
    {
        var lesson = new Lesson
        {
            Localizations = [new LessonLocalization { Locale = "en", Title = title, Body = $"# {title}" }]
        };
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();
        return new SeededLesson(lesson.Id);
    }

    private async Task<SeededChallenge> SeedChallengeAsync()
    {
        var challenge = new CanonicalChallenge
        {
            Type = ChallengeType.StaticAttachment,
            PublicationState = ChallengePublicationState.Published,
            SourceType = "native",
            SourceId = $"s07-{Guid.NewGuid():N}",
            Localizations = [new ChallengeLocalization { Locale = "en", Title = "Solved challenge" }]
        };
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();
        return new SeededChallenge(challenge.Id);
    }

    private async Task<SeededPath> SeedPathAsync(string name, IReadOnlyList<IReadOnlyList<LearningItemSeed>> modules)
    {
        var path = new LearningPath
        {
            Slug = $"s07-{name}-{Guid.NewGuid():N}",
            Localizations = [new LearningPathLocalization { Locale = "en", Title = name, Summary = "Summary" }]
        };
        var revision = new LearningPathRevision
        {
            Path = path,
            Status = LearningPathRevisionStatus.Published,
            PublishedAtUtc = DateTimeOffset.UtcNow
        };
        for (var moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++)
        {
            var module = new LearningModule
            {
                Revision = revision,
                SortOrder = moduleIndex,
                Localizations =
                [new LearningModuleLocalization { Locale = "en", Title = $"Module {moduleIndex}", Summary = "Summary" }]
            };
            var items = modules[moduleIndex];
            for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
            {
                var item = items[itemIndex];
                if (item.LessonId is null && item.ChallengeId is null)
                {
                    var placeholder = await SeedChallengeAsync();
                    item = new LearningItemSeed(null, placeholder.ChallengeId);
                }
                module.Items.Add(new ModuleItem
                {
                    Module = module,
                    SortOrder = itemIndex,
                    LessonId = item.LessonId,
                    ChallengeId = item.ChallengeId
                });
            }
            revision.Modules.Add(module);
        }
        path.Revisions.Add(revision);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.LearningPaths.Add(path);
        await db.SaveChangesAsync();
        path.CurrentPublishedRevisionId = revision.Id;
        await db.SaveChangesAsync();
        return new SeededPath(path.Id, path.Slug);
    }

    private async Task SeedEnrollmentAsync(Guid userId, Guid pathId, bool isCurrent)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Enrollments.Add(new Enrollment { UserId = userId, PathId = pathId, IsCurrent = isCurrent });
        await db.SaveChangesAsync();
    }

    private async Task SeedEnrollmentAndProgressAsync(
        Guid userId, Guid pathId, Guid? lessonId, Guid? challengeId, ChallengeSolveMode solveMode)
    {
        await SeedEnrollmentAsync(userId, pathId, true);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (lessonId is { } lesson)
            db.LessonProgress.Add(new LessonProgress { UserId = userId, LessonId = lesson });
        if (challengeId is { } challenge)
            db.ChallengeProgress.Add(new ChallengeProgress
            {
                UserId = userId, ChallengeId = challenge, SolveMode = solveMode
            });
        await db.SaveChangesAsync();
    }

    private async Task PublishReplacementRevisionAsync(Guid pathId, Guid lessonId, Guid challengeId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var path = await db.LearningPaths.Include(item => item.Revisions)
            .SingleAsync(item => item.Id == pathId);
        var current = path.Revisions.Single(item => item.Status == LearningPathRevisionStatus.Published);
        current.Status = LearningPathRevisionStatus.Archived;
        var revision = new LearningPathRevision
        {
            PathId = pathId,
            Status = LearningPathRevisionStatus.Published,
            PublishedAtUtc = DateTimeOffset.UtcNow,
            Modules =
            [
                new LearningModule
                {
                    SortOrder = 0,
                    Localizations = [new LearningModuleLocalization { Locale = "en", Title = "New module" }],
                    Items =
                    [
                        new ModuleItem { SortOrder = 0, LessonId = lessonId },
                        new ModuleItem { SortOrder = 1, ChallengeId = challengeId }
                    ]
                }
            ]
        };
        db.LearningPathRevisions.Add(revision);
        await db.SaveChangesAsync();
        path.CurrentPublishedRevisionId = revision.Id;
        await db.SaveChangesAsync();
    }

    private sealed record SeededPath(Guid PathId, string Slug);
    private sealed record SeededLesson(Guid LessonId);
    private sealed record SeededChallenge(Guid ChallengeId);
    private sealed record LearningItemSeed(Guid? LessonId, Guid? ChallengeId);
}
