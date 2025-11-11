using GZCTF.Integration.Test.Base;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Repository;

/// <summary>
/// Integration tests for DivisionRepository focusing on permission calculations and challenge configs
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class DivisionRepositoryTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task GetPermission_ShouldReturnDefaultPermissions_WhenNoDivision()
    {
        using var scope = factory.Services.CreateScope();
        var divisionRepo = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();

        // No division means all permissions
        var permission = await divisionRepo.GetPermission(null, null, CancellationToken.None);
        Assert.Equal(GamePermission.All, permission);

        output.WriteLine($"Default permission verified: {permission}");
    }

    [Fact]
    public async Task GetPermission_ShouldReturnDefaultPermissions_WhenNoChallengeConfig()
    {
        using var scope = factory.Services.CreateScope();
        var divisionRepo = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "Permission Default Test",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create division with specific default permissions
        var defaultPerms = GamePermission.JoinGame | GamePermission.ViewChallenge | GamePermission.SubmitFlags;
        var division = await divisionRepo.CreateDivision(game,
            new DivisionCreateModel
            {
                Name = "Default Permission Division",
                InviteCode = "DEFPERM",
                DefaultPermissions = defaultPerms
            }, CancellationToken.None);

        // Get permission without challenge ID should return default
        var permission = await divisionRepo.GetPermission(division.Id, null, CancellationToken.None);
        Assert.Equal(defaultPerms, permission);

        // Get permission for non-existent challenge should return default
        var permissionNonExistent = await divisionRepo.GetPermission(division.Id, 9999, CancellationToken.None);
        Assert.Equal(defaultPerms, permissionNonExistent);

        output.WriteLine($"Default permission test passed: {permission}");
    }

    [Fact]
    public async Task GetPermission_ShouldReturnChallengeSpecificPermissions_WhenConfigExists()
    {
        using var scope = factory.Services.CreateScope();
        var divisionRepo = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

        // Create game
        var game = new Game
        {
            Title = "Challenge Permission Test",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create challenge
        var challenge = new GameChallenge
        {
            Title = "Permission Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Create division with default permissions and challenge-specific config
        var defaultPerms = GamePermission.All;
        var challengePerms = GamePermission.ViewChallenge | GamePermission.SubmitFlags; // No GetScore

        var division = await divisionRepo.CreateDivision(game, new DivisionCreateModel
        {
            Name = "Challenge Config Division",
            InviteCode = "CHGCFG",
            DefaultPermissions = defaultPerms,
            ChallengeConfigs =
            [
                new DivisionChallengeConfigModel { ChallengeId = challenge.Id, Permissions = challengePerms }
            ]
        }, CancellationToken.None);

        // Get permission for configured challenge should return challenge-specific permissions
        var permission = await divisionRepo.GetPermission(division.Id, challenge.Id, CancellationToken.None);
        Assert.Equal(challengePerms, permission);

        // Verify GetScore is not included
        Assert.False(permission.HasFlag(GamePermission.GetScore));
        Assert.True(permission.HasFlag(GamePermission.ViewChallenge));
        Assert.True(permission.HasFlag(GamePermission.SubmitFlags));

        output.WriteLine($"Challenge-specific permission test passed: {permission}");
    }

    [Fact]
    public async Task GetJoinableDivisionIds_ShouldReturnOnlyJoinable()
    {
        using var scope = factory.Services.CreateScope();
        var divisionRepo = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "Joinable Test",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create joinable division
        var joinable1 = await divisionRepo.CreateDivision(game, new DivisionCreateModel
        {
            Name = "Joinable 1",
            InviteCode = "JOIN1",
            DefaultPermissions = GamePermission.All // Includes JoinGame
        }, CancellationToken.None);

        var joinable2 = await divisionRepo.CreateDivision(game,
            new DivisionCreateModel
            {
                Name = "Joinable 2",
                InviteCode = "JOIN2",
                DefaultPermissions = GamePermission.JoinGame | GamePermission.ViewChallenge
            }, CancellationToken.None);

        // Create non-joinable division
        var notJoinable = await divisionRepo.CreateDivision(game, new DivisionCreateModel
        {
            Name = "Not Joinable",
            InviteCode = "NOJOIN",
            DefaultPermissions = GamePermission.ViewChallenge | GamePermission.SubmitFlags // No JoinGame
        }, CancellationToken.None);

        // Get joinable divisions
        var joinableIds = await divisionRepo.GetJoinableDivisionIds(game.Id, CancellationToken.None);

        Assert.Equal(2, joinableIds.Count);
        Assert.Contains(joinable1.Id, joinableIds);
        Assert.Contains(joinable2.Id, joinableIds);
        Assert.DoesNotContain(notJoinable.Id, joinableIds);

        output.WriteLine($"Joinable divisions test passed - Found {joinableIds.Count} joinable divisions");
    }

    [Fact]
    public async Task CreateDivision_ShouldPersistChallengeConfigs()
    {
        using var scope = factory.Services.CreateScope();
        var divisionRepo = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

        // Create game
        var game = new Game
        {
            Title = "Config Persist Test",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create challenges
        var challenge1 = new GameChallenge
        {
            Title = "Challenge 1",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        var challenge2 = new GameChallenge
        {
            Title = "Challenge 2",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 200,
            MinScoreRate = 1.0,
            Difficulty = 2
        };

        await challengeRepo.CreateChallenge(game, challenge1, CancellationToken.None);
        await challengeRepo.CreateChallenge(game, challenge2, CancellationToken.None);

        // Create division with configs
        var division = await divisionRepo.CreateDivision(game, new DivisionCreateModel
        {
            Name = "Config Division",
            InviteCode = "CONFIG",
            DefaultPermissions = GamePermission.All,
            ChallengeConfigs =
            [
                new DivisionChallengeConfigModel
                {
                    ChallengeId = challenge1.Id, Permissions = GamePermission.ViewChallenge
                },
                new DivisionChallengeConfigModel
                {
                    ChallengeId = challenge2.Id,
                    Permissions = GamePermission.ViewChallenge | GamePermission.SubmitFlags
                }
            ]
        }, CancellationToken.None);

        // Retrieve division and verify configs
        var retrieved = await divisionRepo.GetDivision(game.Id, division.Id, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.ChallengeConfigs.Count);

        var config1 = retrieved.ChallengeConfigs.FirstOrDefault(c => c.ChallengeId == challenge1.Id);
        var config2 = retrieved.ChallengeConfigs.FirstOrDefault(c => c.ChallengeId == challenge2.Id);

        Assert.NotNull(config1);
        Assert.NotNull(config2);
        Assert.Equal(GamePermission.ViewChallenge, config1.Permissions);
        Assert.Equal(GamePermission.ViewChallenge | GamePermission.SubmitFlags, config2.Permissions);

        output.WriteLine($"Challenge configs persisted correctly - Found {retrieved.ChallengeConfigs.Count} configs");
    }

    [Fact]
    public async Task UpdateDivision_ShouldUpdateChallengeConfigs()
    {
        using var scope = factory.Services.CreateScope();
        var divisionRepo = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

        // Create game
        var game = new Game
        {
            Title = "Update Config Test",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create challenge
        var challenge = new GameChallenge
        {
            Title = "Update Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Create division with initial config
        var division = await divisionRepo.CreateDivision(game, new DivisionCreateModel
        {
            Name = "Update Division",
            InviteCode = "UPDATE",
            DefaultPermissions = GamePermission.All,
            ChallengeConfigs =
            [
                new DivisionChallengeConfigModel
                {
                    ChallengeId = challenge.Id, Permissions = GamePermission.ViewChallenge
                }
            ]
        }, CancellationToken.None);

        // Verify initial config
        var initialPerm = await divisionRepo.GetPermission(division.Id, challenge.Id, CancellationToken.None);
        Assert.Equal(GamePermission.ViewChallenge, initialPerm);

        // Update division with new config
        await divisionRepo.UpdateDivision(division, new DivisionEditModel
        {
            ChallengeConfigs =
            [
                new DivisionChallengeConfigModel
                {
                    ChallengeId = challenge.Id,
                    Permissions = GamePermission.ViewChallenge | GamePermission.SubmitFlags |
                                  GamePermission.GetScore
                }
            ]
        }, CancellationToken.None);

        // Verify updated config
        var updatedPerm = await divisionRepo.GetPermission(division.Id, challenge.Id, CancellationToken.None);
        Assert.Equal(GamePermission.ViewChallenge | GamePermission.SubmitFlags | GamePermission.GetScore, updatedPerm);

        output.WriteLine($"Challenge config update test passed - Before: {initialPerm}, After: {updatedPerm}");
    }

    [Fact]
    public async Task RemoveDivision_ShouldDeleteSuccessfully()
    {
        using var scope = factory.Services.CreateScope();
        var divisionRepo = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "Remove Division Test",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create division
        var division = await divisionRepo.CreateDivision(game,
            new DivisionCreateModel
            {
                Name = "To Be Removed",
                InviteCode = "REMOVE",
                DefaultPermissions = GamePermission.All
            }, CancellationToken.None);

        var divisionId = division.Id;

        // Verify division exists
        var exists = await divisionRepo.GetDivision(game.Id, divisionId, CancellationToken.None);
        Assert.NotNull(exists);

        // Remove division
        await divisionRepo.RemoveDivision(division, CancellationToken.None);

        // Verify division no longer exists
        var removed = await divisionRepo.GetDivision(game.Id, divisionId, CancellationToken.None);
        Assert.Null(removed);

        output.WriteLine($"Division {divisionId} successfully removed");
    }
}
