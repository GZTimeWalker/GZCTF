using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using TaskStatus = GZCTF.Utils.TaskStatus;

namespace GZCTF.Integration.Test.Tests.Repository;

/// <summary>
/// Integration tests for GameChallengeRepository focusing on flag management and challenge operations
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class GameChallengeRepositoryTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task AddFlags_ShouldAddMultipleFlags()
    {
        using var scope = factory.Services.CreateScope();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "Flag Add Test Game",
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
            Title = "Multi-Flag Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Add multiple flags
        var flagModels = new[]
        {
            new FlagCreateModel { Flag = "flag{test1}", AttachmentType = FileType.None },
            new FlagCreateModel { Flag = "flag{test2}", AttachmentType = FileType.None },
            new FlagCreateModel { Flag = "flag{test3}", AttachmentType = FileType.None }
        };

        await challengeRepo.AddFlags(challenge, flagModels, CancellationToken.None);

        // Verify flags were added
        await challengeRepo.LoadFlags(challenge, CancellationToken.None);

        Assert.Equal(3, challenge.Flags.Count);
        Assert.Contains(challenge.Flags, f => f.Flag == "flag{test1}");
        Assert.Contains(challenge.Flags, f => f.Flag == "flag{test2}");
        Assert.Contains(challenge.Flags, f => f.Flag == "flag{test3}");

        output.WriteLine($"Successfully added {challenge.Flags.Count} flags to challenge {challenge.Id}");
    }

    [Fact]
    public async Task RemoveFlag_ShouldRemoveSingleFlag()
    {
        using var scope = factory.Services.CreateScope();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "Flag Remove Test Game",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create challenge with flags
        var challenge = new GameChallenge
        {
            Title = "Flag Remove Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        var flag1 = new FlagContext { Flag = "flag{keep}", Challenge = challenge };
        var flag2 = new FlagContext { Flag = "flag{remove}", Challenge = challenge };

        challenge.Flags.Add(flag1);
        challenge.Flags.Add(flag2);

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Verify initial state
        await challengeRepo.LoadFlags(challenge, CancellationToken.None);
        Assert.Equal(2, challenge.Flags.Count);

        // Remove one flag
        var flagToRemove = challenge.Flags.First(f => f.Flag == "flag{remove}");
        var result = await challengeRepo.RemoveFlag(challenge, flagToRemove.Id, CancellationToken.None);

        Assert.Equal(TaskStatus.Success, result);

        // Verify flag was removed
        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedChallenge = await context.GameChallenges.FindAsync(challenge.Id);
        Assert.NotNull(updatedChallenge);
        await challengeRepo.LoadFlags(updatedChallenge, CancellationToken.None);

        Assert.Single(updatedChallenge.Flags);
        Assert.Contains(updatedChallenge.Flags, f => f.Flag == "flag{keep}");
        Assert.DoesNotContain(updatedChallenge.Flags, f => f.Flag == "flag{remove}");

        // Challenge should still be enabled since there's one flag left
        Assert.True(updatedChallenge.IsEnabled);

        output.WriteLine($"Successfully removed flag from challenge {challenge.Id}");
    }

    [Fact]
    public async Task RemoveFlag_ShouldDisableChallenge_WhenLastFlagRemoved()
    {
        using var scope = factory.Services.CreateScope();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "Last Flag Test Game",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create challenge with single flag
        var challenge = new GameChallenge
        {
            Title = "Last Flag Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        var flag = new FlagContext { Flag = "flag{last}", Challenge = challenge };
        challenge.Flags.Add(flag);

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Verify challenge is enabled
        Assert.True(challenge.IsEnabled);

        // Remove the last flag
        await challengeRepo.LoadFlags(challenge, CancellationToken.None);
        var flagId = challenge.Flags.First().Id;
        var result = await challengeRepo.RemoveFlag(challenge, flagId, CancellationToken.None);

        Assert.Equal(TaskStatus.Success, result);

        // Verify challenge is now disabled
        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedChallenge = await context.GameChallenges.FindAsync(challenge.Id);
        Assert.NotNull(updatedChallenge);
        Assert.False(updatedChallenge.IsEnabled);

        output.WriteLine($"Challenge {challenge.Id} correctly disabled after removing last flag");
    }

    [Fact]
    public async Task RemoveFlag_ShouldReturnNotFound_ForInvalidFlagId()
    {
        using var scope = factory.Services.CreateScope();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game and challenge
        var game = new Game
        {
            Title = "Invalid Flag Test Game",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        var challenge = new GameChallenge
        {
            Title = "Invalid Flag Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        var flag = new FlagContext { Flag = "flag{valid}", Challenge = challenge };
        challenge.Flags.Add(flag);

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Try to remove non-existent flag
        var result = await challengeRepo.RemoveFlag(challenge, 99999, CancellationToken.None);

        Assert.Equal(TaskStatus.NotFound, result);

        output.WriteLine("Correctly returned NotFound for invalid flag ID");
    }

    [Fact]
    public async Task LoadFlags_ShouldLoadChallengeFlags()
    {
        using var scope = factory.Services.CreateScope();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "Load Flags Test Game",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create challenge with flags
        var challenge = new GameChallenge
        {
            Title = "Load Flags Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        challenge.Flags.Add(new FlagContext { Flag = "flag{load1}", Challenge = challenge });
        challenge.Flags.Add(new FlagContext { Flag = "flag{load2}", Challenge = challenge });

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Get challenge using AsNoTracking to avoid auto-includes
        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var retrievedChallenge = await context.GameChallenges
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .FirstOrDefaultAsync(c => c.Id == challenge.Id);
        Assert.NotNull(retrievedChallenge);

        // Create a new context to get a fresh entity
        using var context2 = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challengeToLoad = await context2.GameChallenges.FindAsync(challenge.Id);
        Assert.NotNull(challengeToLoad);

        // Load flags explicitly
        await challengeRepo.LoadFlags(challengeToLoad, CancellationToken.None);

        // Verify flags are loaded
        Assert.Equal(2, challengeToLoad.Flags.Count);
        Assert.Contains(challengeToLoad.Flags, f => f.Flag == "flag{load1}");
        Assert.Contains(challengeToLoad.Flags, f => f.Flag == "flag{load2}");

        output.WriteLine($"Successfully loaded {challengeToLoad.Flags.Count} flags");
    }

    [Fact]
    public async Task EnsureInstances_ShouldCreateInstances_ForExistingParticipations()
    {
        using var scope = factory.Services.CreateScope();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

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
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Instance Team");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = new Participation
        {
            GameId = game.Id,
            TeamId = team.Id,
            Status = ParticipationStatus.Accepted
        };

        await context.Participations.AddAsync(participation);
        await context.SaveChangesAsync();

        // Create challenge after participation exists
        var challenge = new GameChallenge
        {
            Title = "Instance Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Ensure instances - should create one for existing participation
        var gameEntity = await context.Games.FindAsync(game.Id);
        Assert.NotNull(gameEntity);

        var instancesCreated = await challengeRepo.EnsureInstances(challenge, gameEntity, CancellationToken.None);
        Assert.True(instancesCreated);

        // Verify instance was created
        var instances = context.Set<GameInstance>()
            .Where(gi => gi.ChallengeId == challenge.Id && gi.ParticipationId == participation.Id)
            .ToList();

        Assert.Single(instances);

        output.WriteLine($"Successfully created instance for challenge {challenge.Id}");
    }

    [Fact]
    public async Task EnsureInstances_ShouldReturnFalse_WhenNoNewInstances()
    {
        using var scope = factory.Services.CreateScope();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "No Instance Test Game",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create challenge (no participations exist)
        var challenge = new GameChallenge
        {
            Title = "No Instance Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gameEntity = await context.Games.FindAsync(game.Id);
        Assert.NotNull(gameEntity);

        // Ensure instances - should return false since no participations
        var instancesCreated = await challengeRepo.EnsureInstances(challenge, gameEntity, CancellationToken.None);
        Assert.False(instancesCreated);

        output.WriteLine("Correctly returned false when no new instances needed");
    }

    [Fact]
    public async Task GetChallenge_ShouldIncludeFirstSolves()
    {
        using var scope = factory.Services.CreateScope();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "First Solve Test Game",
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
            Title = "First Solve Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Get challenge
        var retrieved = await challengeRepo.GetChallenge(game.Id, challenge.Id, CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(challenge.Id, retrieved.Id);
        Assert.Equal(challenge.Title, retrieved.Title);
        Assert.NotNull(retrieved.FirstSolves); // Should be auto-included

        output.WriteLine($"Successfully retrieved challenge {challenge.Id} with FirstSolves");
    }

    [Fact]
    public async Task GetChallenges_ShouldReturnAllGameChallenges()
    {
        using var scope = factory.Services.CreateScope();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        // Create game
        var game = new Game
        {
            Title = "Multiple Challenges Test Game",
            Summary = "Test",
            Content = "Test",
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2),
            AcceptWithoutReview = true
        };

        await gameRepo.CreateGame(game, CancellationToken.None);

        // Create multiple challenges
        for (int i = 1; i <= 3; i++)
        {
            var challenge = new GameChallenge
            {
                Title = $"Challenge {i}",
                Content = "Content",
                GameId = game.Id,
                Type = ChallengeType.StaticAttachment,
                IsEnabled = true,
                OriginalScore = 100 * i,
                MinScoreRate = 1.0,
                Difficulty = i
            };

            await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);
        }

        // Get all challenges
        var challenges = await challengeRepo.GetChallenges(game.Id, CancellationToken.None);

        Assert.Equal(3, challenges.Length);
        Assert.All(challenges, c => Assert.Equal(game.Id, c.GameId));
        Assert.Contains(challenges, c => c.Title == "Challenge 1");
        Assert.Contains(challenges, c => c.Title == "Challenge 2");
        Assert.Contains(challenges, c => c.Title == "Challenge 3");

        // Verify ordering by ID
        for (int i = 0; i < challenges.Length - 1; i++)
        {
            Assert.True(challenges[i].Id < challenges[i + 1].Id);
        }

        output.WriteLine($"Successfully retrieved {challenges.Length} challenges ordered by ID");
    }
}
