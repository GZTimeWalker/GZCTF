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
/// Integration tests for GameController JoinGame participation status logic fix
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class GameJoinParticipationTests(GZCTFApplicationFactory factory)
{
    /// <summary>
    /// Test that joining a game with RequireReview division sets status to Pending
    /// </summary>
    [Fact]
    public async Task JoinGame_WithRequireReviewDivision_ShouldSetStatusToPending()
    {
        // Arrange: Create admin, game, and division with RequireReview
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Review Required Game",
            acceptWithoutReview: false);

        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });

        // Create division with RequireReview permission
        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Review Division",
                InviteCode = "REVIEW123",
                DefaultPermissions = GamePermission.All | GamePermission.RequireReview
            });
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division);

        // Create user and team
        var userPassword = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {userName}");

        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });

        // Act: Join game with RequireReview division
        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "REVIEW123" });

        // Assert: Should be successful but with Pending status
        joinResponse.EnsureSuccessStatusCode();

        // Verify participation status is Pending
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = await context.Participations
            .FirstOrDefaultAsync(p => p.GameId == game.Id && p.TeamId == team.Id);

        Assert.NotNull(participation);
        Assert.Equal(ParticipationStatus.Pending, participation.Status);
    }

    /// <summary>
    /// Test that joining a game without RequireReview division and with AcceptWithoutReview=false still sets to Pending
    /// </summary>
    [Fact]
    public async Task JoinGame_WithoutRequireReviewDivisionButAcceptWithoutReviewFalse_ShouldSetStatusToPending()
    {
        // Arrange: Create admin, game with AcceptWithoutReview=false, and division without RequireReview
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Manual Approval Game",
            acceptWithoutReview: false);

        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });

        // Create division without RequireReview
        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Auto Join Division",
                InviteCode = "AUTOJOIN",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division);

        // Create user and team
        var userPassword = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {userName}");

        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });

        // Act: Join game (division doesn't require review but game has AcceptWithoutReview=false)
        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "AUTOJOIN" });

        // Assert: Should be successful with Pending status due to game-level setting
        joinResponse.EnsureSuccessStatusCode();

        // Verify participation status is Pending
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = await context.Participations
            .FirstOrDefaultAsync(p => p.GameId == game.Id && p.TeamId == team.Id);

        Assert.NotNull(participation);
        Assert.Equal(ParticipationStatus.Pending, participation.Status);
    }

    /// <summary>
    /// Test that joining a game without RequireReview division and with AcceptWithoutReview=true sets to Accepted
    /// </summary>
    [Fact]
    public async Task JoinGame_WithoutRequireReviewDivisionAndAcceptWithoutReviewTrue_ShouldSetStatusToAccepted()
    {
        // Arrange: Create admin, game with AcceptWithoutReview=true, and division without RequireReview
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Auto Accept Game",
            acceptWithoutReview: true);

        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });

        // Create division without RequireReview
        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Auto Accept Division",
                InviteCode = "AUTOACCEPT",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division);

        // Create user and team
        var userPassword = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {userName}");

        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });

        // Act: Join game (division doesn't require review and game has AcceptWithoutReview=true)
        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "AUTOACCEPT" });

        // Assert: Should be successful with Accepted status
        joinResponse.EnsureSuccessStatusCode();

        // Verify participation status is Accepted
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = await context.Participations
            .FirstOrDefaultAsync(p => p.GameId == game.Id && p.TeamId == team.Id);

        Assert.NotNull(participation);
        Assert.Equal(ParticipationStatus.Accepted, participation.Status);
    }

    /// <summary>
    /// Test that joining a game with RequireReview division always sets to Pending regardless of game setting
    /// </summary>
    [Fact]
    public async Task JoinGame_WithRequireReviewDivisionAndAcceptWithoutReviewTrue_ShouldStillSetStatusToPending()
    {
        // Arrange: Create admin, game with AcceptWithoutReview=true, but division with RequireReview
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Division Override Game",
            acceptWithoutReview: true); // Game accepts without review

        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });

        // Create division WITH RequireReview (overrides game setting)
        var divisionResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Strict Division",
                InviteCode = "STRICT123",
                DefaultPermissions = GamePermission.All | GamePermission.RequireReview
            });
        divisionResponse.EnsureSuccessStatusCode();
        var division = await divisionResponse.Content.ReadFromJsonAsync<Division>();
        Assert.NotNull(division);

        // Create user and team
        var userPassword = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {userName}");

        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });

        // Act: Join game with RequireReview division (even though game has AcceptWithoutReview=true)
        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id, DivisionId = division.Id, InviteCode = "STRICT123" });

        // Assert: Should be Pending because division RequireReview takes precedence
        joinResponse.EnsureSuccessStatusCode();

        // Verify participation status is Pending
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = await context.Participations
            .FirstOrDefaultAsync(p => p.GameId == game.Id && p.TeamId == team.Id);

        Assert.NotNull(participation);
        Assert.Equal(ParticipationStatus.Pending, participation.Status);
    }

    /// <summary>
    /// Test joining without division respects game's AcceptWithoutReview setting
    /// </summary>
    [Fact]
    public async Task JoinGame_WithoutDivision_ShouldRespectGameAcceptWithoutReviewSetting()
    {
        // Arrange: Create two games with different AcceptWithoutReview settings
        var game1 = await TestDataSeeder.CreateGameAsync(factory.Services, "Auto Accept Game No Division",
            acceptWithoutReview: true);
        var game2 = await TestDataSeeder.CreateGameAsync(factory.Services, "Manual Review Game No Division",
            acceptWithoutReview: false);

        // Create user and teams
        var userPassword = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, userPassword);
        var team1 = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team1 {userName}");
        var team2 = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team2 {userName}");

        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });

        // Act: Join game1 without division (should be auto-accepted)
        var joinResponse1 = await userClient.PostAsJsonAsync($"/api/Game/{game1.Id}",
            new GameJoinModel { TeamId = team1.Id });
        joinResponse1.EnsureSuccessStatusCode();

        // Act: Join game2 without division (should be pending)
        var joinResponse2 = await userClient.PostAsJsonAsync($"/api/Game/{game2.Id}",
            new GameJoinModel { TeamId = team2.Id });
        joinResponse2.EnsureSuccessStatusCode();

        // Assert: Verify participation statuses
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var participation1 = await context.Participations
            .FirstOrDefaultAsync(p => p.GameId == game1.Id && p.TeamId == team1.Id);
        Assert.NotNull(participation1);
        Assert.Equal(ParticipationStatus.Accepted, participation1.Status);

        var participation2 = await context.Participations
            .FirstOrDefaultAsync(p => p.GameId == game2.Id && p.TeamId == team2.Id);
        Assert.NotNull(participation2);
        Assert.Equal(ParticipationStatus.Pending, participation2.Status);
    }

    /// <summary>
    /// Test edge case: Division is null but game has AcceptWithoutReview=false
    /// </summary>
    [Fact]
    public async Task JoinGame_NullDivisionWithGameRequiringReview_ShouldSetStatusToPending()
    {
        // Arrange
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "No Division Manual Review",
            acceptWithoutReview: false);

        var userPassword = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {userName}");

        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });

        // Act: Join without division
        var joinResponse = await userClient.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id });

        // Assert
        joinResponse.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = await context.Participations
            .FirstOrDefaultAsync(p => p.GameId == game.Id && p.TeamId == team.Id);

        Assert.NotNull(participation);
        Assert.Equal(ParticipationStatus.Pending, participation.Status);
    }
}
