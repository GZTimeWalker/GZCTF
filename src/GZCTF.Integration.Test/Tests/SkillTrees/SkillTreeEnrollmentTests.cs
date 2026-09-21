using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Features.SkillTrees.Application;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Integration.Test.Tests.SkillTrees;

[Collection(nameof(IntegrationTestCollection))]
public class SkillTreeEnrollmentTests(GZCTFApplicationFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new DateTimeOffsetJsonConverter() }
    };

    [Fact]
    public async Task Shared_solve_counts_in_every_tree_without_duplicate_progress_rows()
    {
        var seed = await SeedTreesAsync();
        var (user, client) = await CreateUserClientAsync();

        await client.PostAsync($"/api/skill-tree-enrollments/{seed.FirstTreeId}", null);
        await client.PostAsync($"/api/skill-tree-enrollments/{seed.SecondTreeId}", null);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ChallengeProgress.Add(new ChallengeProgress
            {
                UserId = user.Id,
                ChallengeId = seed.ChallengeId,
                SolvedAtUtc = DateTimeOffset.UtcNow,
                SolveMode = ChallengeSolveMode.Independent
            });
            await db.SaveChangesAsync();
        }

        var record = await client.GetFromJsonAsync<MyLearningResponse>("/api/my-learning", JsonOptions);
        Assert.NotNull(record);
        Assert.Equal(2, record!.SkillTrees.Count);
        Assert.All(record.SkillTrees, tree =>
        {
            Assert.Equal(1, tree.ChallengeCount);
            Assert.Equal(1, tree.CompletedChallengeCount);
        });
    }

    [Fact]
    public async Task First_enrollment_becomes_current_and_leaving_clears_it()
    {
        var seed = await SeedTreesAsync();
        var (_, client) = await CreateUserClientAsync();

        var enrolled = await client.PostAsJsonAsync($"/api/skill-tree-enrollments/{seed.FirstTreeId}", new { });
        enrolled.EnsureSuccessStatusCode();
        var first = await enrolled.Content.ReadFromJsonAsync<SkillTreeEnrollmentResponse>(JsonOptions);
        Assert.True(first!.IsCurrent);

        var second = await client.PostAsJsonAsync($"/api/skill-tree-enrollments/{seed.SecondTreeId}", new { });
        second.EnsureSuccessStatusCode();
        var secondEnrollment = await second.Content.ReadFromJsonAsync<SkillTreeEnrollmentResponse>(JsonOptions);
        Assert.False(secondEnrollment!.IsCurrent);

        var select = await client.PutAsJsonAsync($"/api/skill-tree-enrollments/{seed.SecondTreeId}/current", new { });
        select.EnsureSuccessStatusCode();
        var selected = await select.Content.ReadFromJsonAsync<SkillTreeEnrollmentResponse>(JsonOptions);
        Assert.True(selected!.IsCurrent);

        var leave = await client.DeleteAsync($"/api/skill-tree-enrollments/{seed.SecondTreeId}");
        Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);
        var record = await client.GetFromJsonAsync<MyLearningResponse>("/api/my-learning", JsonOptions);
        Assert.Null(record!.CurrentSkillTreeId);
    }

    [Fact]
    public async Task Deleting_current_tree_keeps_history_and_clears_pointer()
    {
        var seed = await SeedTreesAsync();
        var (_, client) = await CreateUserClientAsync();
        await client.PostAsync($"/api/skill-tree-enrollments/{seed.FirstTreeId}", null);

        using var admin = await CreateAdminClientAsync();
        var impact = await admin.GetFromJsonAsync<SkillTreeDeleteImpactResponse>(
            $"/api/admin/skill-trees/{seed.FirstTreeId}/delete-impact", JsonOptions);
        Assert.NotNull(impact);
        Assert.True(impact!.RequiresTypedConfirmation);

        var delete = await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete,
            $"/api/admin/skill-trees/{seed.FirstTreeId}")
        {
            Content = JsonContent.Create(new DeleteSkillTreeCommand(impact.Name, impact.RowVersion))
        });
        delete.EnsureSuccessStatusCode();

        var record = await client.GetFromJsonAsync<MyLearningResponse>("/api/my-learning", JsonOptions);
        Assert.Null(record!.CurrentSkillTreeId);
        Assert.Contains(record.SkillTrees, tree => tree.SkillTreeId == seed.FirstTreeId && tree.IsDeleted);
    }

    private async Task<(Guid FirstTreeId, Guid SecondTreeId, Guid ChallengeId)> SeedTreesAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = new CanonicalChallenge
        {
            Id = Guid.CreateVersion7(),
            Type = ChallengeType.StaticAttachment,
            PublicationState = ChallengePublicationState.Published,
            IsEnabled = true
        };
        challenge.Localizations.Add(new ChallengeLocalization { Locale = "en", Title = "Shared challenge" });

        var category = new SkillCategory { Id = Guid.CreateVersion7(), Name = "Shared", Summary = "", IconKey = "web" };
        category.Contents.Add(new CategoryContent { Category = category, CategoryId = category.Id, Challenge = challenge, ChallengeId = challenge.Id, SortOrder = 0 });

        var first = BuildTree("First", "flag", category);
        var second = BuildTree("Second", "pwn", category);
        db.Challenges.Add(challenge);
        db.SkillTrees.AddRange(first.Tree, second.Tree);
        await db.SaveChangesAsync();
        await SetCurrentAsync(db, first.Tree.Id, first.Revision.Id);
        await SetCurrentAsync(db, second.Tree.Id, second.Revision.Id);

        return (first.Tree.Id, second.Tree.Id, challenge.Id);
    }

    private static (SkillTree Tree, SkillTreeRevision Revision) BuildTree(string name, string icon, SkillCategory category)
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

    private static async Task SetCurrentAsync(AppDbContext db, Guid treeId, Guid revisionId) =>
        await db.SkillTrees.Where(item => item.Id == treeId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CurrentPublishedRevisionId, revisionId));

    private async Task<(TestDataSeeder.SeededUser User, HttpClient Client)> CreateUserClientAsync()
    {
        const string password = "S12!UserPassword";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        login.EnsureSuccessStatusCode();
        return (user, client);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        const string password = "S12!AdminPassword";
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), password, role: Role.Admin);
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        login.EnsureSuccessStatusCode();
        return client;
    }
}
