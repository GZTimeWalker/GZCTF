using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.SkillTrees.Application;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Integration.Test.Tests.SkillTrees;

[Collection(nameof(IntegrationTestCollection))]
public class ContentPublicationTests(GZCTFApplicationFactory factory)
{
    [Theory]
    [InlineData("challenge")]
    [InlineData("lesson")]
    public async Task Draft_can_be_uncategorized_but_publish_requires_active_tree_category(string kind)
    {
        using var admin = await CreateAdminClientAsync();

        var missing = await PublishAsync(admin, kind, await SeedDraftAsync(kind), [], []);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("content_category_required", await ReadCodeAsync(missing));

        var orphanCategoryId = await SeedOrphanCategoryAsync();
        var noTree = await PublishAsync(admin, kind, await SeedDraftAsync(kind), [orphanCategoryId], []);
        Assert.Equal(HttpStatusCode.BadRequest, noTree.StatusCode);
        Assert.Equal("content_category_has_no_active_tree", await ReadCodeAsync(noTree));
    }

    [Fact]
    public async Task Published_content_appears_in_every_referencing_tree()
    {
        var (treeId, categoryId) = await SeedPublishedTreeAsync();
        var challenge = await SeedDraftAsync("challenge");
        using var admin = await CreateAdminClientAsync();

        var publish = await PublishAsync(admin, "challenge", challenge, [categoryId], []);
        publish.EnsureSuccessStatusCode();

        using var anon = factory.CreateClient();
        var detail = await anon.GetFromJsonAsync<SkillTreeDetailResponse>($"/api/skill-trees/{treeId}");
        var category = Assert.Single(detail!.Categories);
        Assert.Contains(category.Contents, item => item.ContentId == challenge.Id && item.Kind == "challenge");
    }

    [Fact]
    public async Task Publishing_rejects_missing_mode_specific_flag_or_runtime_configuration()
    {
        var (_, categoryId) = await SeedPublishedTreeAsync();
        using var admin = await CreateAdminClientAsync();

        var noFlag = await PublishAsync(admin, "challenge",
            await SeedDraftAsync("challenge", ChallengeType.StaticAttachment, includeFlag: false), [categoryId], []);
        Assert.Equal(HttpStatusCode.BadRequest, noFlag.StatusCode);
        Assert.Equal("content_static_flag_required", await ReadCodeAsync(noFlag));

        var noContainer = await PublishAsync(admin, "challenge",
            await SeedDraftAsync("challenge", ChallengeType.DynamicContainer), [categoryId], []);
        Assert.Equal(HttpStatusCode.BadRequest, noContainer.StatusCode);
        Assert.Equal("content_container_configuration_required", await ReadCodeAsync(noContainer));

        var noAttachmentPool = await PublishAsync(admin, "challenge",
            await SeedDraftAsync("challenge", ChallengeType.DynamicAttachment), [categoryId], []);
        Assert.Equal(HttpStatusCode.BadRequest, noAttachmentPool.StatusCode);
        Assert.Equal("content_attachment_pool_required", await ReadCodeAsync(noAttachmentPool));
    }

    [Fact]
    public async Task Publishing_rejects_an_attachment_that_is_missing_from_storage()
    {
        var (_, categoryId) = await SeedPublishedTreeAsync();
        var challengeId = Guid.CreateVersion7();
        uint rowVersion;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var challenge = new CanonicalChallenge
            {
                Id = challengeId,
                Type = ChallengeType.DynamicAttachment,
                PublicationState = ChallengePublicationState.Draft,
                SourceType = "native",
                SourceId = challengeId.ToString("N"),
                Localizations = [new ChallengeLocalization { Locale = "en", Title = "Missing file" }],
                Flags = [new ChallengeFlag
                {
                    Kind = ChallengeFlagKind.DynamicAttachment,
                    MetadataJson = JsonSerializer.Serialize(new[]
                    {
                        new { FileName = "missing.zip", StorageKey = $"uploads/ff/ff/{Guid.NewGuid():N}",
                            Sha256 = "ff" + new string('0', 62), Flag = "flag{missing}" }
                    })
                }]
            };
            db.Challenges.Add(challenge);
            await db.SaveChangesAsync();
            rowVersion = challenge.RowVersion;
        }

