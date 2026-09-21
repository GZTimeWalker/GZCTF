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
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Integration.Test.Tests.SkillTrees;

[Collection(nameof(IntegrationTestCollection))]
public class SkillCategoryTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Reordering_one_category_changes_every_referencing_tree()
    {
        var seed = await SeedSharedCategoryAsync();
        using var admin = await CreateAdminClientAsync();

        var response = await admin.PutAsJsonAsync($"/api/admin/skill-categories/{seed.CategoryId}/contents",
            new UpdateCategoryContentsCommand(seed.CategoryRowVersion,
                [new CategoryContentOrderCommand("lesson", seed.LessonId, 0),
                 new CategoryContentOrderCommand("challenge", seed.ChallengeId, 1)]));
        response.EnsureSuccessStatusCode();

        using var anon = factory.CreateClient();
        var first = await anon.GetFromJsonAsync<SkillTreeDetailResponse>($"/api/skill-trees/{seed.FirstTreeId}");
        var second = await anon.GetFromJsonAsync<SkillTreeDetailResponse>($"/api/skill-trees/{seed.SecondTreeId}");
        var expected = new[] { seed.LessonId, seed.ChallengeId };
        Assert.Equal(expected, first!.Categories.Single().Contents.Select(item => item.ContentId));
        Assert.Equal(expected, second!.Categories.Single().Contents.Select(item => item.ContentId));
    }

    [Fact]
    public async Task Merge_deduplicates_refs_and_keeps_survivor_order_first()
    {
        var seed = await SeedMergePairAsync();
        using var admin = await CreateAdminClientAsync();

        var survivor = await admin.GetFromJsonAsync<SkillCategoryAdminResponse>(
            $"/api/admin/skill-categories/{seed.SurvivorId}");
        var duplicate = await admin.GetFromJsonAsync<SkillCategoryAdminResponse>(
            $"/api/admin/skill-categories/{seed.DuplicateId}");

        var merge = await admin.PostAsJsonAsync("/api/admin/skill-categories/merge",
            new MergeSkillCategoryCommand(seed.SurvivorId, seed.DuplicateId,
                survivor!.RowVersion, duplicate!.RowVersion));
        merge.EnsureSuccessStatusCode();

        var merged = await admin.GetFromJsonAsync<SkillCategoryAdminResponse>(
            $"/api/admin/skill-categories/{seed.SurvivorId}");
        Assert.Equal(
            new[] { seed.SurvivorFirstId, seed.SharedId, seed.DuplicateOnlyId },
            merged!.Contents.Select(item => item.ContentId));

        var removed = await admin.GetAsync($"/api/admin/skill-categories/{seed.DuplicateId}");
        Assert.Equal(HttpStatusCode.NotFound, removed.StatusCode);
    }

    [Fact]
    public async Task Delete_hides_category_but_keeps_published_audit_refs()
    {
        var seed = await SeedSharedCategoryAsync();
        using var admin = await CreateAdminClientAsync();

        var category = await admin.GetFromJsonAsync<SkillCategoryAdminResponse>(
            $"/api/admin/skill-categories/{seed.CategoryId}");
        var impact = await admin.GetFromJsonAsync<CategoryDeleteImpactResponse>(
            $"/api/admin/skill-categories/{seed.CategoryId}/delete-impact");
        Assert.True(impact!.RequiresTypedConfirmation);

        var delete = await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete,
            $"/api/admin/skill-categories/{seed.CategoryId}")
        {
            Content = JsonContent.Create(new DeleteCategoryCommand("Shared", category!.RowVersion))
        });
        delete.EnsureSuccessStatusCode();

        using var anon = factory.CreateClient();
        var detail = await anon.GetFromJsonAsync<SkillTreeDetailResponse>($"/api/skill-trees/{seed.FirstTreeId}");
        Assert.Empty(detail!.Categories);
    }

    private async Task<(Guid FirstTreeId, Guid SecondTreeId, Guid CategoryId, uint CategoryRowVersion, Guid ChallengeId, Guid LessonId)>
        SeedSharedCategoryAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = PublishedChallenge("Shared challenge");
        var lesson = PublishedLesson("Shared lesson");
        var category = new SkillCategory { Id = Guid.CreateVersion7(), Name = "Shared", Summary = "", IconKey = "web" };
        category.Contents.Add(new CategoryContent { Category = category, CategoryId = category.Id, Challenge = challenge, ChallengeId = challenge.Id, SortOrder = 0 });
        category.Contents.Add(new CategoryContent { Category = category, CategoryId = category.Id, Lesson = lesson, LessonId = lesson.Id, SortOrder = 1 });
        db.SkillCategories.Add(category);
        db.Challenges.Add(challenge);
        db.Lessons.Add(lesson);

        var first = PublishedTree("First", "flag", category);
        var second = PublishedTree("Second", "pwn", category);
        db.SkillTrees.AddRange(first.Tree, second.Tree);
        await db.SaveChangesAsync();
        await SetCurrentRevisionAsync(db, first.Tree.Id, first.Revision.Id);
        await SetCurrentRevisionAsync(db, second.Tree.Id, second.Revision.Id);

        return (first.Tree.Id, second.Tree.Id, category.Id, category.RowVersion, challenge.Id, lesson.Id);
    }

    private async Task<(Guid SurvivorId, Guid DuplicateId, Guid SurvivorFirstId, Guid SharedId, Guid DuplicateOnlyId)>
        SeedMergePairAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var survivorFirst = PublishedLesson("Survivor first");
        var shared = PublishedChallenge("Shared");
        var duplicateOnly = PublishedLesson("Duplicate only");

        var survivor = new SkillCategory { Id = Guid.CreateVersion7(), Name = "Survivor", Summary = "", IconKey = "flag" };
        survivor.Contents.Add(new CategoryContent { Category = survivor, CategoryId = survivor.Id, Lesson = survivorFirst, LessonId = survivorFirst.Id, SortOrder = 0 });
        survivor.Contents.Add(new CategoryContent { Category = survivor, CategoryId = survivor.Id, Challenge = shared, ChallengeId = shared.Id, SortOrder = 1 });

        var duplicate = new SkillCategory { Id = Guid.CreateVersion7(), Name = "Duplicate", Summary = "", IconKey = "flag" };
        duplicate.Contents.Add(new CategoryContent { Category = duplicate, CategoryId = duplicate.Id, Challenge = shared, ChallengeId = shared.Id, SortOrder = 0 });
        duplicate.Contents.Add(new CategoryContent { Category = duplicate, CategoryId = duplicate.Id, Lesson = duplicateOnly, LessonId = duplicateOnly.Id, SortOrder = 1 });

        db.SkillCategories.AddRange(survivor, duplicate);
        db.Challenges.Add(shared);
        db.Lessons.AddRange(survivorFirst, duplicateOnly);
        await db.SaveChangesAsync();

        return (survivor.Id, duplicate.Id, survivorFirst.Id, shared.Id, duplicateOnly.Id);
    }

    private static (SkillTree Tree, SkillTreeRevision Revision) PublishedTree(string name, string icon, SkillCategory category)
    {
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
            Name = name,
            Summary = "",
            IconKey = icon,
            Revisions = [revision]
        };
        return (tree, revision);
    }

    private static async Task SetCurrentRevisionAsync(AppDbContext db, Guid treeId, Guid revisionId) =>
        await db.SkillTrees.Where(item => item.Id == treeId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CurrentPublishedRevisionId, revisionId));

    private static CanonicalChallenge PublishedChallenge(string title)
    {
        var challenge = new CanonicalChallenge
        {
            Id = Guid.CreateVersion7(),
            Type = ChallengeType.StaticAttachment,
            PublicationState = ChallengePublicationState.Published,
            IsEnabled = true
        };
        challenge.Localizations.Add(new ChallengeLocalization { Locale = "en", Title = title });
        return challenge;
    }

    private static Lesson PublishedLesson(string title)
    {
        var lesson = new Lesson
        {
            Id = Guid.CreateVersion7(),
            PublicationState = LessonPublicationState.Published
        };
        lesson.Localizations.Add(new LessonLocalization { Locale = "en", Title = title, Body = "Body" });
        return lesson;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        const string password = "S08!AdminPassword";
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), password, role: Role.Admin);
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        login.EnsureSuccessStatusCode();
        return client;
    }
}
