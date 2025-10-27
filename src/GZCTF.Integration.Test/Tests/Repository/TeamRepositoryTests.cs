using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Repository;

/// <summary>
/// Integration tests for TeamRepository focusing on search, locking, and team operations
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class TeamRepositoryTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task SearchTeams_ShouldFindByName()
    {
        using var scope = factory.Services.CreateScope();
        var teamRepo = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        // Create teams with distinct names and different captains
        var user1 = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123", $"search1{Guid.NewGuid():N}@test.com");
        var user2 = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123", $"search2{Guid.NewGuid():N}@test.com");
        var user3 = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123", $"search3{Guid.NewGuid():N}@test.com");

        var team1 = await TestDataSeeder.CreateTeamAsync(factory.Services, user1.Id, "SearchableTeam Alpha");
        var team2 = await TestDataSeeder.CreateTeamAsync(factory.Services, user2.Id, "SearchableTeam Beta");
        var team3 = await TestDataSeeder.CreateTeamAsync(factory.Services, user3.Id, "DifferentName Gamma");

        // Search for teams containing "searchable"
        var results = await teamRepo.SearchTeams("searchable", CancellationToken.None);

        Assert.True(results.Length >= 2);
        Assert.Contains(results, t => t.Id == team1.Id);
        Assert.Contains(results, t => t.Id == team2.Id);

        // Search for specific team
        var alphaResults = await teamRepo.SearchTeams("alpha", CancellationToken.None);
        Assert.Contains(alphaResults, t => t.Id == team1.Id);

        output.WriteLine(
            $"Search tests passed - Found {results.Length} teams for 'searchable', {alphaResults.Length} for 'alpha'");
    }

    [Fact]
    public async Task SearchTeams_ShouldFindById()
    {
        using var scope = factory.Services.CreateScope();
        var teamRepo = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        // Create team
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Team By ID");

        // Search by ID
        var results = await teamRepo.SearchTeams(team.Id.ToString(), CancellationToken.None);

        Assert.Contains(results, t => t.Id == team.Id);

        output.WriteLine($"Search by ID test passed - Found team {team.Id}");
    }

    [Fact]
    public async Task AnyActiveGame_ShouldReturnFalse_WhenNoActiveGames()
    {
        using var scope = factory.Services.CreateScope();
        var teamRepo = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        // Create team
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "No Active Games Team");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var teamEntity = await context.Teams.FindAsync(team.Id);
        Assert.NotNull(teamEntity);

        // Check no active games
        var hasActive = await teamRepo.AnyActiveGame(teamEntity, CancellationToken.None);
        Assert.False(hasActive);

        output.WriteLine($"No active games test passed for team {team.Id}");
    }

    [Fact]
    public async Task AnyActiveGame_ShouldReturnTrue_WhenGameActive()
    {
        using var scope = factory.Services.CreateScope();
        var teamRepo = scope.ServiceProvider.GetRequiredService<ITeamRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create team
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Active Game Team");

        // Create active game
        var game = new Game
        {
            Title = "Active Game Test",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2), // Still active
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create participation
        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = new Participation
        {
            GameId = game.Id,
            TeamId = team.Id,
            Status = ParticipationStatus.Accepted
        };

        await context.Participations.AddAsync(participation);
        await context.SaveChangesAsync();

        var teamEntity = await context.Teams.FindAsync(team.Id);
        Assert.NotNull(teamEntity);

        // Check has active games
        var hasActive = await teamRepo.AnyActiveGame(teamEntity, CancellationToken.None);
        Assert.True(hasActive);

        output.WriteLine($"Active game test passed for team {team.Id}");
    }

    [Fact]
    public async Task AnyActiveGame_ShouldUnlockTeam_WhenNoActiveGamesButLocked()
    {
        using var scope = factory.Services.CreateScope();
        var teamRepo = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        // Create locked team
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Locked Team");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var teamEntity = await context.Teams.FindAsync(team.Id);
        Assert.NotNull(teamEntity);

        // Lock the team
        teamEntity.Locked = true;
        await context.SaveChangesAsync();

        // Verify team is locked
        Assert.True(teamEntity.Locked);

        // Check active games (should unlock since no active games)
        var hasActive = await teamRepo.AnyActiveGame(teamEntity, CancellationToken.None);
        Assert.False(hasActive);

        // Verify team is now unlocked
        var updatedTeam = await context.Teams.FindAsync(team.Id);
        Assert.NotNull(updatedTeam);
        Assert.False(updatedTeam.Locked);

        output.WriteLine($"Team auto-unlock test passed for team {team.Id}");
    }

    [Fact]
    public async Task Transfer_ShouldChangeCaptain()
    {
        using var scope = factory.Services.CreateScope();
        var teamRepo = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        // Create team with original captain
        var captain1 = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, captain1.Id, "Transfer Team");

        // Create new captain
        var captain2 = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Add new captain to team
        var teamEntity = await context.Teams.FindAsync(team.Id);
        var user2 = await context.Users.FindAsync(captain2.Id);
        Assert.NotNull(teamEntity);
        Assert.NotNull(user2);

        teamEntity.Members.Add(user2);
        await context.SaveChangesAsync();

        // Verify original captain
        Assert.Equal(captain1.Id, teamEntity.CaptainId);

        // Transfer to new captain
        await teamRepo.Transfer(teamEntity, user2, CancellationToken.None);

        // Verify captain changed
        var updated = await teamRepo.GetTeamById(team.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(captain2.Id, updated.CaptainId);

        output.WriteLine($"Captain transfer test passed - Transferred from {captain1.Id} to {captain2.Id}");
    }

    [Fact]
    public async Task CheckIsCaptain_ShouldReturnTrue_ForCaptains()
    {
        using var scope = factory.Services.CreateScope();
        var teamRepo = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        // Create team
        var captain = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        await TestDataSeeder.CreateTeamAsync(factory.Services, captain.Id, "Captain Check Team");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userEntity = await context.Users.FindAsync(captain.Id);
        Assert.NotNull(userEntity);

        // Check is captain
        var isCaptain = await teamRepo.CheckIsCaptain(userEntity, CancellationToken.None);
        Assert.True(isCaptain);

        output.WriteLine($"Captain check test passed for user {captain.Id}");
    }

    [Fact]
    public async Task CheckIsCaptain_ShouldReturnFalse_ForNonCaptains()
    {
        using var scope = factory.Services.CreateScope();
        var teamRepo = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        // Create non-captain user
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userEntity = await context.Users.FindAsync(user.Id);
        Assert.NotNull(userEntity);

        // Check is not captain
        var isCaptain = await teamRepo.CheckIsCaptain(userEntity, CancellationToken.None);
        Assert.False(isCaptain);

        output.WriteLine($"Non-captain check test passed for user {user.Id}");
    }

    [Fact]
    public async Task VerifyToken_ShouldValidateInviteCode()
    {
        using var scope = factory.Services.CreateScope();
        var teamRepo = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        // Create team
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Token Team");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var teamEntity = await context.Teams.FindAsync(team.Id);
        Assert.NotNull(teamEntity);

        var inviteCode = teamEntity.InviteCode;

        // Valid token
        var validToken = await teamRepo.VerifyToken(team.Id, inviteCode, CancellationToken.None);
        Assert.True(validToken);

        // Invalid token
        var invalidToken = await teamRepo.VerifyToken(team.Id, "WRONG_CODE", CancellationToken.None);
        Assert.False(invalidToken);

        // Invalid team ID
        var invalidTeam = await teamRepo.VerifyToken(99999, inviteCode, CancellationToken.None);
        Assert.False(invalidTeam);

        output.WriteLine($"Token verification test passed for team {team.Id}");
    }
}
