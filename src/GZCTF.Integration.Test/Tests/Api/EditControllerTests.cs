using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for EditController:
/// - Scoreboard integration for GetGameChallenges and GetGameChallenge
/// - Challenge and division management operations
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class EditControllerTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    /// <summary>
    /// Test GetGameChallenges returns challenges with scores from scoreboard when available
    /// </summary>
    [Fact]
    public async Task GetGameChallenges_WithScoreboard_ShouldIncludeScoresFromScoreboard()
    {
        // Arrange: Create admin user
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        // Create game and challenges
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Scoreboard Test Game", token: TestContext.Current.CancellationToken);
        var challenge1 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Challenge 1", "flag{test1}", originalScore: 1000, token: TestContext.Current.CancellationToken);
        _ = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Challenge 2", "flag{test2}", originalScore: 500, token: TestContext.Current.CancellationToken);

        // Create user and team to generate submissions
        const string userPassword = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, userName, userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {userName}", token: TestContext.Current.CancellationToken);

        // Join game
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id, token: TestContext.Current.CancellationToken);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Act 1: Get challenges before any submissions (no scoreboard)
        var initialResponse = await adminClient.GetAsync($"/api/Edit/Games/{game.Id}/Challenges", TestContext.Current.CancellationToken);
        initialResponse.EnsureSuccessStatusCode();
        var initialChallenges = await initialResponse.Content.ReadFromJsonAsync<ChallengeInfoModel[]>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert: Should return challenges with original scores
        Assert.NotNull(initialChallenges);
        Assert.Equal(2, initialChallenges.Length);
        var initialChallenge1 = initialChallenges.First(c => c.Id == challenge1.Id);
        Assert.Equal(1000, initialChallenge1.Score); // Original score
        Assert.Equal(1000, initialChallenge1.OriginalScore);

        // Login as user and submit flag to generate scoreboard
        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        userLoginResponse.EnsureSuccessStatusCode();

        // Submit correct flag
        var submitResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge1.Id}", new FlagSubmitModel { Flag = "flag{test1}" }, cancellationToken: TestContext.Current.CancellationToken);
        submitResponse.EnsureSuccessStatusCode();

        // Wait a bit for scoreboard calculation
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Act 2: Get challenges after submission (with scoreboard)
        var updatedResponse = await adminClient.GetAsync($"/api/Edit/Games/{game.Id}/Challenges", TestContext.Current.CancellationToken);
        updatedResponse.EnsureSuccessStatusCode();
        var updatedChallenges = await updatedResponse.Content.ReadFromJsonAsync<ChallengeInfoModel[]>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert: Should return challenges with scores from scoreboard
        Assert.NotNull(updatedChallenges);
        Assert.Equal(2, updatedChallenges.Length);
        var updatedChallenge1 = updatedChallenges.First(c => c.Id == challenge1.Id);

        // The score should be updated from scoreboard (might be different due to dynamic scoring)
        Assert.Equal(1000, updatedChallenge1.OriginalScore);
        // Score might have changed based on submissions, but should not be 0
        Assert.True(updatedChallenge1.Score > 0);
    }

    /// <summary>
    /// Test GetGameChallenge returns challenge with AcceptedCount from scoreboard
    /// </summary>
    [Fact]
    public async Task GetGameChallenge_WithScoreboard_ShouldIncludeAcceptedCountFromScoreboard()
    {
        // Arrange: Create admin user
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        // Create game and challenge
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Challenge Detail Test", token: TestContext.Current.CancellationToken);
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Test Challenge", "flag{solution}", originalScore: 800, token: TestContext.Current.CancellationToken);

        // Create two teams
        var user1Password = "User1@Pass123";
        var user1Name = TestDataSeeder.RandomName();
        var user1 = await TestDataSeeder.CreateUserAsync(factory.Services, user1Name, user1Password, token: TestContext.Current.CancellationToken);
        var team1 = await TestDataSeeder.CreateTeamAsync(factory.Services, user1.Id, $"Team {user1Name}", token: TestContext.Current.CancellationToken);

        var user2Password = "User2@Pass123";
        var user2Name = TestDataSeeder.RandomName();
        var user2 = await TestDataSeeder.CreateUserAsync(factory.Services, user2Name, user2Password, token: TestContext.Current.CancellationToken);
        var team2 = await TestDataSeeder.CreateTeamAsync(factory.Services, user2.Id, $"Team {user2Name}", token: TestContext.Current.CancellationToken);

        // Join game
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team1.Id, user1.Id, token: TestContext.Current.CancellationToken);
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team2.Id, user2.Id, token: TestContext.Current.CancellationToken);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Act 1: Get challenge detail before submissions
        var initialResponse = await adminClient.GetAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}", TestContext.Current.CancellationToken);
        initialResponse.EnsureSuccessStatusCode();
        var initialDetail = await initialResponse.Content.ReadFromJsonAsync<ChallengeEditDetailModel>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert: AcceptedCount should be 0
        Assert.NotNull(initialDetail);
        Assert.Equal(0, initialDetail.AcceptedCount);
        Assert.Equal("Test Challenge", initialDetail.Title);

        // Submit flags from both users
        using var user1Client = factory.CreateClient();
        await user1Client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user1.UserName, Password = user1Password }, cancellationToken: TestContext.Current.CancellationToken);
        await user1Client.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}", new FlagSubmitModel { Flag = "flag{solution}" }, cancellationToken: TestContext.Current.CancellationToken);

        using var user2Client = factory.CreateClient();
        await user2Client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user2.UserName, Password = user2Password }, cancellationToken: TestContext.Current.CancellationToken);
        await user2Client.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}", new FlagSubmitModel { Flag = "flag{solution}" }, cancellationToken: TestContext.Current.CancellationToken);

        // Wait for scoreboard update
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Act 2: Get challenge detail after submissions
        var updatedResponse = await adminClient.GetAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}", TestContext.Current.CancellationToken);
        updatedResponse.EnsureSuccessStatusCode();
        var updatedDetail = await updatedResponse.Content.ReadFromJsonAsync<ChallengeEditDetailModel>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert: AcceptedCount should reflect the submissions from scoreboard
        Assert.NotNull(updatedDetail);
        Assert.True(updatedDetail.AcceptedCount >= 2,
            $"Expected AcceptedCount >= 2, but got {updatedDetail.AcceptedCount}");
    }

    /// <summary>
    /// Test GetGameChallenge without scoreboard still works correctly
    /// </summary>
    [Fact]
    public async Task GetGameChallenge_WithoutScoreboard_ShouldReturnChallengeWithZeroAcceptedCount()
    {
        // Arrange: Create admin user and game
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "No Scoreboard Test", token: TestContext.Current.CancellationToken);
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Static Challenge", "flag{test}", token: TestContext.Current.CancellationToken);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Act: Get challenge detail
        var response = await adminClient.GetAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<ChallengeEditDetailModel>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(detail);
        Assert.Equal(challenge.Id, detail.Id);
        Assert.Equal(0, detail.AcceptedCount);
    }

    /// <summary>
    /// Test DeleteDivision works correctly
    /// </summary>
    [Fact]
    public async Task DeleteDivision_ShouldDeleteSuccessfully()
    {
        // Arrange: Create admin user and game with division
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Division Delete Test", token: TestContext.Current.CancellationToken);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Create division
        var createResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Test Division",
                InviteCode = "TEST123",
                DefaultPermissions = GamePermission.All
            }, cancellationToken: TestContext.Current.CancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var division = await createResponse.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division);

        // Act: Delete division
        var deleteResponse = await adminClient.DeleteAsync($"/api/Edit/Games/{game.Id}/Divisions/{division.Id}", TestContext.Current.CancellationToken);

        // Assert
        deleteResponse.EnsureSuccessStatusCode();

        // Verify division is deleted
        var getDivisionsResponse = await adminClient.GetAsync($"/api/Edit/Games/{game.Id}/Divisions", TestContext.Current.CancellationToken);
        getDivisionsResponse.EnsureSuccessStatusCode();
        var divisions = await getDivisionsResponse.Content.ReadFromJsonAsync<Division[]>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(divisions);
        Assert.DoesNotContain(divisions, d => d.Id == division.Id);
    }

    /// <summary>
    /// Test DeleteDivision with non-existent division returns 404
    /// </summary>
    [Fact]
    public async Task DeleteDivision_WithNonExistentDivision_ShouldReturn404()
    {
        // Arrange: Create admin user and game
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Invalid Division Test", token: TestContext.Current.CancellationToken);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Act: Try to delete non-existent division
        var deleteResponse = await adminClient.DeleteAsync($"/api/Edit/Games/{game.Id}/Divisions/99999", TestContext.Current.CancellationToken);

        // Assert: Should return 404
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    /// <summary>
    /// Test RemoveGameChallenge works correctly
    /// </summary>
    [Fact]
    public async Task RemoveGameChallenge_ShouldDeleteSuccessfully()
    {
        // Arrange: Create admin user, game, and challenge
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Challenge Delete Test", token: TestContext.Current.CancellationToken);
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Deletable Challenge", "flag{delete_me}", token: TestContext.Current.CancellationToken);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Act: Delete challenge
        var deleteResponse = await adminClient.DeleteAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}", TestContext.Current.CancellationToken);

        // Assert
        deleteResponse.EnsureSuccessStatusCode();

        // Verify challenge is deleted
        var getChallengesResponse = await adminClient.GetAsync($"/api/Edit/Games/{game.Id}/Challenges", TestContext.Current.CancellationToken);
        getChallengesResponse.EnsureSuccessStatusCode();
        var challenges = await getChallengesResponse.Content.ReadFromJsonAsync<ChallengeInfoModel[]>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(challenges);
        Assert.DoesNotContain(challenges, c => c.Id == challenge.Id);
    }

    /// <summary>
    /// Test RemoveGameChallenge with non-existent challenge returns 404
    /// </summary>
    [Fact]
    public async Task RemoveGameChallenge_WithNonExistentChallenge_ShouldReturn404()
    {
        // Arrange: Create admin user and game
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Invalid Challenge Test", token: TestContext.Current.CancellationToken);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Act: Try to delete non-existent challenge
        var deleteResponse = await adminClient.DeleteAsync($"/api/Edit/Games/{game.Id}/Challenges/99999", TestContext.Current.CancellationToken);

        // Assert: Should return 404
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    /// <summary>
    /// Test UpdateAttachment endpoint routing works correctly
    /// </summary>
    [Fact]
    public async Task UpdateAttachment_WithNonExistentChallenge_ShouldReturn404ForChallenge()
    {
        // Arrange: Create admin user and game
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Attachment Test", token: TestContext.Current.CancellationToken);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Act: Try to update attachment for non-existent challenge
        var updateResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Challenges/99999/Attachment", new AttachmentCreateModel { AttachmentType = FileType.Local, FileHash = "test-hash" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert: Should return 404 for challenge (not for game)
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
    }

    /// <summary>
    /// Test AddFlags works correctly
    /// </summary>
    [Fact]
    public async Task AddFlags_ShouldAddSuccessfully()
    {
        // Arrange: Create admin user, game, and challenge
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Add Flags Test", token: TestContext.Current.CancellationToken);
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Multi-Flag Challenge", "flag{initial}", token: TestContext.Current.CancellationToken);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Act: Add additional flags
        var addFlagsResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}/Flags", new[]
            {
                new FlagCreateModel { Flag = "flag{second}", AttachmentType = FileType.None },
                new FlagCreateModel { Flag = "flag{third}", AttachmentType = FileType.None }
            }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        addFlagsResponse.EnsureSuccessStatusCode();

        // Verify flags are added
        var getChallengeResponse = await adminClient.GetAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}", TestContext.Current.CancellationToken);
        getChallengeResponse.EnsureSuccessStatusCode();
        var challengeDetail = await getChallengeResponse.Content.ReadFromJsonAsync<ChallengeEditDetailModel>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(challengeDetail);
        Assert.True(challengeDetail.Flags.Count >= 3, $"Expected at least 3 flags, got {challengeDetail.Flags.Count}");
    }

    /// <summary>
    /// Test RemoveFlag works correctly
    /// </summary>
    [Fact]
    public async Task RemoveFlag_ShouldRemoveSuccessfully()
    {
        // Arrange: Create admin user, game, and challenge with multiple flags
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Remove Flag Test", token: TestContext.Current.CancellationToken);
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Challenge with Flags", "flag{keep}", token: TestContext.Current.CancellationToken);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Add another flag
        await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}/Flags", new[] { new FlagCreateModel { Flag = "flag{remove}", AttachmentType = FileType.None } }, cancellationToken: TestContext.Current.CancellationToken);

        // Get challenge to find flag IDs
        var getChallengeResponse = await adminClient.GetAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}", TestContext.Current.CancellationToken);
        getChallengeResponse.EnsureSuccessStatusCode();
        var challengeDetail = await getChallengeResponse.Content.ReadFromJsonAsync<ChallengeEditDetailModel>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(challengeDetail);
        var flagToRemove = challengeDetail.Flags.First(f => f.Flag == "flag{remove}");

        // Act: Remove flag
        var removeResponse = await adminClient.DeleteAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}/Flags/{flagToRemove.Id}", TestContext.Current.CancellationToken);

        // Assert
        removeResponse.EnsureSuccessStatusCode();

        // Verify flag is removed
        var updatedChallengeResponse =
            await adminClient.GetAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}", TestContext.Current.CancellationToken);
        updatedChallengeResponse.EnsureSuccessStatusCode();
        var updatedDetail = await updatedChallengeResponse.Content.ReadFromJsonAsync<ChallengeEditDetailModel>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedDetail);
        Assert.DoesNotContain(updatedDetail.Flags, f => f.Id == flagToRemove.Id);
    }

    /// <summary>
    /// Test CreateTestContainer and retrieve flag via TCP connection
    /// </summary>
    [Fact]
    public async Task TestContainerOperations_ShouldRetrieveFlagSuccessfully()
    {
        // Arrange: Create admin user, game, and dynamic container challenge
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Container Test", token: TestContext.Current.CancellationToken);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gameEntity = await context.Games.FirstOrDefaultAsync(g => g.Id == game.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(gameEntity);

        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var challenge = await challengeRepo.CreateChallenge(gameEntity,
            new GameChallenge
            {
                Title = "Container Challenge",
                Type = ChallengeType.DynamicContainer,
                GameId = game.Id,
                IsEnabled = true,
                ContainerImage = "ghcr.io/gzctf/challenge-base/echo:latest",
                ExposePort = 70,
                MemoryLimit = 64,
                CPUCount = 1,
                StorageLimit = 256
            }, CancellationToken.None);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Act: Create test container
        var createContainerResponse = await adminClient.PostAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}/Container", null, TestContext.Current.CancellationToken);

        Assert.True(createContainerResponse.IsSuccessStatusCode);

        // Try to wait for admin test container if available
        await ContainerHelper.WaitAdminContainerAsync(factory.Services, challenge.Id, output);

        var envVars = await ContainerHelper.GetAdminContainerEnvAsync(factory.Services, challenge.Id);
        Assert.Equal("admin", envVars["GZCTF_TEAM_ID"]);
        Assert.Equal(adminUser.Id.ToString(), envVars["GZCTF_USER_ID"]);
        Assert.Equal(challenge.Id.ToString(), envVars["GZCTF_CHALLENGE_ID"]);
        Assert.Equal(game.Id.ToString(), envVars["GZCTF_GAME_ID"]);

        try
        {
            var responseText = await createContainerResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var responseJson = System.Text.Json.JsonDocument.Parse(responseText);
            var root = responseJson.RootElement;
            Assert.True(root.TryGetProperty("entry", out var entryElement));
            var entry = entryElement.GetString();
            Assert.NotNull(entry);
            Assert.NotEmpty(entry);

            output.WriteLine($"✅ Container entry: {entry}");

            var flag = await ContainerHelper.FetchFlag(entry, factory.Server, isNoInst: true);

            // Assert: Should have retrieved a flag
            Assert.NotNull(flag);
            Assert.NotEmpty(flag);
            Assert.Equal("flag{GZCTF_dynamic_flag_test}", flag);
            Assert.Equal(flag, envVars["GZCTF_FLAG"]);

            // Output the retrieved flag for verification
            output.WriteLine($"✅ Successfully retrieved flag from container: {flag}");

            // Clean up: Destroy the container
            var destroyContainerResponse = await adminClient.DeleteAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}/Container", TestContext.Current.CancellationToken);

            // Should not return 404 for game
            Assert.NotEqual(HttpStatusCode.NotFound, destroyContainerResponse.StatusCode);
        }
        finally
        {
            // Ensure cleanup even if test fails
            try
            {
                await adminClient.DeleteAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge.Id}/Container", TestContext.Current.CancellationToken);
                // Ignore cleanup errors
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Test that optimization didn't break error handling for non-existent resources
    /// </summary>
    [Fact]
    public async Task EditOperations_WithNonExistentResources_ShouldReturn404()
    {
        // Arrange: Create admin user
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        var nonExistentGameId = 99999;
        var nonExistentChallengeId = 99998;
        var nonExistentDivisionId = 99997;

        // Act & Assert: Various operations with non-existent resources

        // GetGameChallenge with non-existent challenge
        var getChallengeResponse = await adminClient.GetAsync($"/api/Edit/Games/{nonExistentGameId}/Challenges/{nonExistentChallengeId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getChallengeResponse.StatusCode);

        // DeleteDivision with non-existent division
        var deleteDivisionResponse = await adminClient.DeleteAsync($"/api/Edit/Games/{nonExistentGameId}/Divisions/{nonExistentDivisionId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, deleteDivisionResponse.StatusCode);

        // RemoveGameChallenge with non-existent challenge
        var deleteChallengeResponse = await adminClient.DeleteAsync($"/api/Edit/Games/{nonExistentGameId}/Challenges/{nonExistentChallengeId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, deleteChallengeResponse.StatusCode);

        // UpdateAttachment with non-existent challenge
        var updateAttachmentResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{nonExistentGameId}/Challenges/{nonExistentChallengeId}/Attachment", new AttachmentCreateModel { AttachmentType = FileType.Local, FileHash = "test-hash" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, updateAttachmentResponse.StatusCode);
    }

    /// <summary>
    /// Test DeleteGame endpoint - successful deletion
    /// </summary>
    [Fact]
    public async Task DeleteGame_WhenGameExists_ShouldDeleteSuccessfully()
    {
        // Setup: Create admin user
        var adminPassword = "Admin@Delete123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        // Create a game to delete with challenges and teams
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Game To Delete", token: TestContext.Current.CancellationToken);
        var gameId = game.Id;

        // Add challenges
        var challenge1 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, gameId, "Challenge 1", "flag{delete1}", token: TestContext.Current.CancellationToken);
        var challenge2 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, gameId, "Challenge 2", "flag{delete2}", token: TestContext.Current.CancellationToken);

        // Add teams to verify cascade
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Pass@123", token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Delete Test Team", token: TestContext.Current.CancellationToken);

        // Join game
        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = "Pass@123" }, cancellationToken: TestContext.Current.CancellationToken);
        await userClient.PostAsJsonAsync($"/api/Game/{gameId}", new GameJoinModel { TeamId = team.Id }, cancellationToken: TestContext.Current.CancellationToken);

        // Login as admin and delete the game
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = adminUser.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        adminLoginResponse.EnsureSuccessStatusCode();

        // Attempt to delete on non-existent game should return 404
        var notFoundResponse = await adminClient.DeleteAsync("/api/Edit/Games/99999", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);

        // Attempt to delete as non-admin should return 403
        using var normalUserClient = factory.CreateClient();
        await normalUserClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = "Pass@123" }, cancellationToken: TestContext.Current.CancellationToken);
        var forbiddenResponse = await normalUserClient.DeleteAsync($"/api/Edit/Games/{gameId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        // Actual deletion by admin should succeed
        var deleteResponse = await adminClient.DeleteAsync($"/api/Edit/Games/{gameId}", TestContext.Current.CancellationToken);
        deleteResponse.EnsureSuccessStatusCode();

        // Verify game and all related data are deleted
        using var postDeleteScope = factory.Services.CreateScope();
        var postDeleteDb = postDeleteScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gameAfterDelete = await postDeleteDb.Games.FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(gameAfterDelete);

        // Verify challenges are cascade deleted
        var challengesAfterDelete = await postDeleteDb.GameChallenges
            .Where(c => c.Id == challenge1.Id || c.Id == challenge2.Id)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(challengesAfterDelete);

        // Verify participations are deleted
        var participationsAfterDelete = await postDeleteDb.Participations
            .Where(p => p.GameId == gameId)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(participationsAfterDelete);
    }
}
