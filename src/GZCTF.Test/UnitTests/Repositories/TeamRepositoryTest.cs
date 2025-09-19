using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Test.Infrastructure;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Test.UnitTests.Repositories;

public class DatabaseContextTest : TestBase
{
    public DatabaseContextTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task DbContext_ShouldAllowCRUDOperations()
    {
        // Arrange
        var user = TestDataFactory.CreateUser("testuser", "test@test.com");
        
        // Act - Create
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        // Act - Read
        var savedUser = await DbContext.Users.FindAsync(user.Id);

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.UserName.Should().Be("testuser");
        savedUser.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task DbContext_ShouldHandleTeamOperations()
    {
        // Arrange
        var captain = TestDataFactory.CreateUser("captain", "captain@test.com");
        var team = TestDataFactory.CreateTeam(captain, "Test Team");

        // Act
        DbContext.Users.Add(captain);
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        // Assert
        var savedTeam = await DbContext.Teams
            .Include(t => t.Captain)
            .FirstOrDefaultAsync(t => t.Id == team.Id);

        savedTeam.Should().NotBeNull();
        savedTeam!.Name.Should().Be("Test Team");
        savedTeam.Captain.Should().NotBeNull();
        savedTeam.Captain!.UserName.Should().Be("captain");
    }

    [Fact]
    public async Task DbContext_ShouldHandleGameOperations()
    {
        // Arrange
        var game = TestDataFactory.CreateGame("Test Game");

        // Act
        DbContext.Games.Add(game);
        await DbContext.SaveChangesAsync();

        // Assert
        var savedGame = await DbContext.Games.FindAsync(game.Id);
        savedGame.Should().NotBeNull();
        savedGame!.Title.Should().Be("Test Game");
    }

    [Fact]
    public async Task DbContext_ShouldHandleRelationships()
    {
        // Arrange
        var captain = TestDataFactory.CreateUser("captain", "captain@test.com");
        var member = TestDataFactory.CreateUser("member", "member@test.com");
        var team = TestDataFactory.CreateTeam(captain, "Test Team");
        var game = TestDataFactory.CreateGame("Test Game");
        var participation = TestDataFactory.CreateParticipation(team, game);

        // Act
        DbContext.Users.AddRange(captain, member);
        DbContext.Teams.Add(team);
        DbContext.Games.Add(game);
        DbContext.Participations.Add(participation);
        await DbContext.SaveChangesAsync();

        // Assert
        var savedParticipation = await DbContext.Participations
            .Include(p => p.Team)
            .Include(p => p.Game)
            .FirstOrDefaultAsync(p => p.Id == participation.Id);

        savedParticipation.Should().NotBeNull();
        savedParticipation!.Team.Name.Should().Be("Test Team");
        savedParticipation.Game.Title.Should().Be("Test Game");
    }

    [Fact]
    public async Task DbContext_ShouldHandleQueryOperations()
    {
        // Arrange
        await TestDataFactory.SeedDatabaseAsync(DbContext);

        // Act
        var userCount = await DbContext.Users.CountAsync();
        var teamCount = await DbContext.Teams.CountAsync();
        var gameCount = await DbContext.Games.CountAsync();

        // Assert
        userCount.Should().BeGreaterThan(0);
        teamCount.Should().BeGreaterThan(0);
        gameCount.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("user")]
    public async Task DbContext_ShouldFindUserByName(string username)
    {
        // Arrange
        await TestDataFactory.SeedDatabaseAsync(DbContext);

        // Act
        var user = await DbContext.Users
            .FirstOrDefaultAsync(u => u.UserName == username);

        // Assert
        user.Should().NotBeNull();
        user!.UserName.Should().Be(username);
    }
}