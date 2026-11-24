using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for Game Join functionality covering division management,
/// permission validation, and participation state handling
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class GameJoinTests(GZCTFApplicationFactory factory)
{
    /// <summary>
    /// Test 1: User can join game without division (no invite code required)
    /// </summary>
    [Fact]
    public async Task JoinGame_WithoutDivision_ShouldSucceed()
    {
        // Arrange
        var gameId = await TestDataSeeder.GetOrCreateBasicGameAsync(factory.Services);
        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Act
        var joinResponse = await client.PostAsJsonAsync($"/api/Game/{gameId}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        // Verify participation was created
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, gameId, CancellationToken.None);
        Assert.NotNull(participation);
        Assert.Null(participation.DivisionId);
        Assert.Equal(ParticipationStatus.Accepted, participation.Status);
    }

    /// <summary>
    /// Test 2: User cannot join game with invalid division ID
    /// Note: When joinable divisions exist, trying to join an invalid division should fail
    /// </summary>
    [Fact]
    public async Task JoinGame_WithInvalidDivision_ShouldFail()
    {
        // Arrange - Create game with a valid joinable division
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Invalid Division Test Game", token: TestContext.Current.CancellationToken);

        // Create a valid joinable division so that joinable divisions exist
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = admin.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        adminLoginResponse.EnsureSuccessStatusCode();

        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Valid Division",
                InviteCode = "VALID_DIV",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        divisionResponse.EnsureSuccessStatusCode();

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Act - Try to join with non-existent division (should fail since joinable divisions exist)
        var joinResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = 99999, InviteCode = "WRONG" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, joinResponse.StatusCode);
        var error = await joinResponse.Content.ReadFromJsonAsync<RequestResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(error);
        Assert.Contains("division", error.Title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 3: User can join game with valid division and correct invite code
    /// </summary>
    [Fact]
    public async Task JoinGame_WithValidDivisionAndInviteCode_ShouldSucceed()
    {
        // Arrange - Create admin user
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        // Create regular user and team
        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Division Game", token: TestContext.Current.CancellationToken);

        // Create division as admin
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = admin.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        adminLoginResponse.EnsureSuccessStatusCode();

        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Test Division",
                InviteCode = "TESTDIV2025",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division);

        // Act - User joins with correct invite code
        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        userLoginResponse.EnsureSuccessStatusCode();

        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "TESTDIV2025" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        // Verify participation
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(participation);
        Assert.Equal(division.Id, participation.DivisionId);
        Assert.Equal(ParticipationStatus.Accepted, participation.Status);
    }

    /// <summary>
    /// Test 4: User cannot join division with incorrect invite code
    /// </summary>
    [Fact]
    public async Task JoinGame_WithWrongInviteCode_ShouldFail()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Invite Code Game", token: TestContext.Current.CancellationToken);

        // Create division with invite code
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = admin.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        adminLoginResponse.EnsureSuccessStatusCode();

        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Secure Division",
                InviteCode = "CORRECT_CODE",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division);

        // Act - Try to join with wrong code
        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        userLoginResponse.EnsureSuccessStatusCode();

        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "WRONG_CODE" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, joinResponse.StatusCode);
        var error = await joinResponse.Content.ReadFromJsonAsync<RequestResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(error);
        Assert.Contains("invitation", error.Title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 5: User cannot join division without JoinGame permission
    /// Note: When both joinable and non-joinable divisions exist, user must choose a joinable division
    /// </summary>
    [Fact]
    public async Task JoinGame_DivisionWithoutJoinPermission_ShouldFail()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Restricted Division Game", token: TestContext.Current.CancellationToken);

        // Create two divisions: one joinable and one without JoinGame permission
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = admin.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        adminLoginResponse.EnsureSuccessStatusCode();

        // Create joinable division
        var joinableDivisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Joinable Division",
                InviteCode = "JOINABLE",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        joinableDivisionResponse.EnsureSuccessStatusCode();

        // Create division without JoinGame permission
        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "No Join Division",
                InviteCode = "NOJOIN",
                DefaultPermissions = GamePermission.All & ~GamePermission.JoinGame & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division);

        // Act - Try to join the non-joinable division
        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        userLoginResponse.EnsureSuccessStatusCode();

        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "NOJOIN" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, joinResponse.StatusCode);
        var error = await joinResponse.Content.ReadFromJsonAsync<RequestResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(error);
        Assert.Contains("division", error.Title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 6: Rejected team can rejoin with different division
    /// </summary>
    [Fact]
    public async Task JoinGame_RejectedTeamRejoinsWithNewDivision_ShouldSucceed()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Rejoin Test Game", token: TestContext.Current.CancellationToken);

        // Create two divisions
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = admin.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        adminLoginResponse.EnsureSuccessStatusCode();

        var division1Response = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Division 1",
                InviteCode = "DIV1",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        division1Response.EnsureSuccessStatusCode();
        var division1 = await division1Response.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division1);

        var division2Response = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Division 2",
                InviteCode = "DIV2",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        division2Response.EnsureSuccessStatusCode();
        var division2 = await division2Response.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division2);

        // User joins Division 1
        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        userLoginResponse.EnsureSuccessStatusCode();

        var join1Response = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division1.Id, InviteCode = "DIV1" }, cancellationToken: TestContext.Current.CancellationToken);
        join1Response.EnsureSuccessStatusCode();

        // Admin rejects the participation
        using var scope1 = factory.Services.CreateScope();
        var participationRepo = scope1.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var gameRepo = scope1.ServiceProvider.GetRequiredService<IGameRepository>();
        var teamRepo = scope1.ServiceProvider.GetRequiredService<ITeamRepository>();

        var gameEntity = await gameRepo.GetGameById(game.Id, TestContext.Current.CancellationToken);
        var teamEntity = await teamRepo.GetTeamById(team.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(gameEntity);
        Assert.NotNull(teamEntity);

        var participation = await participationRepo.GetParticipation(teamEntity, gameEntity, TestContext.Current.CancellationToken);
        Assert.NotNull(participation);

        await participationRepo.UpdateParticipationStatus(participation, ParticipationStatus.Rejected, TestContext.Current.CancellationToken);

        // Act - User rejoins with Division 2
        var join2Response = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division2.Id, InviteCode = "DIV2" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, join2Response.StatusCode);

        // Verify division changed and status is pending
        using var scope2 = factory.Services.CreateScope();
        var participationRepo2 = scope2.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var updatedParticipation = await participationRepo2.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updatedParticipation);
        Assert.Equal(division2.Id, updatedParticipation.DivisionId);
        Assert.Equal(ParticipationStatus.Accepted, updatedParticipation.Status);
    }

    /// <summary>
    /// Test 7: Cannot change division after being accepted
    /// </summary>
    [Fact]
    public async Task JoinGame_CannotChangeDivisionAfterAccepted_ShouldFail()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "No Change Division Game", token: TestContext.Current.CancellationToken);

        // Create two divisions
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = admin.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        adminLoginResponse.EnsureSuccessStatusCode();

        var division1Response = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Division Alpha",
                InviteCode = "ALPHA",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        division1Response.EnsureSuccessStatusCode();
        var division1 = await division1Response.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division1);

        var division2Response = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Division Beta",
                InviteCode = "BETA",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        division2Response.EnsureSuccessStatusCode();
        var division2 = await division2Response.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division2);

        // User joins Division 1 and gets accepted
        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        userLoginResponse.EnsureSuccessStatusCode();

        var join1Response = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division1.Id, InviteCode = "ALPHA" }, cancellationToken: TestContext.Current.CancellationToken);
        join1Response.EnsureSuccessStatusCode();

        // Verify accepted status
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(participation);
        Assert.Equal(ParticipationStatus.Accepted, participation.Status);

        // Act - Try to join with different division
        var join2Response = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division2.Id, InviteCode = "BETA" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, join2Response.StatusCode);
        var error = await join2Response.Content.ReadFromJsonAsync<RequestResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(error);
        Assert.Contains("joined", error.Title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 8: Idempotent join - user already in team returns success
    /// </summary>
    [Fact]
    public async Task JoinGame_AlreadyJoined_ShouldBeIdempotent()
    {
        // Arrange
        var gameId = await TestDataSeeder.GetOrCreateBasicGameAsync(factory.Services);
        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // First join
        var join1Response = await client.PostAsJsonAsync($"/api/Game/{gameId}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, join1Response.StatusCode);

        // Act - Join again with same parameters
        var join2Response = await client.PostAsJsonAsync($"/api/Game/{gameId}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Should return bad request indicating already joined
        Assert.Equal(HttpStatusCode.BadRequest, join2Response.StatusCode);

        // Verify only one membership exists
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var memberCount = await context.UserParticipations
            .CountAsync(up => up.GameId == gameId && up.TeamId == team.Id && up.UserId == user.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, memberCount);
    }

    /// <summary>
    /// Test 9: Cannot join from different team after already joined
    /// Note: Uses independent game because it tests cross-team join prevention which requires clean user state
    /// </summary>
    [Fact]
    public async Task JoinGame_FromDifferentTeam_ShouldFail()
    {
        // Arrange - Create independent game for this test
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Team Switch Test Game", token: TestContext.Current.CancellationToken);
        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team1 = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team 1 {user.UserName}", token: TestContext.Current.CancellationToken);
        var team2 = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team 2 {user.UserName}", token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Join with team1
        var join1Response = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team1.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);
        join1Response.EnsureSuccessStatusCode();

        // Act - Try to join with team2
        var join2Response = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team2.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, join2Response.StatusCode);
        var error = await join2Response.Content.ReadFromJsonAsync<RequestResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(error);
        Assert.Contains("other team", error.Title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 10: Multiple users can join same team
    /// </summary>
    [Fact]
    public async Task JoinGame_MultipleUsersToSameTeam_ShouldSucceed()
    {
        // Arrange
        var gameId = await TestDataSeeder.GetOrCreateBasicGameAsync(factory.Services);
        var user1Password = "User1@Pass123";
        var user1 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), user1Password, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user1.Id, $"Team {user1.UserName}", token: TestContext.Current.CancellationToken);

        var user2Password = "User2@Pass123";
        var user2 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), user2Password, token: TestContext.Current.CancellationToken);

        // Add user2 to team
        using var scope1 = factory.Services.CreateScope();
        var context1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var teamEntity = await context1.Teams.Include(t => t.Members)
            .FirstAsync(t => t.Id == team.Id, cancellationToken: TestContext.Current.CancellationToken);
        var user2Entity = await context1.Users.FirstAsync(u => u.Id == user2.Id, cancellationToken: TestContext.Current.CancellationToken);
        teamEntity.Members.Add(user2Entity);
        await context1.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Both users join
        using var client1 = factory.CreateClient();
        var login1Response = await client1.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user1.UserName, Password = user1Password }, cancellationToken: TestContext.Current.CancellationToken);
        login1Response.EnsureSuccessStatusCode();

        var join1Response = await client1.PostAsJsonAsync($"/api/Game/{gameId}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, join1Response.StatusCode);

        using var client2 = factory.CreateClient();
        var login2Response = await client2.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user2.UserName, Password = user2Password }, cancellationToken: TestContext.Current.CancellationToken);
        login2Response.EnsureSuccessStatusCode();

        var join2Response = await client2.PostAsJsonAsync($"/api/Game/{gameId}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, join2Response.StatusCode);

        // Verify both users are in the participation
        using var scope2 = factory.Services.CreateScope();
        var context2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var userParticipations = await context2.UserParticipations
            .Where(up => up.GameId == gameId && up.TeamId == team.Id)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, userParticipations.Count);
        Assert.Contains(userParticipations, up => up.UserId == user1.Id);
        Assert.Contains(userParticipations, up => up.UserId == user2.Id);
    }

    /// <summary>
    /// Test 11: Team member limit is enforced
    /// </summary>
    [Fact]
    public async Task JoinGame_ExceedTeamMemberLimit_ShouldFail()
    {
        // Arrange - Create game with team member limit of 2
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Limited Team Game", token: TestContext.Current.CancellationToken);

        using var scope0 = factory.Services.CreateScope();
        var gameRepo = scope0.ServiceProvider.GetRequiredService<IGameRepository>();
        var gameEntity = await gameRepo.GetGameById(game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(gameEntity);
        gameEntity.TeamMemberCountLimit = 2;
        await gameRepo.SaveAsync(TestContext.Current.CancellationToken);

        var user1Password = "User1@Pass123";
        var user1 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), user1Password, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user1.Id, $"Team {user1.UserName}", token: TestContext.Current.CancellationToken);

        var user2Password = "User2@Pass123";
        var user2 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), user2Password, token: TestContext.Current.CancellationToken);

        var user3Password = "User3@Pass123";
        var user3 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), user3Password, token: TestContext.Current.CancellationToken);

        // Add users to team
        using var scope1 = factory.Services.CreateScope();
        var context1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var teamEntity = await context1.Teams.Include(t => t.Members)
            .FirstAsync(t => t.Id == team.Id, cancellationToken: TestContext.Current.CancellationToken);
        teamEntity.Members.Add(await context1.Users.FirstAsync(u => u.Id == user2.Id, cancellationToken: TestContext.Current.CancellationToken));
        teamEntity.Members.Add(await context1.Users.FirstAsync(u => u.Id == user3.Id, cancellationToken: TestContext.Current.CancellationToken));
        await context1.SaveChangesAsync(TestContext.Current.CancellationToken);

        // User1 and User2 join successfully
        using var client1 = factory.CreateClient();
        await client1.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user1.UserName, Password = user1Password }, cancellationToken: TestContext.Current.CancellationToken);
        var join1 = await client1.PostAsJsonAsync($"/api/Game/{gameEntity.Id}", new GameJoinModel { TeamId = team.Id }, cancellationToken: TestContext.Current.CancellationToken);
        join1.EnsureSuccessStatusCode();

        using var client2 = factory.CreateClient();
        await client2.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user2.UserName, Password = user2Password }, cancellationToken: TestContext.Current.CancellationToken);
        var join2 = await client2.PostAsJsonAsync($"/api/Game/{gameEntity.Id}", new GameJoinModel { TeamId = team.Id }, cancellationToken: TestContext.Current.CancellationToken);
        join2.EnsureSuccessStatusCode();

        // Act - User3 tries to join (exceeds limit)
        using var client3 = factory.CreateClient();
        await client3.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user3.UserName, Password = user3Password }, cancellationToken: TestContext.Current.CancellationToken);
        var join3Response = await client3.PostAsJsonAsync($"/api/Game/{gameEntity.Id}", new GameJoinModel { TeamId = team.Id }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, join3Response.StatusCode);
        var error = await join3Response.Content.ReadFromJsonAsync<RequestResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(error);
        Assert.Contains("limit", error.Title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 12: Game invite code is checked when no division specified
    /// </summary>
    [Fact]
    public async Task JoinGame_GameInviteCodeRequired_ShouldValidate()
    {
        // Arrange
        var gameId = await TestDataSeeder.GetOrCreateInviteGameAsync(factory.Services);
        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);

        // Act - Try to join without invite code
        var joinWrongResponse = await client.PostAsJsonAsync($"/api/Game/{gameId}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = "WRONG" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Wrong code should fail
        Assert.Equal(HttpStatusCode.BadRequest, joinWrongResponse.StatusCode);

        // Act - Join with correct code
        var joinCorrectResponse = await client.PostAsJsonAsync($"/api/Game/{gameId}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = "SHARED_INVITE_2025" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Correct code should succeed
        Assert.Equal(HttpStatusCode.OK, joinCorrectResponse.StatusCode);
    }

    /// <summary>
    /// Test 13: Division invite code takes precedence over game invite code
    /// Note: Uses independent game because it modifies division configuration
    /// </summary>
    [Fact]
    public async Task JoinGame_DivisionInviteCodeTakesPrecedence_ShouldValidate()
    {
        // Arrange - Create independent game with game-level invite code
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Division Priority Test Game", token: TestContext.Current.CancellationToken);

        // Set game invite code via API
        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = admin.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);

        var gameUpdateResponse = await adminClient.PutAsJsonAsync($"/api/Edit/Games/{game.Id}", new GameInfoModel
            {
                Title = game.Title,
                Summary = "Test game summary",
                Content = "Test game content",
                InviteCode = "GAME_CODE",
                AcceptWithoutReview = true,
                TeamMemberCountLimit = 0,
                ContainerCountLimit = 3,
                Hidden = false,
                PracticeMode = false,
                WriteupRequired = false,
                StartTimeUtc = game.Start,
                EndTimeUtc = game.End,
                WriteupDeadline = game.End
            }, cancellationToken: TestContext.Current.CancellationToken);
        gameUpdateResponse.EnsureSuccessStatusCode();

        // Create division with its own invite code
        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "PriorityDiv", // Shorter name to fit within max length
                InviteCode = "DIVCODE",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);

        // Act - Try to join division with game code (should fail)
        var joinGameCodeResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "GAME_CODE" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Should fail because division code is required
        Assert.Equal(HttpStatusCode.BadRequest, joinGameCodeResponse.StatusCode);

        // Act - Join with division code (should succeed)
        var joinDivCodeResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "DIVCODE" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, joinDivCodeResponse.StatusCode);
    }

    /// <summary>
    /// Test: Division permissions take precedence over game settings for auto-acceptance
    /// When division requires review, participation should be pending even if game allows auto-accept
    /// </summary>
    [Fact]
    public async Task JoinGame_DivisionRequiresReview_ShouldNotAutoAccept()
    {
        // Arrange - Create game that allows auto-accept
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Division Review Test Game", acceptWithoutReview: true, token: TestContext.Current.CancellationToken); // Game allows auto-accept

        // Create division that requires review
        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = admin.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);

        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "ReviewRequiredDiv",
                InviteCode = "REVIEW_DIV",
                DefaultPermissions = GamePermission.All // Includes RequireReview by default
            }, cancellationToken: TestContext.Current.CancellationToken);
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);

        // Act - Join division that requires review
        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "REVIEW_DIV" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        // Verify participation is pending (not auto-accepted)
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(participation);
        Assert.Equal(division.Id, participation.DivisionId);
        Assert.Equal(ParticipationStatus.Pending,
            participation.Status); // Should be pending due to division requiring review
    }

    /// <summary>
    /// Test: Join game with divisions but none has JoinGame flag
    /// When no division has JoinGame permission, invitation code check should fallback to game level,
    /// and user should be able to join without specifying a division.
    /// </summary>
    [Fact]
    public async Task JoinGame_NoDivisionsHaveJoinGameFlag_ShouldFallbackToGameLevel()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "No Joinable Divisions Game", token: TestContext.Current.CancellationToken);

        // Create divisions without JoinGame permission
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = admin.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        adminLoginResponse.EnsureSuccessStatusCode();

        var division1Response = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Closed Division 1",
                InviteCode = "CLOSED1",
                DefaultPermissions = GamePermission.All & ~GamePermission.JoinGame & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        division1Response.EnsureSuccessStatusCode();

        var division2Response = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Closed Division 2",
                InviteCode = "CLOSED2",
                DefaultPermissions = GamePermission.All & ~GamePermission.JoinGame & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        division2Response.EnsureSuccessStatusCode();

        // Set game level invite code
        using var scope0 = factory.Services.CreateScope();
        var gameRepo0 = scope0.ServiceProvider.GetRequiredService<IGameRepository>();
        var gameEntity = await gameRepo0.GetGameById(game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(gameEntity);
        gameEntity.InviteCode = "GAME_LEVEL_CODE";
        await gameRepo0.SaveAsync(TestContext.Current.CancellationToken);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        // Setup user client for testing
        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        userLoginResponse.EnsureSuccessStatusCode();

        // Test 1 - Try to join a non-joinable division with code (should fail)
        var division1 = await division1Response.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division1);
        var joinClosedDivisionResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division1.Id, InviteCode = "CLOSED1" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, joinClosedDivisionResponse.StatusCode);

        // Test 2 - Try to join a non-existent division (should fail)
        var joinNonExistentDivisionResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = 99999, InviteCode = "ANYTHING" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, joinNonExistentDivisionResponse.StatusCode);

        // Act - Join without division, using game level invite code
        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = "GAME_LEVEL_CODE" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        // Verify participation was created without division
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(participation);
        Assert.Null(participation.DivisionId);
        Assert.Equal(ParticipationStatus.Accepted, participation.Status);

        // Check GetGameJoinCheckInfo API - should show no joined teams (since joined without division) and no joinable divisions
        var checkInfoResponse = await userClient.GetAsync($"/api/Game/{game.Id}/Check", TestContext.Current.CancellationToken);
        checkInfoResponse.EnsureSuccessStatusCode();
        var checkInfo = await checkInfoResponse.Content.ReadFromJsonAsync<GameJoinCheckInfoModel>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(checkInfo);
        // Should show no joined teams (because JoinedTeams only shows teams with specific divisions, this user joined without division)
        Assert.Empty(checkInfo.JoinedTeams);
        // Should show no joinable divisions (since none have JoinGame permission)
        Assert.Empty(checkInfo.JoinableDivisions);
    }

    /// <summary>
    /// Test: User joins game with specific division that has no invitation code
    /// When game has joinable divisions, division-level invite code takes precedence.
    /// If division has no code, no code is required to join that division (even if game has code).
    /// </summary>
    [Fact]
    public async Task JoinGame_DivisionWithoutInviteCode_NoCodeRequired()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Division Without Code Game", token: TestContext.Current.CancellationToken);

        // Set game level invite code
        using var scope0 = factory.Services.CreateScope();
        var gameRepo0 = scope0.ServiceProvider.GetRequiredService<IGameRepository>();
        var gameEntity0 = await gameRepo0.GetGameById(game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(gameEntity0);
        gameEntity0.InviteCode = "GAME_LEVEL_CODE";
        await gameRepo0.SaveAsync(TestContext.Current.CancellationToken);

        // Create a division WITHOUT invite code
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = admin.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        adminLoginResponse.EnsureSuccessStatusCode();

        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Division No Code",
                InviteCode = null, // Division has no code - no code required to join this division
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        userLoginResponse.EnsureSuccessStatusCode();

        // Act 1 - Join without code (should succeed since division has no code requirement)
        var joinWithoutCodeResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert 1
        Assert.Equal(HttpStatusCode.OK, joinWithoutCodeResponse.StatusCode);

        // Verify participation was created with the division
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(participation);
        Assert.Equal(division.Id, participation.DivisionId);
        Assert.Equal(ParticipationStatus.Accepted, participation.Status);

        // Act 2 - Create a new user and team to test joining with any code (should also succeed)
        var user2Password = "User2@Pass123";
        var user2 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), user2Password, token: TestContext.Current.CancellationToken);
        var team2 = await TestDataSeeder.CreateTeamAsync(factory.Services, user2.Id, $"Team {user2.UserName}", token: TestContext.Current.CancellationToken);

        using var user2Client = factory.CreateClient();
        var user2LoginResponse = await user2Client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user2.UserName, Password = user2Password }, cancellationToken: TestContext.Current.CancellationToken);
        user2LoginResponse.EnsureSuccessStatusCode();

        // Join with any code (since division requires no code, it should be ignored)
        var joinWithAnyCodeResponse = await user2Client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team2.Id, DivisionId = division.Id, InviteCode = "ANY_CODE" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert 2 - Should succeed because division has no code requirement
        Assert.Equal(HttpStatusCode.OK, joinWithAnyCodeResponse.StatusCode);

        // Check GetGameJoinCheckInfo API for first user - should show 1 team joined and 1 joinable division
        var checkInfoResponse1 = await userClient.GetAsync($"/api/Game/{game.Id}/Check", TestContext.Current.CancellationToken);
        checkInfoResponse1.EnsureSuccessStatusCode();
        var checkInfo1 = await checkInfoResponse1.Content.ReadFromJsonAsync<GameJoinCheckInfoModel>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(checkInfo1);
        // Should show 1 joined team
        Assert.Single(checkInfo1.JoinedTeams);
        Assert.Equal(team.Id, checkInfo1.JoinedTeams[0].TeamId);
        // Should show 1 joinable division (the one we created with no code)
        Assert.Single(checkInfo1.JoinableDivisions);
        Assert.Contains(division.Id, checkInfo1.JoinableDivisions);

        // Check GetGameJoinCheckInfo API for second user - should show 1 team joined and 1 joinable division
        var checkInfoResponse2 = await user2Client.GetAsync($"/api/Game/{game.Id}/Check", TestContext.Current.CancellationToken);
        checkInfoResponse2.EnsureSuccessStatusCode();
        var checkInfo2 = await checkInfoResponse2.Content.ReadFromJsonAsync<GameJoinCheckInfoModel>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(checkInfo2);
        // Should show 1 joined team
        Assert.Single(checkInfo2.JoinedTeams);
        Assert.Equal(team2.Id, checkInfo2.JoinedTeams[0].TeamId);
        // Should show 1 joinable division
        Assert.Single(checkInfo2.JoinableDivisions);
        Assert.Contains(division.Id, checkInfo2.JoinableDivisions);
    }

    /// <summary>
    /// Test: User can leave game with Pending status
    /// </summary>
    [Fact]
    public async Task LeaveGame_WithPendingStatus_ShouldSucceed()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        // Create game with review requirement
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Leave Game Test - Pending", acceptWithoutReview: false, token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Join the game (participation should be pending due to review requirement)
        var joinResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);
        joinResponse.EnsureSuccessStatusCode();

        // Verify participation is pending
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(participation);
        Assert.Equal(ParticipationStatus.Pending, participation.Status);

        // Act - Leave the game
        var leaveResponse = await client.DeleteAsync($"/api/Game/{game.Id}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, leaveResponse.StatusCode);

        // Verify participation was removed - create new scope to get fresh data
        using var scopeAfter = factory.Services.CreateScope();
        var participationRepoAfter = scopeAfter.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var removedParticipation = await participationRepoAfter.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.Null(removedParticipation);
    }

    /// <summary>
    /// Test: User can leave game with Rejected status
    /// Covers multiple participation status paths in LeaveGame
    /// </summary>
    [Fact]
    public async Task LeaveGame_WithRejectedStatus_ShouldSucceed()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Leave Game Test - Rejected", acceptWithoutReview: false, token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Join the game
        var joinResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);
        joinResponse.EnsureSuccessStatusCode();

        // Reject the participation via admin action
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(participation);

        await participationRepo.UpdateParticipationStatus(participation, ParticipationStatus.Rejected, TestContext.Current.CancellationToken);

        // Verify status is rejected
        Assert.Equal(ParticipationStatus.Rejected, participation.Status);

        // Act - Leave the game
        var leaveResponse = await client.DeleteAsync($"/api/Game/{game.Id}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, leaveResponse.StatusCode);

        // Verify participation was removed - create new scope to get fresh data from DB after HTTP operation
        using var scopeAfter = factory.Services.CreateScope();
        var participationRepoAfter = scopeAfter.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var removedParticipation = await participationRepoAfter.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.Null(removedParticipation);
    }

    /// <summary>
    /// Test: Comprehensive Leave Game error scenarios
    /// Tests multiple failure cases without requiring separate games:
    /// - GameNotFound (non-existent game ID)
    /// - NotJoined (user not in game)
    /// - AfterAccepted (cannot leave accepted participation)
    /// </summary>
    [Fact]
    public async Task LeaveGame_ErrorScenarios_ShouldFail()
    {
        // Arrange - Setup single user and shared game for reuse
        var gameId = await TestDataSeeder.GetOrCreateBasicGameAsync(factory.Services);
        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Scenario 1: Try to leave non-existent game
        var leaveNonExistentResponse = await client.DeleteAsync("/api/Game/99999", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, leaveNonExistentResponse.StatusCode);
        var nonExistentError = await leaveNonExistentResponse.Content.ReadFromJsonAsync<RequestResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(nonExistentError);
        Assert.Contains("not found", nonExistentError.Title, StringComparison.OrdinalIgnoreCase);

        // Scenario 2: Try to leave without joining first
        var leaveNotJoinedResponse = await client.DeleteAsync($"/api/Game/{gameId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, leaveNotJoinedResponse.StatusCode);
        var notJoinedError = await leaveNotJoinedResponse.Content.ReadFromJsonAsync<RequestResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(notJoinedError);
        Assert.Contains("not joining", notJoinedError.Title, StringComparison.OrdinalIgnoreCase);

        // Scenario 3: Join game (auto-accepted) and try to leave after acceptance
        var joinResponse = await client.PostAsJsonAsync($"/api/Game/{gameId}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);
        joinResponse.EnsureSuccessStatusCode();

        // Verify accepted status
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, gameId, TestContext.Current.CancellationToken);
        Assert.NotNull(participation);
        Assert.Equal(ParticipationStatus.Accepted, participation.Status);

        // Try to leave after acceptance
        var leaveAcceptedResponse = await client.DeleteAsync($"/api/Game/{gameId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, leaveAcceptedResponse.StatusCode);
        var acceptedError = await leaveAcceptedResponse.Content.ReadFromJsonAsync<RequestResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(acceptedError);
        Assert.Contains("entrance approval", acceptedError.Title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test: User cannot leave game with Suspended status
    /// Another status variant that should prevent leaving
    /// </summary>
    [Fact]
    public async Task LeaveGame_AfterSuspended_ShouldFail()
    {
        // Arrange
        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Leave Game Test - Suspended", acceptWithoutReview: false, token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Join the game
        var joinResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);
        joinResponse.EnsureSuccessStatusCode();

        // Suspend the participation
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(participation);

        await participationRepo.UpdateParticipationStatus(participation, ParticipationStatus.Suspended, TestContext.Current.CancellationToken);

        // Verify status is suspended
        Assert.Equal(ParticipationStatus.Suspended, participation.Status);

        // Act - Try to leave after suspension
        var leaveResponse = await client.DeleteAsync($"/api/Game/{game.Id}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, leaveResponse.StatusCode);
        var error = await leaveResponse.Content.ReadFromJsonAsync<RequestResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(error);
        Assert.Contains("entrance approval", error.Title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test: Team participation is removed when last member leaves
    /// </summary>
    [Fact]
    public async Task LeaveGame_LastMemberLeaves_RemovesParticipation()
    {
        // Arrange
        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Leave Game Test - Single Member", acceptWithoutReview: false, token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Join the game
        var joinResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);
        joinResponse.EnsureSuccessStatusCode();

        // Verify participation exists with 1 member
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existingParticipation = await context.Participations
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.TeamId == team.Id && p.GameId == game.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(existingParticipation);
        Assert.Single(existingParticipation.Members);

        // Act - Leave the game
        var leaveResponse = await client.DeleteAsync($"/api/Game/{game.Id}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, leaveResponse.StatusCode);

        // Verify participation was completely removed - create new scope to get fresh data
        using var scopeAfter = factory.Services.CreateScope();
        var contextAfter = scopeAfter.ServiceProvider.GetRequiredService<AppDbContext>();
        var removedParticipation = await contextAfter.Participations
            .FirstOrDefaultAsync(p => p.TeamId == team.Id && p.GameId == game.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(removedParticipation);
    }

    /// <summary>
    /// Test: Other team members remain after one member leaves
    /// </summary>
    [Fact]
    public async Task LeaveGame_PartialMemberLeaves_PreservesOtherMembers()
    {
        // Arrange
        var user1Password = "User1@Pass123";
        var user1 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), user1Password, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user1.Id, $"Team {user1.UserName}", token: TestContext.Current.CancellationToken);

        var user2Password = "User2@Pass123";
        var user2 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), user2Password, token: TestContext.Current.CancellationToken);

        // Add user2 to team
        using var scopeSetup = factory.Services.CreateScope();
        var context = scopeSetup.ServiceProvider.GetRequiredService<AppDbContext>();
        var teamEntity = await context.Teams.Include(t => t.Members)
            .FirstAsync(t => t.Id == team.Id, cancellationToken: TestContext.Current.CancellationToken);
        var user2Entity = await context.Users.FirstAsync(u => u.Id == user2.Id, cancellationToken: TestContext.Current.CancellationToken);
        teamEntity.Members.Add(user2Entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Leave Game Test - Multiple Members", acceptWithoutReview: false, token: TestContext.Current.CancellationToken);

        // Both users join the game
        using var client1 = factory.CreateClient();
        var login1Response = await client1.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user1.UserName, Password = user1Password }, cancellationToken: TestContext.Current.CancellationToken);
        login1Response.EnsureSuccessStatusCode();

        var join1Response = await client1.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);
        join1Response.EnsureSuccessStatusCode();

        using var client2 = factory.CreateClient();
        var login2Response = await client2.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user2.UserName, Password = user2Password }, cancellationToken: TestContext.Current.CancellationToken);
        login2Response.EnsureSuccessStatusCode();

        var join2Response = await client2.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null }, cancellationToken: TestContext.Current.CancellationToken);
        join2Response.EnsureSuccessStatusCode();

        // Verify both members are in the participation
        using var scope = factory.Services.CreateScope();
        var context2 = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = await context2.Participations
            .Include(p => p.Members)
            .FirstAsync(p => p.TeamId == team.Id && p.GameId == game.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, participation.Members.Count);

        // Act - User1 leaves the game
        var leaveResponse = await client1.DeleteAsync($"/api/Game/{game.Id}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, leaveResponse.StatusCode);

        // Verify participation still exists with only user2 - create new scope to get fresh data from DB after HTTP operation
        using var scopeAfter = factory.Services.CreateScope();
        var contextAfter = scopeAfter.ServiceProvider.GetRequiredService<AppDbContext>();
        var remainingParticipation = await contextAfter.Participations
            .Include(p => p.Members)
            .FirstAsync(p => p.TeamId == team.Id && p.GameId == game.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(remainingParticipation.Members);
        Assert.Equal(user2.Id, remainingParticipation.Members.First().UserId);
    }

    /// <summary>
    /// Test: Leave game with division
    /// Verifies that division context doesn't prevent leaving in Pending status
    /// </summary>
    [Fact]
    public async Task LeaveGame_WithDivision_ShouldSucceed()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword, role: Role.Admin, token: TestContext.Current.CancellationToken);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {user.UserName}", token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Leave Game Test - With Division", acceptWithoutReview: false, token: TestContext.Current.CancellationToken);

        // Create division
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = admin.UserName, Password = adminPassword }, cancellationToken: TestContext.Current.CancellationToken);
        adminLoginResponse.EnsureSuccessStatusCode();

        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions", new DivisionCreateModel
            {
                Name = "Leave Division",
                InviteCode = "LEAVE_DIV",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Join with division
        var joinResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "LEAVE_DIV" }, cancellationToken: TestContext.Current.CancellationToken);
        joinResponse.EnsureSuccessStatusCode();

        // Change division to require review so participation stays pending
        using var scope = factory.Services.CreateScope();
        var divisionRepo = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();
        var divEntity = await divisionRepo.GetDivision(game.Id, division.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(divEntity);
        divEntity.DefaultPermissions = GamePermission.All; // Add RequireReview
        await divisionRepo.SaveAsync(TestContext.Current.CancellationToken);

        // Verify participation is accepted
        using var scope2 = factory.Services.CreateScope();
        var participationRepo = scope2.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(participation);
        Assert.Equal(ParticipationStatus.Accepted, participation.Status);

        // Manually set to pending to test leaving pending division participation
        await participationRepo.UpdateParticipationStatus(participation, ParticipationStatus.Pending, TestContext.Current.CancellationToken);

        // Act - Leave the game
        var leaveResponse = await client.DeleteAsync($"/api/Game/{game.Id}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, leaveResponse.StatusCode);

        // Verify participation was removed - create new scope to get fresh data
        using var scopeAfter = factory.Services.CreateScope();
        var participationRepoAfter = scopeAfter.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var removedParticipation = await participationRepoAfter.GetParticipation(user.Id, game.Id, TestContext.Current.CancellationToken);
        Assert.Null(removedParticipation);
    }

    [Fact]
    public async Task JoinGame_WhenParticipationSuspended_NewMemberCannotJoin()
    {
        var ownerPassword = "Owner@Pass123";
        var owner = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), ownerPassword,
            token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, owner.Id, $"Team {owner.UserName}",
            token: TestContext.Current.CancellationToken);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Suspension Bypass Test Game",
            acceptWithoutReview: true, token: TestContext.Current.CancellationToken);

        using var ownerClient = factory.CreateClient();
        var ownerLogin = await ownerClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = owner.UserName, Password = ownerPassword },
            cancellationToken: TestContext.Current.CancellationToken);
        ownerLogin.EnsureSuccessStatusCode();

        var joinResponse = await ownerClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null },
            cancellationToken: TestContext.Current.CancellationToken);
        joinResponse.EnsureSuccessStatusCode();

        var newcomerPassword = "Newcomer@Pass123";
        var newcomer = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(),
            newcomerPassword, token: TestContext.Current.CancellationToken);

        using (var setupScope = factory.Services.CreateScope())
        {
            var context = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var teamEntity = await context.Teams.Include(t => t.Members)
                .FirstAsync(t => t.Id == team.Id, cancellationToken: TestContext.Current.CancellationToken);
            var newcomerEntity = await context.Users
                .FirstAsync(u => u.Id == newcomer.Id, cancellationToken: TestContext.Current.CancellationToken);
            teamEntity.Members.Add(newcomerEntity);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
            var participation = await participationRepo.GetParticipation(owner.Id, game.Id,
                TestContext.Current.CancellationToken);
            Assert.NotNull(participation);
            await participationRepo.UpdateParticipationStatus(participation, ParticipationStatus.Suspended,
                TestContext.Current.CancellationToken);
        }

        using var newcomerClient = factory.CreateClient();
        var newcomerLogin = await newcomerClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = newcomer.UserName, Password = newcomerPassword },
            cancellationToken: TestContext.Current.CancellationToken);
        newcomerLogin.EnsureSuccessStatusCode();

        var rejoinResponse = await newcomerClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, rejoinResponse.StatusCode);
        var error = await rejoinResponse.Content.ReadFromJsonAsync<RequestResponse>(
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(error);
        Assert.Contains("rejected or banned", error.Title, StringComparison.OrdinalIgnoreCase);

        using var scopeAfter = factory.Services.CreateScope();
        var participationRepoAfter = scopeAfter.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var updatedParticipation = await participationRepoAfter.GetParticipation(owner.Id, game.Id,
            TestContext.Current.CancellationToken);
        Assert.NotNull(updatedParticipation);
        Assert.Equal(ParticipationStatus.Suspended, updatedParticipation.Status);
        Assert.Single(updatedParticipation.Members);
        Assert.Equal(owner.Id, updatedParticipation.Members.Single().UserId);

        var detailsResponse = await ownerClient.GetAsync($"/api/Game/{game.Id}/Details",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, detailsResponse.StatusCode);
    }

    [Fact]
    public async Task JoinGame_WhenDivisionParticipationSuspended_OnlyAdminCanRestore()
    {
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), adminPassword,
            role: Role.Admin, token: TestContext.Current.CancellationToken);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Suspension Bypass Division Game",
            acceptWithoutReview: true, token: TestContext.Current.CancellationToken);

        using var adminClient = factory.CreateClient();
        var adminLogin = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = adminPassword },
            cancellationToken: TestContext.Current.CancellationToken);
        adminLogin.EnsureSuccessStatusCode();

        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Suspended Division",
                InviteCode = "SUSP_DIV",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            }, cancellationToken: TestContext.Current.CancellationToken);
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>(
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(division);

        var ownerPassword = "Owner@Pass123";
        var owner = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), ownerPassword,
            token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, owner.Id, $"Team {owner.UserName}",
            token: TestContext.Current.CancellationToken);

        using var ownerClient = factory.CreateClient();
        var ownerLogin = await ownerClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = owner.UserName, Password = ownerPassword },
            cancellationToken: TestContext.Current.CancellationToken);
        ownerLogin.EnsureSuccessStatusCode();

        var joinResponse = await ownerClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "SUSP_DIV" },
            cancellationToken: TestContext.Current.CancellationToken);
        joinResponse.EnsureSuccessStatusCode();

        var newcomerPassword = "Newcomer@Pass123";
        var newcomer = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(),
            newcomerPassword, token: TestContext.Current.CancellationToken);

        using (var setupScope = factory.Services.CreateScope())
        {
            var context = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var teamEntity = await context.Teams.Include(t => t.Members)
                .FirstAsync(t => t.Id == team.Id, cancellationToken: TestContext.Current.CancellationToken);
            var newcomerEntity = await context.Users
                .FirstAsync(u => u.Id == newcomer.Id, cancellationToken: TestContext.Current.CancellationToken);
            teamEntity.Members.Add(newcomerEntity);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        int participationId;
        using (var scope = factory.Services.CreateScope())
        {
            var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
            var participation = await participationRepo.GetParticipation(owner.Id, game.Id,
                TestContext.Current.CancellationToken);
            Assert.NotNull(participation);
            participationId = participation.Id;
        }

        var suspendResponse = await adminClient.PutAsJsonAsync($"/api/Admin/Participation/{participationId}",
            new ParticipationEditModel { Status = ParticipationStatus.Suspended, DivisionId = division.Id },
            cancellationToken: TestContext.Current.CancellationToken);
        suspendResponse.EnsureSuccessStatusCode();

        using var newcomerClient = factory.CreateClient();
        var newcomerLogin = await newcomerClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = newcomer.UserName, Password = newcomerPassword },
            cancellationToken: TestContext.Current.CancellationToken);
        newcomerLogin.EnsureSuccessStatusCode();

        var rejoinResponse = await newcomerClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "SUSP_DIV" },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, rejoinResponse.StatusCode);

        using (var scopeAfter = factory.Services.CreateScope())
        {
            var participationRepoAfter = scopeAfter.ServiceProvider.GetRequiredService<IParticipationRepository>();
            var suspendedParticipation = await participationRepoAfter.GetParticipation(owner.Id, game.Id,
                TestContext.Current.CancellationToken);
            Assert.NotNull(suspendedParticipation);
            Assert.Equal(ParticipationStatus.Suspended, suspendedParticipation.Status);
            Assert.Single(suspendedParticipation.Members);
        }

        var restoreResponse = await adminClient.PutAsJsonAsync($"/api/Admin/Participation/{participationId}",
            new ParticipationEditModel { Status = ParticipationStatus.Accepted, DivisionId = division.Id },
            cancellationToken: TestContext.Current.CancellationToken);
        restoreResponse.EnsureSuccessStatusCode();

        var joinAfterRestoreResponse = await newcomerClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "SUSP_DIV" },
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, joinAfterRestoreResponse.StatusCode);

        using var finalScope = factory.Services.CreateScope();
        var finalParticipationRepo = finalScope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var restoredParticipation = await finalParticipationRepo.GetParticipation(owner.Id, game.Id,
            TestContext.Current.CancellationToken);
        Assert.NotNull(restoredParticipation);
        Assert.Equal(ParticipationStatus.Accepted, restoredParticipation.Status);
        Assert.Equal(2, restoredParticipation.Members.Count);
    }
}
