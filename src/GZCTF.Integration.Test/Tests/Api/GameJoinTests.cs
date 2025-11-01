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
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Act
        var joinResponse = await client.PostAsJsonAsync($"/api/Game/{gameId}",
            new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null });

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
    /// </summary>
    [Fact]
    public async Task JoinGame_WithInvalidDivision_ShouldFail()
    {
        // Arrange
        var gameId = await TestDataSeeder.GetOrCreateBasicGameAsync(factory.Services);
        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Act - Try to join with non-existent division
        var joinResponse = await client.PostAsJsonAsync($"/api/Game/{gameId}",
            new GameJoinModel { TeamId = team.Id, DivisionId = 99999, InviteCode = "WRONG" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, joinResponse.StatusCode);
        var error = await joinResponse.Content.ReadFromJsonAsync<RequestResponse>();
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
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        // Create regular user and team
        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Division Game");

        // Create division as admin
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = adminPassword });
        adminLoginResponse.EnsureSuccessStatusCode();

        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Test Division",
                InviteCode = "TESTDIV2025",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division);

        // Act - User joins with correct invite code
        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        userLoginResponse.EnsureSuccessStatusCode();

        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "TESTDIV2025" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        // Verify participation
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id);
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
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Invite Code Game");

        // Create division with invite code
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = adminPassword });
        adminLoginResponse.EnsureSuccessStatusCode();

        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Secure Division",
                InviteCode = "CORRECT_CODE",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division);

        // Act - Try to join with wrong code
        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        userLoginResponse.EnsureSuccessStatusCode();

        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "WRONG_CODE" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, joinResponse.StatusCode);
        var error = await joinResponse.Content.ReadFromJsonAsync<RequestResponse>();
        Assert.NotNull(error);
        Assert.Contains("invitation", error.Title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 5: User cannot join division without JoinGame permission
    /// </summary>
    [Fact]
    public async Task JoinGame_DivisionWithoutJoinPermission_ShouldFail()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Restricted Division Game");

        // Create division without JoinGame permission
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = adminPassword });
        adminLoginResponse.EnsureSuccessStatusCode();

        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "No Join Division",
                InviteCode = "NOJOIN",
                DefaultPermissions = GamePermission.All & ~GamePermission.JoinGame & ~GamePermission.RequireReview
            });
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division);

        // Act - Try to join
        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        userLoginResponse.EnsureSuccessStatusCode();

        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "NOJOIN" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, joinResponse.StatusCode);
        var error = await joinResponse.Content.ReadFromJsonAsync<RequestResponse>();
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
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Rejoin Test Game");

        // Create two divisions
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = adminPassword });
        adminLoginResponse.EnsureSuccessStatusCode();

        var division1Response = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Division 1",
                InviteCode = "DIV1",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });
        division1Response.EnsureSuccessStatusCode();
        var division1 = await division1Response.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division1);

        var division2Response = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Division 2",
                InviteCode = "DIV2",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });
        division2Response.EnsureSuccessStatusCode();
        var division2 = await division2Response.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division2);

        // User joins Division 1
        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        userLoginResponse.EnsureSuccessStatusCode();

        var join1Response = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division1.Id, InviteCode = "DIV1" });
        join1Response.EnsureSuccessStatusCode();

        // Admin rejects the participation
        using var scope1 = factory.Services.CreateScope();
        var participationRepo = scope1.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var gameRepo = scope1.ServiceProvider.GetRequiredService<IGameRepository>();
        var teamRepo = scope1.ServiceProvider.GetRequiredService<ITeamRepository>();

        var gameEntity = await gameRepo.GetGameById(game.Id);
        var teamEntity = await teamRepo.GetTeamById(team.Id);
        Assert.NotNull(gameEntity);
        Assert.NotNull(teamEntity);

        var participation = await participationRepo.GetParticipation(teamEntity, gameEntity);
        Assert.NotNull(participation);

        await participationRepo.UpdateParticipationStatus(participation, ParticipationStatus.Rejected);

        // Act - User rejoins with Division 2
        var join2Response = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division2.Id, InviteCode = "DIV2" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, join2Response.StatusCode);

        // Verify division changed and status is pending
        using var scope2 = factory.Services.CreateScope();
        var participationRepo2 = scope2.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var updatedParticipation = await participationRepo2.GetParticipation(user.Id, game.Id);
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
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "No Change Division Game");

        // Create two divisions
        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = adminPassword });
        adminLoginResponse.EnsureSuccessStatusCode();

        var division1Response = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Division Alpha",
                InviteCode = "ALPHA",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });
        division1Response.EnsureSuccessStatusCode();
        var division1 = await division1Response.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division1);

        var division2Response = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Division Beta",
                InviteCode = "BETA",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });
        division2Response.EnsureSuccessStatusCode();
        var division2 = await division2Response.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division2);

        // User joins Division 1 and gets accepted
        using var userClient = factory.CreateClient();
        var userLoginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        userLoginResponse.EnsureSuccessStatusCode();

        var join1Response = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division1.Id, InviteCode = "ALPHA" });
        join1Response.EnsureSuccessStatusCode();

        // Verify accepted status
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id);
        Assert.NotNull(participation);
        Assert.Equal(ParticipationStatus.Accepted, participation.Status);

        // Act - Try to join with different division
        var join2Response = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division2.Id, InviteCode = "BETA" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, join2Response.StatusCode);
        var error = await join2Response.Content.ReadFromJsonAsync<RequestResponse>();
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
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        loginResponse.EnsureSuccessStatusCode();

        // First join
        var join1Response = await client.PostAsJsonAsync($"/api/Game/{gameId}",
            new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null });
        Assert.Equal(HttpStatusCode.OK, join1Response.StatusCode);

        // Act - Join again with same parameters
        var join2Response = await client.PostAsJsonAsync($"/api/Game/{gameId}",
            new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null });

        // Assert - Should return bad request indicating already joined
        Assert.Equal(HttpStatusCode.BadRequest, join2Response.StatusCode);

        // Verify only one membership exists
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var memberCount = await context.UserParticipations
            .CountAsync(up => up.GameId == gameId && up.TeamId == team.Id && up.UserId == user.Id);
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
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Team Switch Test Game");
        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team1 = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team 1 {user.UserName}");
        var team2 = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team 2 {user.UserName}");

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Join with team1
        var join1Response = await client.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team1.Id, DivisionId = null, InviteCode = null });
        join1Response.EnsureSuccessStatusCode();

        // Act - Try to join with team2
        var join2Response = await client.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team2.Id, DivisionId = null, InviteCode = null });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, join2Response.StatusCode);
        var error = await join2Response.Content.ReadFromJsonAsync<RequestResponse>();
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
        var user1 = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), user1Password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user1.Id,
            $"Team {user1.UserName}");

        var user2Password = "User2@Pass123";
        var user2 = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), user2Password);

        // Add user2 to team
        using var scope1 = factory.Services.CreateScope();
        var context1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var teamEntity = await context1.Teams.Include(t => t.Members)
            .FirstAsync(t => t.Id == team.Id);
        var user2Entity = await context1.Users.FirstAsync(u => u.Id == user2.Id);
        teamEntity.Members.Add(user2Entity);
        await context1.SaveChangesAsync();

        // Act - Both users join
        using var client1 = factory.CreateClient();
        var login1Response = await client1.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user1.UserName, Password = user1Password });
        login1Response.EnsureSuccessStatusCode();

        var join1Response = await client1.PostAsJsonAsync($"/api/Game/{gameId}",
            new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null });
        Assert.Equal(HttpStatusCode.OK, join1Response.StatusCode);

        using var client2 = factory.CreateClient();
        var login2Response = await client2.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user2.UserName, Password = user2Password });
        login2Response.EnsureSuccessStatusCode();

        var join2Response = await client2.PostAsJsonAsync($"/api/Game/{gameId}",
            new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = null });

        // Assert
        Assert.Equal(HttpStatusCode.OK, join2Response.StatusCode);

        // Verify both users are in the participation
        using var scope2 = factory.Services.CreateScope();
        var context2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var userParticipations = await context2.UserParticipations
            .Where(up => up.GameId == gameId && up.TeamId == team.Id)
            .ToListAsync();
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
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Limited Team Game");

        using var scope0 = factory.Services.CreateScope();
        var gameRepo = scope0.ServiceProvider.GetRequiredService<IGameRepository>();
        var gameEntity = await gameRepo.GetGameById(game.Id);
        Assert.NotNull(gameEntity);
        gameEntity.TeamMemberCountLimit = 2;
        await gameRepo.SaveAsync();

        var user1Password = "User1@Pass123";
        var user1 = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), user1Password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user1.Id,
            $"Team {user1.UserName}");

        var user2Password = "User2@Pass123";
        var user2 = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), user2Password);

        var user3Password = "User3@Pass123";
        var user3 = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), user3Password);

        // Add users to team
        using var scope1 = factory.Services.CreateScope();
        var context1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var teamEntity = await context1.Teams.Include(t => t.Members)
            .FirstAsync(t => t.Id == team.Id);
        teamEntity.Members.Add(await context1.Users.FirstAsync(u => u.Id == user2.Id));
        teamEntity.Members.Add(await context1.Users.FirstAsync(u => u.Id == user3.Id));
        await context1.SaveChangesAsync();

        // User1 and User2 join successfully
        using var client1 = factory.CreateClient();
        await client1.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user1.UserName, Password = user1Password });
        var join1 = await client1.PostAsJsonAsync($"/api/Game/{gameEntity.Id}",
            new GameJoinModel { TeamId = team.Id });
        join1.EnsureSuccessStatusCode();

        using var client2 = factory.CreateClient();
        await client2.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user2.UserName, Password = user2Password });
        var join2 = await client2.PostAsJsonAsync($"/api/Game/{gameEntity.Id}",
            new GameJoinModel { TeamId = team.Id });
        join2.EnsureSuccessStatusCode();

        // Act - User3 tries to join (exceeds limit)
        using var client3 = factory.CreateClient();
        await client3.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user3.UserName, Password = user3Password });
        var join3Response = await client3.PostAsJsonAsync($"/api/Game/{gameEntity.Id}",
            new GameJoinModel { TeamId = team.Id });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, join3Response.StatusCode);
        var error = await join3Response.Content.ReadFromJsonAsync<RequestResponse>();
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
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });

        // Act - Try to join without invite code
        var joinWrongResponse = await client.PostAsJsonAsync($"/api/Game/{gameId}",
            new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = "WRONG" });

        // Assert - Wrong code should fail
        Assert.Equal(HttpStatusCode.BadRequest, joinWrongResponse.StatusCode);

        // Act - Join with correct code
        var joinCorrectResponse = await client.PostAsJsonAsync($"/api/Game/{gameId}",
            new GameJoinModel { TeamId = team.Id, DivisionId = null, InviteCode = "SHARED_INVITE_2025" });

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
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Division Priority Test Game");

        // Set game invite code via API
        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = adminPassword });

        var gameUpdateResponse = await adminClient.PutAsJsonAsync($"/api/Edit/Games/{game.Id}",
            new GameInfoModel
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
            });
        gameUpdateResponse.EnsureSuccessStatusCode();

        // Create division with its own invite code
        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "PriorityDiv", // Shorter name to fit within max length
                InviteCode = "DIVCODE",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");

        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });

        // Act - Try to join division with game code (should fail)
        var joinGameCodeResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "GAME_CODE" });

        // Assert - Should fail because division code is required
        Assert.Equal(HttpStatusCode.BadRequest, joinGameCodeResponse.StatusCode);

        // Act - Join with division code (should succeed)
        var joinDivCodeResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "DIVCODE" });

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
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Division Review Test Game",
            acceptWithoutReview: true); // Game allows auto-accept

        // Create division that requires review
        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = adminPassword });

        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "ReviewRequiredDiv",
                InviteCode = "REVIEW_DIV",
                DefaultPermissions = GamePermission.All // Includes RequireReview by default
            });
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");

        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });

        // Act - Join division that requires review
        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "REVIEW_DIV" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        // Verify participation is pending (not auto-accepted)
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id);
        Assert.NotNull(participation);
        Assert.Equal(division.Id, participation.DivisionId);
        Assert.Equal(ParticipationStatus.Pending,
            participation.Status); // Should be pending due to division requiring review
    }
}