        using var admin = await CreateAdminClientAsync();
        var publish = await PublishAsync(admin, "challenge", (challengeId, rowVersion), [categoryId], []);
        Assert.Equal(HttpStatusCode.BadRequest, publish.StatusCode);
        Assert.Equal("content_attachment_missing", await ReadCodeAsync(publish));
    }

    [Fact]
    public async Task Inline_category_publishes_an_untouched_tree()
    {
        var treeId = await SeedDraftTreeAsync();
        var lesson = await SeedDraftAsync("lesson");
        using var admin = await CreateAdminClientAsync();

        var publish = await PublishAsync(admin, "lesson", lesson,
            [], [new InlineSkillCategoryCommand(treeId, "Inline", "Inline summary", "brain")]);
        publish.EnsureSuccessStatusCode();

        using var anon = factory.CreateClient();
        var detail = await anon.GetFromJsonAsync<SkillTreeDetailResponse>($"/api/skill-trees/{treeId}");
        var category = Assert.Single(detail!.Categories);
        Assert.Equal("Inline", category.Name);
        Assert.Contains(category.Contents, item => item.ContentId == lesson.Id);
    }

    [Fact]
    public async Task Inline_category_publishes_a_published_tree_without_a_draft()
    {
        // Cloning the draft for a published tree used to be force-attached as an existing
        // row, failing the publish with a bogus revision conflict.
        var (treeId, _) = await SeedPublishedTreeAsync();
        var lesson = await SeedDraftAsync("lesson");
        using var admin = await CreateAdminClientAsync();

        var publish = await PublishAsync(admin, "lesson", lesson,
            [], [new InlineSkillCategoryCommand(treeId, "Inline", "Inline summary", "brain")]);
        publish.EnsureSuccessStatusCode();

        var draft = await admin.GetFromJsonAsync<SkillTreeDraftResponse>($"/api/admin/skill-trees/{treeId}/draft");
        Assert.Contains(draft!.Categories, item => item.Name == "Inline");
    }

    private async Task<HttpResponseMessage> PublishAsync(
        HttpClient admin, string kind, (Guid Id, uint RowVersion) content,
        IReadOnlyList<Guid> categoryIds, IReadOnlyList<InlineSkillCategoryCommand> inline)
    {
        var command = new PublishContentCommand(content.RowVersion, categoryIds, inline);
        var route = kind == "challenge"
            ? $"/api/admin/challenges/{content.Id}/publish"
            : $"/api/admin/lessons/{content.Id}/publish";
        return await admin.PostAsJsonAsync(route, command);
    }

    private async Task<(Guid Id, uint RowVersion)> SeedDraftAsync(
        string kind, ChallengeType challengeType = ChallengeType.StaticAttachment, bool includeFlag = true)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (kind == "challenge")
        {
            var challenge = new CanonicalChallenge
            {
                Id = Guid.CreateVersion7(),
                Type = challengeType,
                PublicationState = ChallengePublicationState.Draft,
                IsEnabled = true,
                Localizations = [new ChallengeLocalization { Locale = "en", Title = "Draft challenge" }],
                Flags = includeFlag ? challengeType switch
                {
                    ChallengeType.StaticAttachment or ChallengeType.StaticContainer =>
                        [new ChallengeFlag { Kind = ChallengeFlagKind.Static, Value = "flag{publish}" }],
                    ChallengeType.DynamicContainer =>
                        [new ChallengeFlag { Kind = ChallengeFlagKind.Template, Template = "flag{{userId}}" }],
                    _ => []
                } : []
            };
            db.Challenges.Add(challenge);
            await db.SaveChangesAsync();
            return (challenge.Id, challenge.RowVersion);
        }
        else
        {
            var lesson = new Lesson
            {
                Id = Guid.CreateVersion7(),
                PublicationState = LessonPublicationState.Draft,
                Localizations = [new LessonLocalization { Locale = "en", Title = "Draft lesson", Body = "Body" }]
            };
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
            return (lesson.Id, lesson.RowVersion);
        }
    }

    private async Task<Guid> SeedOrphanCategoryAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = new SkillCategory { Id = Guid.CreateVersion7(), Name = "Orphan", Summary = "", IconKey = "flag" };
        db.SkillCategories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private async Task<Guid> SeedDraftTreeAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tree = new SkillTree
        {
            Id = Guid.CreateVersion7(),
            Name = "Draft tree",
            Summary = "",
            IconKey = "pwn",
            Revisions =
            [
                new SkillTreeRevision
                {
                    Id = Guid.CreateVersion7(),
                    Status = SkillTreeRevisionStatus.Draft,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                }
            ]
        };
        db.SkillTrees.Add(tree);
        await db.SaveChangesAsync();
        return tree.Id;
    }

    private async Task<(Guid TreeId, Guid CategoryId)> SeedPublishedTreeAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new SkillCategory { Id = Guid.CreateVersion7(), Name = "Shared", Summary = "", IconKey = "web" };
        var revision = new SkillTreeRevision
        {
            Id = Guid.CreateVersion7(),
            Status = SkillTreeRevisionStatus.Published,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            PublishedAtUtc = DateTimeOffset.UtcNow
        };
        revision.Categories.Add(new SkillTreeCategoryRef { Category = category, CategoryId = category.Id, SortOrder = 0 });
        var tree = new SkillTree
        {
            Id = Guid.CreateVersion7(),
            Name = "Published tree",
            Summary = "",
            IconKey = "flag",
            Revisions = [revision]
        };
        db.SkillTrees.Add(tree);
        await db.SaveChangesAsync();
        await db.SkillTrees.Where(item => item.Id == tree.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CurrentPublishedRevisionId, revision.Id));

        return (tree.Id, category.Id);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        const string password = "S10!AdminPassword";
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), password, role: Role.Admin);
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<string> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() ?? "" : "";
    }
}
