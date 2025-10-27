using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Info;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for team management operations
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class TeamManagementTests(GZCTFApplicationFactory factory)
{
    /// <summary>
    /// Test team creation and retrieval
    /// </summary>
    [Fact]
    public async Task Team_Creation_And_Retrieval_ShouldWork()
    {
        var password = "Team@Create123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, password);

        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        // Test 1: Create a new team via API
        var createResponse = await client.PostAsJsonAsync("/api/Team",
            new TeamUpdateModel { Name = "Test Team Alpha", Bio = "This is a test team for integration testing" });
        createResponse.EnsureSuccessStatusCode();
        var createdTeam = await createResponse.Content.ReadFromJsonAsync<TeamInfoModel>();
        Assert.NotNull(createdTeam);
        Assert.Equal("Test Team Alpha", createdTeam.Name);
        Assert.Equal("This is a test team for integration testing", createdTeam.Bio);

        // Test 2: Retrieve the created team by ID
        var getResponse = await client.GetAsync($"/api/Team/{createdTeam.Id}");
        getResponse.EnsureSuccessStatusCode();
        var retrievedTeam = await getResponse.Content.ReadFromJsonAsync<TeamInfoModel>();
        Assert.NotNull(retrievedTeam);
        Assert.Equal(createdTeam.Id, retrievedTeam.Id);
        Assert.Equal(createdTeam.Name, retrievedTeam.Name);

        // Test 3: Get user's teams
        var teamsResponse = await client.GetAsync("/api/Team");
        teamsResponse.EnsureSuccessStatusCode();
        var teams = await teamsResponse.Content.ReadFromJsonAsync<TeamInfoModel[]>();
        Assert.NotNull(teams);
        Assert.Contains(teams, t => t.Id == createdTeam.Id);
    }

    /// <summary>
    /// Test team update functionality
    /// </summary>
    [Fact]
    public async Task Team_Update_ShouldModifyTeamInfo()
    {
        var password = "Team@Update123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Original Team Name");

        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });

        // Test 1: Update team information
        var updateResponse = await client.PutAsJsonAsync($"/api/Team/{team.Id}",
            new TeamUpdateModel { Name = "Updated Team Name", Bio = "Updated bio information" });
        updateResponse.EnsureSuccessStatusCode();
        var updatedTeam = await updateResponse.Content.ReadFromJsonAsync<TeamInfoModel>();
        Assert.NotNull(updatedTeam);
        Assert.Equal("Updated Team Name", updatedTeam.Name);
        Assert.Equal("Updated bio information", updatedTeam.Bio);

        // Test 2: Verify the update persisted
        var getResponse = await client.GetAsync($"/api/Team/{team.Id}");
        getResponse.EnsureSuccessStatusCode();
        var retrievedTeam = await getResponse.Content.ReadFromJsonAsync<TeamInfoModel>();
        Assert.NotNull(retrievedTeam);
        Assert.Equal("Updated Team Name", retrievedTeam.Name);
        Assert.Equal("Updated bio information", retrievedTeam.Bio);
    }

    /// <summary>
    /// Test team member limit enforcement
    /// </summary>
    [Fact]
    public async Task Team_Creation_ShouldEnforceLimit()
    {
        var password = "Team@Limit123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, password);

        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });

        // Create teams up to the limit (MaxTeamsAllowed = 3 per TeamController)
        var team1Response = await client.PostAsJsonAsync("/api/Team", new TeamUpdateModel { Name = "Team One" });
        team1Response.EnsureSuccessStatusCode();

        var team2Response = await client.PostAsJsonAsync("/api/Team", new TeamUpdateModel { Name = "Team Two" });
        team2Response.EnsureSuccessStatusCode();

        var team3Response = await client.PostAsJsonAsync("/api/Team", new TeamUpdateModel { Name = "Team Three" });
        team3Response.EnsureSuccessStatusCode();

        // Test: Attempt to create a 4th team should fail
        var team4Response = await client.PostAsJsonAsync("/api/Team", new TeamUpdateModel { Name = "Team Four" });
        Assert.Equal(HttpStatusCode.BadRequest, team4Response.StatusCode);
    }

    /// <summary>
    /// Test that unauthenticated users cannot access team endpoints
    /// </summary>
    [Fact]
    public async Task Team_Endpoints_ShouldRequireAuthentication()
    {
        using var client = factory.CreateClient();

        // Test 1: GET /api/Team should return 401
        var getTeamsResponse = await client.GetAsync("/api/Team");
        Assert.Equal(HttpStatusCode.Unauthorized, getTeamsResponse.StatusCode);

        // Test 2: POST /api/Team should return 401
        var createResponse =
            await client.PostAsJsonAsync("/api/Team", new TeamUpdateModel { Name = "Unauthorized Team" });
        Assert.Equal(HttpStatusCode.Unauthorized, createResponse.StatusCode);

        // Test 3: PUT /api/Team/{id} should return 401
        var updateResponse = await client.PutAsJsonAsync("/api/Team/1", new TeamUpdateModel { Name = "Updated Name" });
        Assert.Equal(HttpStatusCode.Unauthorized, updateResponse.StatusCode);
    }

    /// <summary>
    /// Test team information validation
    /// </summary>
    [Fact]
    public async Task Team_Creation_ShouldValidateInput()
    {
        var password = "Team@Validate123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, password);

        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });

        // Test 1: Empty team name should be rejected
        var emptyNameResponse = await client.PostAsJsonAsync("/api/Team", new TeamUpdateModel { Name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, emptyNameResponse.StatusCode);

        // Test 2: Null team name should be rejected
        var nullNameResponse = await client.PostAsJsonAsync("/api/Team", new TeamUpdateModel { Name = null! });
        Assert.Equal(HttpStatusCode.BadRequest, nullNameResponse.StatusCode);
    }
}
