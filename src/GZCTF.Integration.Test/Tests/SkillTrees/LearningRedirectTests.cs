using System.Net;
using System.Net.Http.Json;
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

namespace GZCTF.Integration.Test.Tests.SkillTrees;

[Collection(nameof(IntegrationTestCollection))]
public class LearningRedirectTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Old_slug_redirects_to_stable_skill_tree_id()
    {
        using var admin = await CreateAdminClientAsync();
        var tree = await CreateTreeAsync(admin, "RedirectTree", "flag");
        var oldSlug = $"old-slug-{tree.SkillTreeId}";

        await InsertRedirectAsync(oldSlug, tree.SkillTreeId);

        using var anon = factory.CreateClient();
        var response = await anon.GetFromJsonAsync<LearningRedirectResponse>($"/api/skill-tree-redirects/{oldSlug}");
        Assert.Equal($"/skill-trees/{tree.SkillTreeId}", response!.TargetPath);
    }

    [Fact]
    public async Task Old_deep_link_resolves_to_content_workspace()
    {
        using var admin = await CreateAdminClientAsync();
        var tree = await CreateTreeAsync(admin, "DeepLink", "web");
        var categoryId = await SeedCategoryAsync("DeepCat");
        var challengeId = await SeedChallengeAsync("DeepChallenge", categoryId, tree.SkillTreeId);

        var oldSlug = $"deep-slug-{tree.SkillTreeId}";
        await InsertRedirectAsync(oldSlug, tree.SkillTreeId);

        // debug: verify redirect exists
        await using var check = factory.Services.CreateAsyncScope();
        var checkDb = check.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = await checkDb.LearningPathRedirects.AnyAsync(r => r.OldSlug == oldSlug);
        Assert.True(exists, "redirect not inserted");

        using var anon = factory.CreateClient();
        var response = await anon.GetFromJsonAsync<LearningRedirectResponse>(
            $"/api/skill-tree-redirects/{oldSlug}?moduleId={categoryId}&itemId={challengeId}");

        Assert.Equal(
            $"/skill-trees/{tree.SkillTreeId}/{categoryId}/challenge/{challengeId}",
            response!.TargetPath);
    }

    [Fact]
    public async Task Lesson_deep_link_resolves_correctly()
    {
        using var admin = await CreateAdminClientAsync();
        var tree = await CreateTreeAsync(admin, "LessonLink", "brain");
        var categoryId = await SeedCategoryAsync("LessonCat");
        var lessonId = await SeedLessonAsync("DeepLesson", categoryId, tree.SkillTreeId);

        var oldSlug = $"lesson-slug-{tree.SkillTreeId}";
        await InsertRedirectAsync(oldSlug, tree.SkillTreeId);

        using var anon = factory.CreateClient();
        var response = await anon.GetFromJsonAsync<LearningRedirectResponse>(
            $"/api/skill-tree-redirects/{oldSlug}?moduleId={categoryId}&itemId={lessonId}");

        Assert.Equal(
            $"/skill-trees/{tree.SkillTreeId}/{categoryId}/lesson/{lessonId}",
            response!.TargetPath);
    }

    [Fact]
    public async Task Unknown_slug_returns_not_found()
    {
        using var anon = factory.CreateClient();
        var response = await anon.GetAsync("/api/skill-tree-redirects/nope");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Mismatched_module_item_returns_not_found()
    {
        using var admin = await CreateAdminClientAsync();
        var tree = await CreateTreeAsync(admin, "Mismatch", "flag");
        var categoryId = await SeedCategoryAsync("MismatchCat");
        var challengeId = await SeedChallengeAsync("MismatchChallenge", categoryId, tree.SkillTreeId);

        var oldSlug = $"mismatch-{tree.SkillTreeId}";
        await InsertRedirectAsync(oldSlug, tree.SkillTreeId);

        using var anon = factory.CreateClient();
        var response = await anon.GetAsync(
            $"/api/skill-tree-redirects/{oldSlug}?moduleId={categoryId}&itemId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Deleted_tree_returns_not_found()
    {
        using var admin = await CreateAdminClientAsync();
        var tree = await CreateTreeAsync(admin, "DeletedTree", "flag");
        var oldSlug = $"deleted-{tree.SkillTreeId}";

        await InsertRedirectAsync(oldSlug, tree.SkillTreeId);

        using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await db.SkillTrees.FirstAsync(t => t.Id == tree.SkillTreeId);
        entity.DeletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        using var anon = factory.CreateClient();
        var response = await anon.GetAsync($"/api/skill-tree-redirects/{oldSlug}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task InsertRedirectAsync(string oldSlug, Guid skillTreeId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.LearningPathRedirects.Add(new LearningPathRedirect
        {
            LearningPathId = skillTreeId,
            OldSlug = oldSlug,
            SkillTreeId = skillTreeId
        });
        await db.SaveChangesAsync();
    }

    private static async Task<AdminSkillTreeResponse> CreateTreeAsync(HttpClient admin, string name, string iconKey)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/skill-trees",
            new CreateSkillTreeCommand(name, "Test summary", iconKey));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AdminSkillTreeResponse>())!;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        const string password = "S07!AdminPassword";
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), password, role: Role.Admin);
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    private async Task<Guid> SeedCategoryAsync(string name)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = new SkillCategory
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Summary = name,
            IconKey = "flag"
        };
        db.SkillCategories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private async Task<Guid> SeedChallengeAsync(string title, Guid categoryId, Guid treeId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = new Challenge
        {
            Id = Guid.CreateVersion7(),
            Type = ChallengeType.StaticAttachment,
            Difficulty = Difficulty.Normal,
            PublicationState = ChallengePublicationState.Published,
            IsEnabled = true,
            ExpectedMinutes = 10,
            Localizations = new List<ChallengeLocalization>
            {
                new ChallengeLocalization { Locale = "en", Title = title, Summary = title, Body = "body" }
            }
        };
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();

        var content = new CategoryContent
        {
            CategoryId = categoryId,
            ChallengeId = challenge.Id,
            SortOrder = 0
        };
        db.CategoryContents.Add(content);

        var tree = await db.SkillTrees.Include(t => t.Revisions).FirstAsync(t => t.Id == treeId);
        var draftRevision = tree.Revisions.First(r => r.Status == SkillTreeRevisionStatus.Draft);
        db.SkillTreeCategoryRefs.Add(new SkillTreeCategoryRef
        {
            RevisionId = draftRevision.Id,
            CategoryId = categoryId,
            SortOrder = 0
        });
        await db.SaveChangesAsync();

        return content.Id;
    }

    private async Task<Guid> SeedLessonAsync(string title, Guid categoryId, Guid treeId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lesson = new Lesson
        {
            Id = Guid.CreateVersion7(),
            PublicationState = LessonPublicationState.Published,
            Localizations = new List<LessonLocalization>
            {
                new LessonLocalization { Locale = "en", Title = title, Body = "body" }
            }
        };
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();

        var content = new CategoryContent
        {
            CategoryId = categoryId,
            LessonId = lesson.Id,
            SortOrder = 0
        };
        db.CategoryContents.Add(content);

        var tree = await db.SkillTrees.Include(t => t.Revisions).FirstAsync(t => t.Id == treeId);
        var draftRevision = tree.Revisions.First(r => r.Status == SkillTreeRevisionStatus.Draft);
        db.SkillTreeCategoryRefs.Add(new SkillTreeCategoryRef
        {
            RevisionId = draftRevision.Id,
            CategoryId = categoryId,
            SortOrder = 0
        });
        await db.SaveChangesAsync();

        return content.Id;
    }
}
