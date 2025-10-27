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
/// Integration tests for SubmissionRepository focusing on counting, filtering, and data operations
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class SubmissionRepositoryTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task CountSubmissions_ShouldReturnCorrectCount_ForParticipation()
    {
        using var scope = factory.Services.CreateScope();
        var submissionRepo = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

        // Create game
        var game = new Game
        {
            Title = "Submission Count Test",
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

        // Create participation
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Submission Team");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = new Participation
        {
            GameId = game.Id,
            TeamId = team.Id,
            Status = ParticipationStatus.Accepted
        };

        await context.Participations.AddAsync(participation);
        await context.SaveChangesAsync();

        // Initially no submissions
        var initialCount =
            await submissionRepo.CountSubmissions(participation.Id, challenge.Id, CancellationToken.None);
        Assert.Equal(0, initialCount);

        // Add submissions
        for (int i = 0; i < 3; i++)
        {
            var submission = new Submission
            {
                GameId = game.Id,
                ChallengeId = challenge.Id,
                ParticipationId = participation.Id,
                TeamId = team.Id,
                UserId = user.Id,
                Answer = $"flag{i}",
                Status = AnswerResult.WrongAnswer,
                SubmitTimeUtc = DateTimeOffset.UtcNow
            };

            await submissionRepo.AddSubmission(submission, CancellationToken.None);
        }

        // Count should be 3
        var finalCount = await submissionRepo.CountSubmissions(participation.Id, challenge.Id, CancellationToken.None);
        Assert.Equal(3, finalCount);

        output.WriteLine($"Submission count verified: {finalCount}");
    }

    [Fact]
    public async Task GetSubmissions_ShouldFilterByType()
    {
        using var scope = factory.Services.CreateScope();
        var submissionRepo = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

        // Create game
        var game = new Game
        {
            Title = "Filter Test Game",
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
            Title = "Filter Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Create participation
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Filter Team");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = new Participation
        {
            GameId = game.Id,
            TeamId = team.Id,
            Status = ParticipationStatus.Accepted
        };

        await context.Participations.AddAsync(participation);
        await context.SaveChangesAsync();

        // Add different types of submissions
        var submissionTypes = new[]
        {
            AnswerResult.Accepted, AnswerResult.WrongAnswer, AnswerResult.WrongAnswer, AnswerResult.Accepted
        };

        foreach (var status in submissionTypes)
        {
            var submission = new Submission
            {
                GameId = game.Id,
                ChallengeId = challenge.Id,
                ParticipationId = participation.Id,
                TeamId = team.Id,
                UserId = user.Id,
                Answer = "test",
                Status = status,
                SubmitTimeUtc = DateTimeOffset.UtcNow
            };

            await submissionRepo.AddSubmission(submission, CancellationToken.None);
            await Task.Delay(10); // Ensure different timestamps
        }

        // Get all submissions
        var allSubmissions = await submissionRepo.GetSubmissions(game, null, 100, 0, CancellationToken.None);
        Assert.Equal(4, allSubmissions.Length);

        // Get only accepted submissions
        var acceptedSubmissions = await submissionRepo.GetSubmissions(game, AnswerResult.Accepted, 100, 0,
            CancellationToken.None);
        Assert.Equal(2, acceptedSubmissions.Length);
        Assert.All(acceptedSubmissions, s => Assert.Equal(AnswerResult.Accepted, s.Status));

        // Get only wrong submissions
        var wrongSubmissions = await submissionRepo.GetSubmissions(game, AnswerResult.WrongAnswer, 100, 0,
            CancellationToken.None);
        Assert.Equal(2, wrongSubmissions.Length);
        Assert.All(wrongSubmissions, s => Assert.Equal(AnswerResult.WrongAnswer, s.Status));

        output.WriteLine(
            $"Filter test passed - All: {allSubmissions.Length}, Accepted: {acceptedSubmissions.Length}, Wrong: {wrongSubmissions.Length}");
    }

    [Fact]
    public async Task GetSubmissions_ShouldRespectPagination()
    {
        using var scope = factory.Services.CreateScope();
        var submissionRepo = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

        // Create game
        var game = new Game
        {
            Title = "Pagination Test",
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
            Title = "Page Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Create participation
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Page Team");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = new Participation
        {
            GameId = game.Id,
            TeamId = team.Id,
            Status = ParticipationStatus.Accepted
        };

        await context.Participations.AddAsync(participation);
        await context.SaveChangesAsync();

        // Add 10 submissions
        for (int i = 0; i < 10; i++)
        {
            var submission = new Submission
            {
                GameId = game.Id,
                ChallengeId = challenge.Id,
                ParticipationId = participation.Id,
                TeamId = team.Id,
                UserId = user.Id,
                Answer = $"flag{i}",
                Status = AnswerResult.WrongAnswer,
                SubmitTimeUtc = DateTimeOffset.UtcNow.AddSeconds(i)
            };

            await submissionRepo.AddSubmission(submission, CancellationToken.None);
        }

        // Get first 5
        var firstPage = await submissionRepo.GetSubmissions(game, null, 5, 0, CancellationToken.None);
        Assert.Equal(5, firstPage.Length);

        // Get next 5
        var secondPage = await submissionRepo.GetSubmissions(game, null, 5, 5, CancellationToken.None);
        Assert.Equal(5, secondPage.Length);

        // Verify no overlap
        var firstIds = firstPage.Select(s => s.Id).ToHashSet();
        var secondIds = secondPage.Select(s => s.Id).ToHashSet();
        Assert.Empty(firstIds.Intersect(secondIds));

        output.WriteLine($"Pagination test passed - First page: {firstPage.Length}, Second page: {secondPage.Length}");
    }

    [Fact]
    public async Task GetUncheckedFlags_ShouldReturnOnlyFlagSubmitted()
    {
        using var scope = factory.Services.CreateScope();
        var submissionRepo = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

        // Create game
        var game = new Game
        {
            Title = "Unchecked Test",
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
            Title = "Unchecked Challenge",
            Content = "Content",
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 1.0,
            Difficulty = 1
        };

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Create participation
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Unchecked Team");

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participation = new Participation
        {
            GameId = game.Id,
            TeamId = team.Id,
            Status = ParticipationStatus.Accepted
        };

        await context.Participations.AddAsync(participation);
        await context.SaveChangesAsync();

        // Add submissions with different statuses
        var unchecked1 = new Submission
        {
            GameId = game.Id,
            ChallengeId = challenge.Id,
            ParticipationId = participation.Id,
            TeamId = team.Id,
            UserId = user.Id,
            Answer = "unchecked1",
            Status = AnswerResult.FlagSubmitted,
            SubmitTimeUtc = DateTimeOffset.UtcNow
        };

        var unchecked2 = new Submission
        {
            GameId = game.Id,
            ChallengeId = challenge.Id,
            ParticipationId = participation.Id,
            TeamId = team.Id,
            UserId = user.Id,
            Answer = "unchecked2",
            Status = AnswerResult.FlagSubmitted,
            SubmitTimeUtc = DateTimeOffset.UtcNow
        };

        var checkedSubmission = new Submission
        {
            GameId = game.Id,
            ChallengeId = challenge.Id,
            ParticipationId = participation.Id,
            TeamId = team.Id,
            UserId = user.Id,
            Answer = "checked",
            Status = AnswerResult.Accepted,
            SubmitTimeUtc = DateTimeOffset.UtcNow
        };

        await submissionRepo.AddSubmission(unchecked1, CancellationToken.None);
        await submissionRepo.AddSubmission(unchecked2, CancellationToken.None);
        await submissionRepo.AddSubmission(checkedSubmission, CancellationToken.None);

        // Get unchecked flags
        var uncheckedFlags = await submissionRepo.GetUncheckedFlags(CancellationToken.None);

        Assert.Equal(2, uncheckedFlags.Length);
        Assert.All(uncheckedFlags, s => Assert.Equal(AnswerResult.FlagSubmitted, s.Status));

        output.WriteLine($"Unchecked flags test passed - Found {uncheckedFlags.Length} unchecked submissions");
    }
}
