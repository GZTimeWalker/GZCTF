using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Repositories;
using GZCTF.Repositories.Interface;
using GZCTF.Test.Infrastructure;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Test.UnitTests.Repositories;

public class TeamRepositoryTest : TestBase
{
    private readonly ITeamRepository _teamRepository;

    public TeamRepositoryTest(ITestOutputHelper output) : base(output)
    {
        _teamRepository = new TeamRepository(DbContext, Mock.Of<ILogger<TeamRepository>>());
    }

    [Fact]
    public async Task CreateTeam_ShouldAddTeamToDatabase()
    {
        // Arrange
        var captain = TestDataFactory.CreateUser("captain", "captain@test.com");
        DbContext.Users.Add(captain);
        await DbContext.SaveChangesAsync();

        var team = TestDataFactory.CreateTeam(captain, "Test Team");

        // Act
        var result = await _teamRepository.CreateAsync(team, default);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Name.Should().Be("Test Team");
        result.CaptainId.Should().Be(captain.Id);

        var savedTeam = await DbContext.Teams.FindAsync(result.Id);
        savedTeam.Should().NotBeNull();
        savedTeam!.Name.Should().Be("Test Team");
    }

    [Theory]
    [GzctfInlineAutoData("Team Alpha")]
    [GzctfInlineAutoData("Team Beta")]
    [GzctfInlineAutoData("Special Characters ! @ # $ %")]
    public async Task GetTeamByName_ShouldReturnCorrectTeam(string teamName)
    {
        // Arrange
        var captain = TestDataFactory.CreateUser();
        var team = TestDataFactory.CreateTeam(captain, teamName);
        DbContext.Users.Add(captain);
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _teamRepository.GetTeamByName(teamName, default);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(teamName);
        result.Id.Should().Be(team.Id);
    }

    [Fact]
    public async Task GetTeamByName_WithNonExistentName_ShouldReturnNull()
    {
        // Act
        var result = await _teamRepository.GetTeamByName("NonExistentTeam", default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTeamById_ShouldReturnTeamWithMembers()
    {
        // Arrange
        var captain = TestDataFactory.CreateUser("captain", "captain@test.com");
        var member = TestDataFactory.CreateUser("member", "member@test.com");
        var team = TestDataFactory.CreateTeam(captain, "Test Team");
        
        // Add members to team
        team.Members.Add(captain);
        team.Members.Add(member);

        DbContext.Users.AddRange(captain, member);
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _teamRepository.GetTeamById(team.Id, default);

        // Assert
        result.Should().NotBeNull();
        result!.Members.Should().HaveCount(2);
        result.Members.Should().Contain(u => u.Id == captain.Id);
        result.Members.Should().Contain(u => u.Id == member.Id);
    }

    [Fact]
    public async Task UpdateTeam_ShouldPersistChanges()
    {
        // Arrange
        var captain = TestDataFactory.CreateUser();
        var team = TestDataFactory.CreateTeam(captain, "Original Name");
        DbContext.Users.Add(captain);
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        // Act
        team.Name = "Updated Name";
        team.Bio = "Updated bio";
        await _teamRepository.UpdateAsync(team, default);

        // Assert
        var updatedTeam = await DbContext.Teams.FindAsync(team.Id);
        updatedTeam.Should().NotBeNull();
        updatedTeam!.Name.Should().Be("Updated Name");
        updatedTeam.Bio.Should().Be("Updated bio");
    }

    [Fact]
    public async Task DeleteTeam_ShouldRemoveFromDatabase()
    {
        // Arrange
        var captain = TestDataFactory.CreateUser();
        var team = TestDataFactory.CreateTeam(captain, "Team to Delete");
        DbContext.Users.Add(captain);
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        // Act
        await _teamRepository.DeleteAsync(team, default);

        // Assert
        var deletedTeam = await DbContext.Teams.FindAsync(team.Id);
        deletedTeam.Should().BeNull();
    }

    [Fact]
    public async Task GetTeamsByUserId_ShouldReturnUserTeams()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var team1 = TestDataFactory.CreateTeam(user, "Team 1");
        var team2 = TestDataFactory.CreateTeam(null, "Team 2");
        team2.Members.Add(user);

        DbContext.Users.Add(user);
        DbContext.Teams.AddRange(team1, team2);
        await DbContext.SaveChangesAsync();

        // Act
        var teams = await _teamRepository.GetUserTeams(user.Id, default);

        // Assert
        teams.Should().HaveCount(2);
        teams.Should().Contain(t => t.Name == "Team 1");
        teams.Should().Contain(t => t.Name == "Team 2");
    }

    [Theory]
    [GzctfInlineAutoData(0, 10)]
    [GzctfInlineAutoData(1, 5)]
    [GzctfInlineAutoData(2, 3)]
    public async Task GetTeams_WithPagination_ShouldReturnCorrectPage(int skip, int take)
    {
        // Arrange
        var teams = new List<Team>();
        for (int i = 0; i < 15; i++)
        {
            var captain = TestDataFactory.CreateUser($"captain{i}", $"captain{i}@test.com");
            var team = TestDataFactory.CreateTeam(captain, $"Team {i:D2}");
            teams.Add(team);
            DbContext.Users.Add(captain);
        }
        DbContext.Teams.AddRange(teams);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await DbContext.Teams
            .OrderBy(t => t.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(Math.Min(take, 15 - skip));
        
        if (result.Any())
        {
            var expectedFirstTeamIndex = skip;
            if (expectedFirstTeamIndex < 15)
            {
                result.First().Name.Should().Be($"Team {expectedFirstTeamIndex:D2}");
            }
        }
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddScoped<ITeamRepository, TeamRepository>();
    }
}