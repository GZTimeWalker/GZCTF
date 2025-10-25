using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Repository;

/// <summary>
/// Integration tests for GameRepository focusing on calculations and data operations
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class GameRepositoryTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task GenRecentGames_ShouldOrderCorrectly()
    {
        using var scope = factory.Services.CreateScope();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        var now = DateTimeOffset.UtcNow;

        // Create ongoing game (should be first)
        var ongoingGame = new Game
        {
            Title = "Ongoing Game",
            Summary = "Test",
            Content = "Test",
            Hidden = false,
            StartTimeUtc = now.AddHours(-2),
            EndTimeUtc = now.AddHours(2),
            AcceptWithoutReview = true
        };

        // Create upcoming game (should be second)
        var upcomingGame = new Game
        {
            Title = "Upcoming Game",
            Summary = "Test",
            Content = "Test",
            Hidden = false,
            StartTimeUtc = now.AddHours(1),
            EndTimeUtc = now.AddHours(5),
            AcceptWithoutReview = true
        };

        // Create ended game (should be last)
        var endedGame = new Game
        {
            Title = "Ended Game",
            Summary = "Test",
            Content = "Test",
            Hidden = false,
            StartTimeUtc = now.AddHours(-10),
            EndTimeUtc = now.AddHours(-2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(ongoingGame, CancellationToken.None);
        await gameRepo.CreateGame(upcomingGame, CancellationToken.None);
        await gameRepo.CreateGame(endedGame, CancellationToken.None);

        // Get recent games
        var recentGames = await gameRepo.GenRecentGames(CancellationToken.None);

        Assert.NotEmpty(recentGames);

        // Find our games in the results
        var ongoing = recentGames.FirstOrDefault(g => g.Id == ongoingGame.Id);
        var upcoming = recentGames.FirstOrDefault(g => g.Id == upcomingGame.Id);
        var ended = recentGames.FirstOrDefault(g => g.Id == endedGame.Id);

        Assert.NotNull(ongoing);
        Assert.NotNull(upcoming);
        Assert.NotNull(ended);

        var ongoingIndex = Array.IndexOf(recentGames.Select(g => g.Id).ToArray(), ongoingGame.Id);
        var upcomingIndex = Array.IndexOf(recentGames.Select(g => g.Id).ToArray(), upcomingGame.Id);
        var endedIndex = Array.IndexOf(recentGames.Select(g => g.Id).ToArray(), endedGame.Id);

        output.WriteLine($"Game indices - Ongoing: {ongoingIndex}, Upcoming: {upcomingIndex}, Ended: {endedIndex}");
        output.WriteLine($"Total games in recent list: {recentGames.Length}");

        // Verify that ongoing comes before ended (ongoing games should be prioritized over ended)
        Assert.True(ongoingIndex < endedIndex,
            $"Ongoing game should come before ended: ongoing={ongoingIndex}, ended={endedIndex}");

        // Verify that upcoming comes before ended (upcoming games should be prioritized over ended)
        Assert.True(upcomingIndex < endedIndex,
            $"Upcoming game should come before ended: upcoming={upcomingIndex}, ended={endedIndex}");

        output.WriteLine(
            $"Recent games ordering test passed - Ongoing: {ongoingIndex}, Upcoming: {upcomingIndex}, Ended: {endedIndex}");
    }

    [Fact]
    public async Task GetToken_ShouldGenerateValidToken()
    {
        using var scope = factory.Services.CreateScope();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "Token Test Game",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create team
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), $"gametoken{Guid.NewGuid():N}@test.com", "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Token Team");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gameEntity = await context.Games.FindAsync(game.Id);
        var teamEntity = await context.Teams.FindAsync(team.Id);

        Assert.NotNull(gameEntity);
        Assert.NotNull(teamEntity);

        // Generate token
        var token = gameRepo.GetToken(gameEntity, teamEntity);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(":", token); // Token format is "teamId:signature"

        var parts = token.Split(':');
        Assert.Equal(2, parts.Length);
        Assert.Equal(team.Id.ToString(), parts[0]);

        output.WriteLine($"Token generation test passed - Token: {token}");
    }

    [Fact]
    public async Task GetUpcomingGames_ShouldReturnGamesStartingSoon()
    {
        using var scope = factory.Services.CreateScope();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        var now = DateTimeOffset.UtcNow;

        // Create game starting in 10 minutes (should be included)
        var soonGame = new Game
        {
            Title = "Starting Soon Game",
            Summary = "Test",
            Content = "Test",
            Hidden = false,
            StartTimeUtc = now.AddMinutes(10),
            EndTimeUtc = now.AddHours(2),
            AcceptWithoutReview = true
        };

        // Create game starting in 30 minutes (should not be included - outside 15 min window)
        var laterGame = new Game
        {
            Title = "Starting Later Game",
            Summary = "Test",
            Content = "Test",
            Hidden = false,
            StartTimeUtc = now.AddMinutes(30),
            EndTimeUtc = now.AddHours(2),
            AcceptWithoutReview = true
        };

        // Create game already started (should not be included)
        var startedGame = new Game
        {
            Title = "Already Started Game",
            Summary = "Test",
            Content = "Test",
            Hidden = false,
            StartTimeUtc = now.AddMinutes(-5),
            EndTimeUtc = now.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(soonGame, CancellationToken.None);
        await gameRepo.CreateGame(laterGame, CancellationToken.None);
        await gameRepo.CreateGame(startedGame, CancellationToken.None);

        // Get upcoming games
        var upcomingGames = await gameRepo.GetUpcomingGames(CancellationToken.None);

        Assert.Contains(soonGame.Id, upcomingGames);
        Assert.DoesNotContain(laterGame.Id, upcomingGames);
        Assert.DoesNotContain(startedGame.Id, upcomingGames);

        output.WriteLine($"Upcoming games test passed - Found {upcomingGames.Length} upcoming games");
    }

    [Fact]
    public async Task GetCheckInfo_ShouldReturnJoinedTeamsAndDivisions()
    {
        using var scope = factory.Services.CreateScope();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var divisionRepo = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();

        // Create game
        var game = new Game
        {
            Title = "Check Info Test Game",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create divisions
        var division1 = await divisionRepo.CreateDivision(game,
            new DivisionCreateModel
            {
                Name = "Division 1",
                InviteCode = "DIV1",
                DefaultPermissions = GamePermission.All
            }, CancellationToken.None);

        var division2 = await divisionRepo.CreateDivision(game, new DivisionCreateModel
        {
            Name = "Division 2",
            InviteCode = "DIV2",
            DefaultPermissions = GamePermission.JoinGame // Joinable
        }, CancellationToken.None);

        await divisionRepo.CreateDivision(game, new DivisionCreateModel
        {
            Name = "Division 3",
            InviteCode = "DIV3",
            DefaultPermissions = GamePermission.ViewChallenge // Not joinable
        }, CancellationToken.None);

        // Create user and team
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "checkinfo@test.com", "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Check Info Team");

        // Create participation
        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = new Participation
        {
            GameId = game.Id,
            TeamId = team.Id,
            DivisionId = division1.Id,
            Status = ParticipationStatus.Accepted
        };

        await context.Participations.AddAsync(participation);
        await context.SaveChangesAsync();

        var userEntity = await context.Users.FindAsync(user.Id);
        var gameEntity = await context.Games.FindAsync(game.Id);

        Assert.NotNull(userEntity);
        Assert.NotNull(gameEntity);

        // Get check info
        var checkInfo = await gameRepo.GetCheckInfo(gameEntity, userEntity, CancellationToken.None);

        Assert.NotNull(checkInfo);
        Assert.NotNull(checkInfo.JoinedTeams);
        Assert.NotNull(checkInfo.JoinableDivisions);

        // Should have joined team
        Assert.Single(checkInfo.JoinedTeams);
        Assert.Equal(team.Id, checkInfo.JoinedTeams[0].TeamId);
        Assert.Equal(division1.Id, checkInfo.JoinedTeams[0].DivisionId);

        // Should have 2 joinable divisions (division1 and division2)
        Assert.Equal(2, checkInfo.JoinableDivisions.Length);
        Assert.Contains(division1.Id, checkInfo.JoinableDivisions);
        Assert.Contains(division2.Id, checkInfo.JoinableDivisions);

        output.WriteLine(
            $"Check info test passed - Joined teams: {checkInfo.JoinedTeams.Length}, Joinable divisions: {checkInfo.JoinableDivisions.Length}");
    }

    [Fact]
    public async Task HasGameAsync_ShouldReturnCorrectValue()
    {
        using var scope = factory.Services.CreateScope();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "Has Game Test",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Check game exists
        var exists = await gameRepo.HasGameAsync(game.Id, CancellationToken.None);
        Assert.True(exists);

        // Check non-existent game
        var notExists = await gameRepo.HasGameAsync(99999, CancellationToken.None);
        Assert.False(notExists);

        output.WriteLine($"HasGameAsync test passed - Game {game.Id} exists: {exists}");
    }
}
