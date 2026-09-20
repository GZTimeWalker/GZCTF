using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Learning;

[Collection(nameof(IntegrationTestCollection))]
public class LearningPathPublishingTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task AdministratorCanCreatePathWithOneDraft_AndSecondRequestReturnsExistingDraft()
    {
        using var admin = await CreateAdminClientAsync();
        var slug = $"s05-{Guid.NewGuid():N}";

        var first = await admin.PostAsJsonAsync("/api/admin/learning-paths", PathCommand(slug));
        first.EnsureSuccessStatusCode();
        var firstJson = await first.Content.ReadAsStringAsync();

        var second = await admin.PostAsJsonAsync("/api/admin/learning-paths", PathCommand(slug));
        second.EnsureSuccessStatusCode();
        var secondJson = await second.Content.ReadAsStringAsync();

        Assert.Equal(ReadGuid(firstJson, "pathId"), ReadGuid(secondJson, "pathId"));
        Assert.Equal(ReadGuid(firstJson, "revisionId"), ReadGuid(secondJson, "revisionId"));
        Assert.Equal(LearningPathRevisionStatus.Draft,
            await ReadRevisionStatusAsync(ReadGuid(firstJson, "revisionId")));
    }

    [Fact]
    public async Task PublishingRejectsEmptyModule_UnpublishedChallenge_AndMissingEnglishFallback()
    {
        using var admin = await CreateAdminClientAsync();

        var emptySlug = $"empty-{Guid.NewGuid():N}";
        var emptyCreate = await admin.PostAsJsonAsync("/api/admin/learning-paths",
            PathCommand(emptySlug, modules: [ModuleCommand(items: [])]));
        emptyCreate.EnsureSuccessStatusCode();
        var emptyJson = await emptyCreate.Content.ReadAsStringAsync();
        var emptyPublish = await admin.PostAsJsonAsync(
            $"/api/admin/learning-paths/{ReadGuid(emptyJson, "pathId")}/publish",
            new { rowVersion = ReadUInt(emptyJson, "rowVersion") });
        Assert.Equal(HttpStatusCode.BadRequest, emptyPublish.StatusCode);

        var unpublishedChallengeId = await SeedChallengeAsync(ChallengePublicationState.Draft);
        var unpublishedSlug = $"unpublished-{Guid.NewGuid():N}";
        var unpublishedCreate = await admin.PostAsJsonAsync("/api/admin/learning-paths",
            PathCommand(unpublishedSlug, modules:
            [
                ModuleCommand(items: [new { sortOrder = 0, challengeId = unpublishedChallengeId }])
            ]));
        unpublishedCreate.EnsureSuccessStatusCode();
        var unpublishedJson = await unpublishedCreate.Content.ReadAsStringAsync();
        var unpublishedPublish = await admin.PostAsJsonAsync(
            $"/api/admin/learning-paths/{ReadGuid(unpublishedJson, "pathId")}/publish",
            new { rowVersion = ReadUInt(unpublishedJson, "rowVersion") });
        Assert.Equal(HttpStatusCode.BadRequest, unpublishedPublish.StatusCode);

        var missingEnglishLessonId = await SeedLessonAsync();
        var missingEnglishSlug = $"missing-en-{Guid.NewGuid():N}";
        var missingEnglishCreate = await admin.PostAsJsonAsync("/api/admin/learning-paths",
            PathCommand(missingEnglishSlug, localizations:
            [
                new { locale = "zh-CN", title = "中文路径", summary = "中文摘要" }
            ], modules:
            [
                ModuleCommand(items: [new { sortOrder = 0, lessonId = missingEnglishLessonId }])
            ]));
        missingEnglishCreate.EnsureSuccessStatusCode();
        var missingEnglishJson = await missingEnglishCreate.Content.ReadAsStringAsync();
        var missingEnglishPublish = await admin.PostAsJsonAsync(
            $"/api/admin/learning-paths/{ReadGuid(missingEnglishJson, "pathId")}/publish",
            new { rowVersion = ReadUInt(missingEnglishJson, "rowVersion") });
        Assert.Equal(HttpStatusCode.BadRequest, missingEnglishPublish.StatusCode);
        var missingEnglishError = JsonNode.Parse(await missingEnglishPublish.Content.ReadAsStringAsync());
        Assert.Equal("learning.invalid_draft", missingEnglishError?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task PublishSwitchesCurrentRevisionInsideTransaction_AndPreviewOmitsBodies()
    {
        using var admin = await CreateAdminClientAsync();
        var lessonId = await SeedLessonAsync();
        var challengeId = await SeedChallengeAsync(ChallengePublicationState.Published);
        var slug = $"publish-{Guid.NewGuid():N}";
        var create = await admin.PostAsJsonAsync("/api/admin/learning-paths",
            PathCommand(slug, modules:
            [
                ModuleCommand(items:
                [
                    new { sortOrder = 0, lessonId },
                    new { sortOrder = 1, challengeId }
                ])
            ]));
        create.EnsureSuccessStatusCode();
        var createJson = await create.Content.ReadAsStringAsync();
        var pathId = ReadGuid(createJson, "pathId");
        var revisionId = ReadGuid(createJson, "revisionId");

        var draftPreview = await admin.GetAsync($"/api/admin/learning-paths/{pathId}/draft/preview");
        draftPreview.EnsureSuccessStatusCode();
        var draftPreviewJson = await draftPreview.Content.ReadAsStringAsync();
        Assert.Contains("Lesson title", draftPreviewJson);
        Assert.DoesNotContain("# Markdown body", draftPreviewJson);

        var publish = await admin.PostAsJsonAsync($"/api/admin/learning-paths/{pathId}/publish",
            new { rowVersion = ReadUInt(createJson, "rowVersion") });
        publish.EnsureSuccessStatusCode();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var path = await db.LearningPaths.SingleAsync(item => item.Id == pathId);
            Assert.Equal(revisionId, path.CurrentPublishedRevisionId);
            Assert.Equal(LearningPathRevisionStatus.Published,
                await db.LearningPathRevisions.Where(item => item.Id == revisionId)
                    .Select(item => item.Status).SingleAsync());
        }

        using var anonymous = factory.CreateClient();
        var list = await anonymous.GetAsync("/api/learning-paths");
        list.EnsureSuccessStatusCode();
        Assert.Contains(slug, await list.Content.ReadAsStringAsync());
        var preview = await anonymous.GetAsync($"/api/learning-paths/{slug}/preview");
        preview.EnsureSuccessStatusCode();
        var previewJson = await preview.Content.ReadAsStringAsync();
        Assert.Contains("Lesson title", previewJson);
        Assert.Contains("Published challenge", previewJson);
        Assert.DoesNotContain("# Markdown body", previewJson);
        Assert.DoesNotContain("flag{sensitive}", previewJson);
    }

    [Fact]
    public async Task StaleRevisionRowVersionReturnsConflictCode()
    {
        using var admin = await CreateAdminClientAsync();
        var slug = $"conflict-{Guid.NewGuid():N}";
        var create = await admin.PostAsJsonAsync("/api/admin/learning-paths", PathCommand(slug));
        create.EnsureSuccessStatusCode();
        var createJson = await create.Content.ReadAsStringAsync();
        var pathId = ReadGuid(createJson, "pathId");
        var staleVersion = ReadUInt(createJson, "rowVersion");

        var firstEdit = await admin.PutAsJsonAsync($"/api/admin/learning-paths/{pathId}/draft",
            PathCommand(slug, rowVersion: staleVersion, title: "First edit"));
        firstEdit.EnsureSuccessStatusCode();

        var staleEdit = await admin.PutAsJsonAsync($"/api/admin/learning-paths/{pathId}/draft",
            PathCommand(slug, rowVersion: staleVersion, title: "Stale edit"));
        Assert.Equal(HttpStatusCode.Conflict, staleEdit.StatusCode);
        var error = JsonNode.Parse(await staleEdit.Content.ReadAsStringAsync());
        Assert.Equal("learning.revision_conflict", error?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task EditingAfterPublishCreatesNewDraftAndLeavesPublishedGraphUnchanged()
    {
        using var admin = await CreateAdminClientAsync();
        var lessonId = await SeedLessonAsync();
        var slug = $"immutable-{Guid.NewGuid():N}";
        var create = await admin.PostAsJsonAsync("/api/admin/learning-paths",
            PathCommand(slug, modules: [ModuleCommand(items: [new { sortOrder = 0, lessonId }])]));
        create.EnsureSuccessStatusCode();
        var createJson = await create.Content.ReadAsStringAsync();
        var pathId = ReadGuid(createJson, "pathId");
        var publishedRevisionId = ReadGuid(createJson, "revisionId");

        var publish = await admin.PostAsJsonAsync($"/api/admin/learning-paths/{pathId}/publish",
            new { rowVersion = ReadUInt(createJson, "rowVersion") });
        publish.EnsureSuccessStatusCode();

        var draft = await admin.GetAsync($"/api/admin/learning-paths/{pathId}/draft");
        draft.EnsureSuccessStatusCode();
        var draftJson = await draft.Content.ReadAsStringAsync();
        var draftRevisionId = ReadGuid(draftJson, "revisionId");
        Assert.NotEqual(publishedRevisionId, draftRevisionId);

        var edit = await admin.PutAsJsonAsync($"/api/admin/learning-paths/{pathId}/draft",
            PathCommand(slug, rowVersion: ReadUInt(draftJson, "rowVersion"), title: "Edited draft"));
        edit.EnsureSuccessStatusCode();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var path = await db.LearningPaths.Include(item => item.CurrentPublishedRevision)
                .SingleAsync(item => item.Id == pathId);
            Assert.Equal(publishedRevisionId, path.CurrentPublishedRevisionId);
            Assert.Equal(LearningPathRevisionStatus.Published, path.CurrentPublishedRevision!.Status);
            Assert.Equal("Path title", await db.LearningPathLocalizations
                .Where(item => item.PathId == pathId && item.Locale == "en")
                .Select(item => item.Title).SingleAsync());
        }

        using var anonymous = factory.CreateClient();
        var preview = await anonymous.GetAsync($"/api/learning-paths/{slug}/preview");
        preview.EnsureSuccessStatusCode();
        Assert.Contains("Path title", await preview.Content.ReadAsStringAsync());
        Assert.DoesNotContain("Edited draft", await preview.Content.ReadAsStringAsync());
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        const string password = "S05!AdminPassword";
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), password, role: Role.Admin);
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    private async Task<Guid> SeedChallengeAsync(ChallengePublicationState state)
    {
        var id = Guid.CreateVersion7();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Challenges.Add(new Challenge
        {
            Id = id,
            Type = ChallengeType.StaticAttachment,
            PublicationState = state,
            SourceType = "native",
            SourceId = $"s05-{id:N}",
            Localizations =
            [
                new ChallengeLocalization
                {
                    Locale = "en", Title = "Published challenge", Summary = "Summary",
                    Body = "Challenge body"
                }
            ],
            Flags =
            [new ChallengeFlag { Kind = ChallengeFlagKind.Static, Value = "flag{sensitive}" }]
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedLessonAsync()
    {
        var id = Guid.CreateVersion7();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Lessons.Add(new Lesson
        {
            Id = id,
            Localizations =
            [new LessonLocalization { Locale = "en", Title = "Lesson title", Body = "# Markdown body" }]
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<LearningPathRevisionStatus> ReadRevisionStatusAsync(Guid revisionId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.LearningPathRevisions.Where(item => item.Id == revisionId)
            .Select(item => item.Status).SingleAsync();
    }

    private static object PathCommand(
        string slug,
        object[]? localizations = null,
        object[]? modules = null,
        uint? rowVersion = null,
        string title = "Path title") => new
    {
        slug,
        rowVersion,
        localizations = localizations ?? [new { locale = "en", title, summary = "Path summary" }],
        modules = modules ?? [ModuleCommand(items: [])]
    };

    private static object ModuleCommand(object[]? items = null) => new
    {
        sortOrder = 0,
        expectedMinutes = 15,
        localizations = new[] { new { locale = "en", title = "Module title", summary = "Module summary" } },
        items = items ?? [new { sortOrder = 0, lessonId = Guid.CreateVersion7() }]
    };

    private static Guid ReadGuid(string json, string property)
    {
        var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("Expected JSON response");
        return Guid.Parse(node[property]?.GetValue<string>()
                          ?? node["data"]?[property]?.GetValue<string>()
                          ?? throw new InvalidOperationException($"Missing {property}"));
    }

    private static uint ReadUInt(string json, string property)
    {
        var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("Expected JSON response");
        return node[property]?.GetValue<uint>()
               ?? node["data"]?[property]?.GetValue<uint>()
               ?? throw new InvalidOperationException($"Missing {property}");
    }
}
