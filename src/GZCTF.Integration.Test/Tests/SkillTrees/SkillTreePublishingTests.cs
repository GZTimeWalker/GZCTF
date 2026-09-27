using GZCTF.Features.SkillTrees.Application;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace GZCTF.Integration.Test.Tests.SkillTrees;

[Collection(nameof(IntegrationTestCollection))]
public class SkillTreePublishingTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Empty_tree_can_be_published()
    {
        using var admin = await CreateAdminClientAsync();
        var created = await CreateTreeAsync(admin, "Empty", "flag");

        var publish = await admin.PostAsJsonAsync(
            $"/api/admin/skill-trees/{created.SkillTreeId}/publish",
            new PublishSkillTreeCommand(created.RowVersion));
        Assert.Equal(HttpStatusCode.NoContent, publish.StatusCode);

        using var anon = factory.CreateClient();
        var detail = await anon.GetFromJsonAsync<SkillTreeDetailResponse>($"/api/skill-trees/{created.SkillTreeId}");
        Assert.NotNull(detail);
        Assert.Empty(detail!.Categories);
    }

    [Fact]
    public async Task Stale_row_version_does_not_switch_published_revision()
    {
        var categoryId = await SeedCategoryAsync();
        using var admin = await CreateAdminClientAsync();
        var created = await CreateTreeAsync(admin, "Concurrent", "web");
        var staleVersion = created.RowVersion;

        // First edit succeeds and changes the draft row version
        var firstEdit = await admin.PutAsJsonAsync($"/api/admin/skill-trees/{created.SkillTreeId}/draft",
            new UpdateSkillTreeDraftCommand("Updated", "Updated", "flag", staleVersion,
                new[] { new SkillTreeCategoryOrderCommand(categoryId, 0) }));
        firstEdit.EnsureSuccessStatusCode();

        // Second edit with the same stale row version should fail
        var staleEdit = await admin.PutAsJsonAsync($"/api/admin/skill-trees/{created.SkillTreeId}/draft",
            new UpdateSkillTreeDraftCommand("Stale", "Stale", "flag", staleVersion,
                new[] { new SkillTreeCategoryOrderCommand(categoryId, 0) }));
        Assert.Equal(HttpStatusCode.Conflict, staleEdit.StatusCode);
        Assert.Equal("skill_tree_revision_conflict", await ReadCodeAsync(staleEdit));
    }

    [Fact]
    public async Task Duplicate_category_ids_and_gaps_are_normalized_or_rejected()
    {
        var firstCategoryId = await SeedCategoryAsync();
        var secondCategoryId = await SeedSecondCategoryAsync();
        using var admin = await CreateAdminClientAsync();
        var created = await CreateTreeAsync(admin, "Order", "web");

        var dupes = await admin.PutAsJsonAsync($"/api/admin/skill-trees/{created.SkillTreeId}/draft",
            new UpdateSkillTreeDraftCommand("Order", "Summary", "flag", created.RowVersion,
                new[]
                {
                    new SkillTreeCategoryOrderCommand(firstCategoryId, 0),
                    new SkillTreeCategoryOrderCommand(firstCategoryId, 1)
                }));
        Assert.Equal(HttpStatusCode.BadRequest, dupes.StatusCode);

        var gap = await admin.PutAsJsonAsync($"/api/admin/skill-trees/{created.SkillTreeId}/draft",
            new UpdateSkillTreeDraftCommand("Order", "Summary", "flag", created.RowVersion,
                new[]
                {
                    new SkillTreeCategoryOrderCommand(firstCategoryId, 0),
                    new SkillTreeCategoryOrderCommand(secondCategoryId, 2)
                }));
        gap.EnsureSuccessStatusCode();
        var draftJson = await gap.Content.ReadAsStringAsync();
        var draft = JsonSerializer.Deserialize<SkillTreeDraftResponse>(draftJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        Assert.NotNull(draft);
        var orders = draft!.Categories.Select(c => c.SortOrder).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { 0, 1 }, orders);
    }

    private static async Task<AdminSkillTreeResponse> CreateTreeAsync(HttpClient admin, string name, string iconKey)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/skill-trees",
            new CreateSkillTreeCommand(name, "Test summary", iconKey));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AdminSkillTreeResponse>())!;
    }

    private static async Task<string> ReadCodeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("code", out var code))
            return code.GetString() ?? string.Empty;
        return string.Empty;
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

    private async Task<Guid> SeedCategoryAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = new SkillCategory
        {
            Id = Guid.CreateVersion7(),
            Name = "First",
            Summary = "First category",
            IconKey = "web"
        };
        db.SkillCategories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private async Task<Guid> SeedSecondCategoryAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = new SkillCategory
        {
            Id = Guid.CreateVersion7(),
            Name = "Second",
            Summary = "Second category",
            IconKey = "web"
        };
        db.SkillCategories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }
}
