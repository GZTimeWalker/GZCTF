using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Database;

/// <summary>
/// Tests for database context operations using real PostgresSQL
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class DatabaseContextTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task DbContext_ShouldAllowUserCRUDOperations()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new UserInfo
        {
            UserName = TestDataSeeder.RandomName(),
            Email = $"test_{Guid.NewGuid():N}@test.com",
            Role = Role.User,
            EmailConfirmed = true,
            RegisterTimeUtc = DateTimeOffset.UtcNow
        };

        // Act - Create
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        output.WriteLine($"Created user with ID: {user.Id}");

        // Act - Read
        var savedUser = await dbContext.Users.FindAsync(user.Id);

        // Assert
        Assert.NotNull(savedUser);
        Assert.Equal(user.UserName, savedUser.UserName);
        Assert.Equal(user.Email, savedUser.Email);

        // Cleanup
        dbContext.Users.Remove(savedUser);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task DbContext_ShouldHandleTeamOperations()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var captain = new UserInfo
        {
            UserName = TestDataSeeder.RandomName(),
            Email = $"captain_{Guid.NewGuid():N}@test.com",
            Role = Role.User,
            EmailConfirmed = true,
            RegisterTimeUtc = DateTimeOffset.UtcNow
        };

        var team = new Team
        {
            Name = TestDataSeeder.RandomName(),
            Captain = captain,
            Bio = "Test team bio",
            Locked = false
        };

        // Act
        dbContext.Users.Add(captain);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();
        output.WriteLine($"Created team with ID: {team.Id}");

        // Assert
        var savedTeam = await dbContext.Teams
            .Include(t => t.Captain)
            .FirstOrDefaultAsync(t => t.Id == team.Id);

        Assert.NotNull(savedTeam);
        Assert.Equal(team.Name, savedTeam.Name);
        Assert.NotNull(savedTeam.Captain);
        Assert.Equal(captain.UserName, savedTeam.Captain.UserName);

        // Cleanup
        dbContext.Teams.Remove(savedTeam);
        dbContext.Users.Remove(captain);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task DbContext_ShouldHandleGameOperations()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;
        var game = new Game
        {
            Title = TestDataSeeder.RandomName(),
            StartTimeUtc = now.AddDays(1),
            EndTimeUtc = now.AddDays(2),
            Hidden = false,
            PracticeMode = true
        };

        // Act
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync();
        output.WriteLine($"Created game with ID: {game.Id}");

        // Assert
        var savedGame = await dbContext.Games.FindAsync(game.Id);
        Assert.NotNull(savedGame);
        Assert.Equal(game.Title, savedGame.Title);

        // Cleanup
        dbContext.Games.Remove(savedGame);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task DbContext_ShouldHandleParticipationRelationships()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var captain = new UserInfo
        {
            UserName = TestDataSeeder.RandomName(),
            Email = $"captain_{Guid.NewGuid():N}@test.com",
            Role = Role.User,
            EmailConfirmed = true,
            RegisterTimeUtc = DateTimeOffset.UtcNow
        };

        var team = new Team { Name = TestDataSeeder.RandomName(), Captain = captain, Locked = false };

        var now = DateTimeOffset.UtcNow;
        var game = new Game
        {
            Title = TestDataSeeder.RandomName(),
            StartTimeUtc = now.AddDays(1),
            EndTimeUtc = now.AddDays(2),
            Hidden = false,
            PracticeMode = true
        };

        var participation = new Participation { Team = team, Game = game, Status = ParticipationStatus.Accepted };

        // Act
        dbContext.Users.Add(captain);
        dbContext.Teams.Add(team);
        dbContext.Games.Add(game);
        dbContext.Participations.Add(participation);
        await dbContext.SaveChangesAsync();
        output.WriteLine($"Created participation with Team ID: {team.Id}, Game ID: {game.Id}");

        // Assert
        var savedParticipation = await dbContext.Participations
            .Include(p => p.Team)
            .Include(p => p.Game)
            .FirstOrDefaultAsync(p => p.Id == participation.Id);

        Assert.NotNull(savedParticipation);
        Assert.NotNull(savedParticipation.Team);
        Assert.NotNull(savedParticipation.Game);
        Assert.Equal(team.Name, savedParticipation.Team.Name);
        Assert.Equal(game.Title, savedParticipation.Game.Title);

        // Cleanup
        dbContext.Participations.Remove(savedParticipation);
        dbContext.Teams.Remove(team);
        dbContext.Games.Remove(game);
        dbContext.Users.Remove(captain);
        await dbContext.SaveChangesAsync();
    }
}
