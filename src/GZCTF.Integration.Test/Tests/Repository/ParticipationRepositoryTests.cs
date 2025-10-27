using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Repository;

/// <summary>
/// Integration tests for ParticipationRepository focusing on data calculations and complex operations
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class ParticipationRepositoryTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task EnsureInstances_ShouldCreateNewInstances_ForNewChallenges()
    {
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var teamRepo = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        // Create game
        var game = new Game
        {
            Title = "Instance Test Game",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create team and participation
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Test Team");

        var participation = new Participation
        {
            GameId = game.Id,
            TeamId = team.Id,
            Status = ParticipationStatus.Accepted
        };

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Participations.AddAsync(participation);
        await context.SaveChangesAsync();

        // Initially no instances
        var instancesBefore = await EnsureInstances(participationRepo, participation, game);
        Assert.False(instancesBefore); // No challenges yet

        // Add a challenge
        var challenge = new GameChallenge
        {
            Title = "Test Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // EnsureInstances should create new instance
        var instancesCreated = await EnsureInstances(participationRepo, participation, game);
        Assert.True(instancesCreated);

        // Verify instance exists
        var instances = context.Set<GameInstance>()
            .Where(gi => gi.ParticipationId == participation.Id && gi.ChallengeId == challenge.Id)
            .ToList();
        Assert.Single(instances);

        // Running again should not create duplicates
        var noDuplicates = await EnsureInstances(participationRepo, participation, game);
        Assert.False(noDuplicates);

        output.WriteLine($"Successfully created and verified instances for participation {participation.Id}");
    }

    [Fact]
    public async Task UpdateParticipationStatus_ShouldLockTeam_WhenAccepted()
    {
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var teamRepo = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        // Create game
        var game = new Game
        {
            Title = "Lock Test Game",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create team and participation
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Lock Team");

        // Verify team is not locked initially
        var teamBefore = await teamRepo.GetTeamById(team.Id, CancellationToken.None);
        Assert.NotNull(teamBefore);
        Assert.False(teamBefore.Locked);

        var participation = new Participation
        {
            GameId = game.Id,
            TeamId = team.Id,
            Status = ParticipationStatus.Pending
        };

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Participations.AddAsync(participation);
        await context.SaveChangesAsync();

        // Accept participation
        await participationRepo.UpdateParticipationStatus(participation, ParticipationStatus.Accepted,
            CancellationToken.None);

        // Verify team is now locked
        var teamAfter = await teamRepo.GetTeamById(team.Id, CancellationToken.None);
        Assert.NotNull(teamAfter);
        Assert.True(teamAfter.Locked);

        output.WriteLine($"Team {team.Id} successfully locked after participation acceptance");
    }

    [Fact]
    public async Task GetParticipationCount_ShouldReturnCorrectCount()
    {
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "Count Test Game",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Initial count should be 0
        var initialCount = await participationRepo.GetParticipationCount(game.Id, CancellationToken.None);
        Assert.Equal(0, initialCount);

        // Create multiple participations
        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        for (int i = 0; i < 5; i++)
        {
            var user = await TestDataSeeder.CreateUserAsync(factory.Services,
                TestDataSeeder.RandomName(), "Test@123");
            var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Count Team {i}");

            var participation = new Participation
            {
                GameId = game.Id,
                TeamId = team.Id,
                Status = ParticipationStatus.Accepted
            };

            await context.Participations.AddAsync(participation);
        }

        await context.SaveChangesAsync();

        // Count should now be 5
        var finalCount = await participationRepo.GetParticipationCount(game.Id, CancellationToken.None);
        Assert.Equal(5, finalCount);

        output.WriteLine($"Participation count verified: {finalCount}");
    }

    [Fact]
    public async Task CheckRepeatParticipation_ShouldDetectDuplicates()
    {
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "Repeat Test Game",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create user and team
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Repeat Team");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userInfo = await context.Users.FindAsync(user.Id);
        Assert.NotNull(userInfo);
        var gameEntity = await context.Games.FindAsync(game.Id);
        Assert.NotNull(gameEntity);

        // Initially no participation
        var noDuplicate =
            await participationRepo.CheckRepeatParticipation(userInfo, gameEntity, CancellationToken.None);
        Assert.False(noDuplicate);

        // Create participation
        var participation = new Participation
        {
            GameId = game.Id,
            TeamId = team.Id,
            Status = ParticipationStatus.Accepted
        };

        await context.Participations.AddAsync(participation);
        await context.SaveChangesAsync(); // Save to get the participation ID

        // Create UserParticipation link
        var userParticipation = new UserParticipation
        {
            GameId = game.Id,
            TeamId = team.Id,
            UserId = user.Id,
            ParticipationId = participation.Id
        };

        await context.UserParticipations.AddAsync(userParticipation);
        await context.SaveChangesAsync();

        // Should detect repeat participation
        var hasRepeat = await participationRepo.CheckRepeatParticipation(userInfo, gameEntity, CancellationToken.None);
        Assert.True(hasRepeat);

        output.WriteLine($"Repeat participation detection working correctly");
    }

    [Fact]
    public async Task UpdateParticipation_ShouldUpdateDivision()
    {
        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var divisionRepo = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();

        // Create game
        var game = new Game
        {
            Title = "Division Update Test",
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

        var division2 = await divisionRepo.CreateDivision(game,
            new DivisionCreateModel
            {
                Name = "Division 2",
                InviteCode = "DIV2",
                DefaultPermissions = GamePermission.All
            }, CancellationToken.None);

        // Create participation
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Division Team");

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

        // Verify initial division
        Assert.Equal(division1.Id, participation.DivisionId);

        // Update to division 2
        await participationRepo.UpdateParticipation(participation,
            new ParticipationEditModel { DivisionId = division2.Id }, CancellationToken.None);

        // Verify division changed
        var updated = await participationRepo.GetParticipationById(participation.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(division2.Id, updated.DivisionId);

        output.WriteLine($"Division updated from {division1.Id} to {division2.Id}");
    }

    private async Task<bool> EnsureInstances(IParticipationRepository repo, Participation part, Game game)
    {
        // Need to reload participation with fresh context to avoid tracking issues
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var freshPart = await context.Participations.FindAsync(part.Id);
        var freshGame = await context.Games.FindAsync(game.Id);

        if (freshPart == null || freshGame == null)
            return false;

        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        return await participationRepo.EnsureInstances(freshPart, freshGame, CancellationToken.None);
    }
}
