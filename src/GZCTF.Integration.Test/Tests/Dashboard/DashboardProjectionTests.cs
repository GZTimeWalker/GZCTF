using System.Net.Http.Json;
using System.Text.Json.Nodes;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.Dashboard.Application;
using GZCTF.Features.Dashboard.Domain;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Integration.Test.Tests.DashboardProjection;

/// <summary>
/// ST27 release gate. Proves that a challenge shared by several skill tree categories is
/// counted exactly once by the large-screen dashboard, no matter how the snapshot is filtered.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class DashboardProjectionTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Shared_category_solve_counts_once_in_series_and_leaderboard()
    {
        var seed = await SeedSharedReferenceAsync();
        var (dashboardId, rawToken) = await CreateDashboardWithTokenAsync();

        // Solve the single shared challenge once through the normal submission path so the
        // daily projection records it exactly like any other learner action.
        var accepted = await seed.Client.PostAsJsonAsync(
            $"/api/challenges/{seed.ChallengeId}/submissions", new { flag = "flag{s27-shared}" });
        accepted.EnsureSuccessStatusCode();

        var unfiltered = await GetSnapshotAsync(dashboardId, rawToken);
        var member = FindMember(unfiltered, seed.UserName);
        Assert.Equal(1, member["uniqueSolvedCount"]!.GetValue<int>());

        var points = member["points"]!.AsArray();
        Assert.NotEmpty(points);
        Assert.Equal(1, points[^1]!["value"]!.GetValue<int>());

        // Narrowing by cohort and by search must read the same global progress source.
        var byCohort = await GetSnapshotAsync(dashboardId, rawToken, cohortId: seed.CohortId);
        Assert.Equal(1, FindMember(byCohort, seed.UserName)["uniqueSolvedCount"]!.GetValue<int>());

        var bySearch = await GetSnapshotAsync(dashboardId, rawToken, search: seed.UserName);
        Assert.Equal(1, FindMember(bySearch, seed.UserName)["uniqueSolvedCount"]!.GetValue<int>());
        Assert.Equal(1, Assert.Single(bySearch["leaderboard"]!.AsArray())!["uniqueSolvedCount"]!.GetValue<int>());
        Assert.Equal(1, Assert.Single(bySearch["leaderboard"]!.AsArray())!["rank"]!.GetValue<int>());

        // A cohort the learner does not belong to must not surface the solve at all.
        var otherCohort = await GetSnapshotAsync(dashboardId, rawToken, cohortId: seed.OtherCohortId);
        Assert.DoesNotContain(otherCohort["members"]!.AsArray(),
            item => item!["userName"]!.GetValue<string>() == seed.UserName);

        // Rebuilding the projection from global ChallengeProgress keeps the same totals.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var projection = scope.ServiceProvider.GetRequiredService<DailySolveProjection>();
            await projection.RebuildAsync();
        }

        var rebuilt = await GetSnapshotAsync(dashboardId, rawToken, search: seed.UserName);
        Assert.Equal(1, FindMember(rebuilt, seed.UserName)["uniqueSolvedCount"]!.GetValue<int>());
        Assert.Equal(1, Assert.Single(rebuilt["leaderboard"]!.AsArray())!["uniqueSolvedCount"]!.GetValue<int>());
    }

    private static JsonNode FindMember(JsonObject snapshot, string userName) =>
        Assert.Single(snapshot["members"]!.AsArray(),
            item => item!["userName"]!.GetValue<string>() == userName)!;

    private async Task<JsonObject> GetSnapshotAsync(
        Guid dashboardId, string rawToken, Guid? cohortId = null, string? search = null)
    {
        var query = $"token={Uri.EscapeDataString(rawToken)}";
        if (cohortId is not null) query += $"&cohortId={cohortId}";
        if (search is not null) query += $"&search={Uri.EscapeDataString(search)}";
        using var client = factory.CreateClient();
        return await client.GetFromJsonAsync<JsonObject>($"/api/dashboards/{dashboardId}?{query}")
               ?? throw new InvalidOperationException("Empty snapshot.");
    }

    private async Task<(Guid DashboardId, string RawToken)> CreateDashboardWithTokenAsync()
    {
        const string password = "Dashboard!Projection9";
        var admin = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), password, role: Role.Admin);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = password });
        login.EnsureSuccessStatusCode();

        var createDashboard = await client.PostAsJsonAsync("/api/admin/dashboards", new
        {
            name = $"Dashboard-{Guid.NewGuid():N}",
            topCount = 10
        });
        createDashboard.EnsureSuccessStatusCode();
        var dashboardId = (await createDashboard.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<Guid>();

        var createToken = await client.PostAsJsonAsync($"/api/admin/dashboards/{dashboardId}/tokens", new { });
        createToken.EnsureSuccessStatusCode();
        var rawToken = (await createToken.Content.ReadFromJsonAsync<JsonObject>())!["rawToken"]!.GetValue<string>();
        return (dashboardId, rawToken);
    }

    private async Task<Seed> SeedSharedReferenceAsync()
    {
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "S27!SharedSolve");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challengeId = Guid.CreateVersion7();
        var challenge = new CanonicalChallenge
        {
            Id = challengeId,
            Type = ChallengeType.StaticAttachment,
            PublicationState = ChallengePublicationState.Published,
            IsEnabled = true,
            SourceType = "s27-dashboard",
            SourceId = challengeId.ToString("N"),
            Localizations = [new ChallengeLocalization { Locale = "en", Title = "Shared dashboard challenge" }],
            Flags = [new ChallengeFlag { Kind = ChallengeFlagKind.Static, Value = "flag{s27-shared}" }]
        };

        var firstCategoryId = Guid.CreateVersion7();
        var secondCategoryId = Guid.CreateVersion7();
        var categories = new[]
        {
            new SkillCategory
            {
                Id = firstCategoryId, Name = $"Shared A {firstCategoryId:N}", Summary = "", IconKey = "web"
            },
            new SkillCategory
            {
                Id = secondCategoryId, Name = $"Shared B {secondCategoryId:N}", Summary = "", IconKey = "pwn"
            }
        };
        foreach (var category in categories)
            category.Contents.Add(new CategoryContent
            {
                Id = Guid.CreateVersion7(),
                CategoryId = category.Id,
                ChallengeId = challengeId,
                SortOrder = 0
            });

        var cohortId = Guid.CreateVersion7();
        var otherCohortId = Guid.CreateVersion7();
        var cohort = new Cohort { Id = cohortId, Name = $"Cohort-{cohortId:N}" };
        var otherCohort = new Cohort { Id = otherCohortId, Name = $"Cohort-{otherCohortId}" };

        var treeIds = new[] { Guid.CreateVersion7(), Guid.CreateVersion7() };
        var trees = new List<SkillTree>();
        for (var index = 0; index < treeIds.Length; index++)
        {
            var treeId = treeIds[index];
            var revisionId = Guid.CreateVersion7();
            var revision = new SkillTreeRevision
            {
                Id = revisionId,
                SkillTreeId = treeId,
                Status = SkillTreeRevisionStatus.Published,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                PublishedAtUtc = DateTimeOffset.UtcNow
            };
            foreach (var category in categories)
                revision.Categories.Add(new SkillTreeCategoryRef
                {
                    Id = Guid.CreateVersion7(),
                    RevisionId = revisionId,
                    CategoryId = category.Id,
                    Category = category,
                    SortOrder = revision.Categories.Count
                });

            trees.Add(new SkillTree
            {
                Id = treeId,
                Name = $"Shared tree {index} {treeId:N}",
                Summary = "",
                IconKey = "web",
                Revisions = [revision]
            });
        }

        var account = await db.Users.SingleAsync(item => item.Id == user.Id);
        account.CohortId = cohortId;
        cohort.Users.Add(account);

        db.Challenges.Add(challenge);
        db.SkillCategories.AddRange(categories);
        db.SkillTrees.AddRange(trees);
        db.Cohorts.AddRange(cohort, otherCohort);
        await db.SaveChangesAsync();

        foreach (var treeId in treeIds)
            await db.SkillTrees.Where(item => item.Id == treeId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(item => item.CurrentPublishedRevisionId,
                        trees.Single(tree => tree.Id == treeId).Revisions[0].Id));

        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = "S27!SharedSolve" });
        login.EnsureSuccessStatusCode();

        return new Seed(user.UserName, challengeId, cohortId, otherCohortId, client);
    }

    private sealed record Seed(
        string UserName,
        Guid ChallengeId,
        Guid CohortId,
        Guid OtherCohortId,
        HttpClient Client);
}
