using System.Net.Http.Json;
using System.Text.Json.Nodes;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Integration.Test.Tests.DashboardProjection;

[Collection(nameof(IntegrationTestCollection))]
public sealed class DashboardAdministrationTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Members_can_be_assigned_to_and_removed_from_a_cohort_in_batches()
    {
        var first = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "Cohort!Member1");
        var second = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "Cohort!Member2");
        using var admin = await CreateAdminClientAsync();
        var createResponse = await admin.PostAsJsonAsync("/api/admin/cohorts", new
        {
            name = $"Cohort-{Guid.NewGuid():N}"
        });
        createResponse.EnsureSuccessStatusCode();
        var cohortId = (await createResponse.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<Guid>();

        (await admin.PostAsJsonAsync($"/api/admin/cohorts/{cohortId}/members", new
        {
            userIds = new[] { first.Id, second.Id }
        })).EnsureSuccessStatusCode();

        var assigned = await admin.GetFromJsonAsync<JsonArray>($"/api/admin/cohorts/{cohortId}/members");
        Assert.Equal(2, assigned!.Count);

        (await admin.DeleteAsync($"/api/admin/cohorts/{cohortId}/members/{first.Id}"))
            .EnsureSuccessStatusCode();
        var remaining = await admin.GetFromJsonAsync<JsonArray>($"/api/admin/cohorts/{cohortId}/members");
        var onlyMember = Assert.Single(remaining!);
        Assert.Equal(second.Id, onlyMember!["id"]!.GetValue<Guid>());
    }

    [Fact]
    public async Task Active_tokens_can_be_listed_without_exposing_hashes_and_revoked()
    {
        using var admin = await CreateAdminClientAsync();
        var dashboardResponse = await admin.PostAsJsonAsync("/api/admin/dashboards", new
        {
            name = $"Dashboard-{Guid.NewGuid():N}",
            topCount = 10
        });
        dashboardResponse.EnsureSuccessStatusCode();
        var dashboardId = (await dashboardResponse.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<Guid>();

        var createTokenResponse = await admin.PostAsJsonAsync($"/api/admin/dashboards/{dashboardId}/tokens", new { });
        createTokenResponse.EnsureSuccessStatusCode();
        var tokenResult = (await createTokenResponse.Content.ReadFromJsonAsync<JsonObject>())!;
        var tokenId = tokenResult["tokenId"]!.GetValue<Guid>();

        var listResponse = await admin.GetAsync($"/api/admin/dashboards/{dashboardId}/tokens");
        listResponse.EnsureSuccessStatusCode();
        var responseText = await listResponse.Content.ReadAsStringAsync();
        var listed = JsonNode.Parse(responseText)!.AsArray();
        Assert.Single(listed);
        Assert.Equal(tokenId, listed[0]!["tokenId"]!.GetValue<Guid>());
        Assert.DoesNotContain("tokenHash", responseText, StringComparison.OrdinalIgnoreCase);

        (await admin.PostAsync($"/api/admin/dashboards/{dashboardId}/tokens/{tokenId}/revoke", null))
            .EnsureSuccessStatusCode();
        var afterRevoke = await admin.GetFromJsonAsync<JsonArray>($"/api/admin/dashboards/{dashboardId}/tokens");
        Assert.Empty(afterRevoke!);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        const string password = "Dashboard!Admin1";
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), password, role: Role.Admin);
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        login.EnsureSuccessStatusCode();
        return client;
    }
}
