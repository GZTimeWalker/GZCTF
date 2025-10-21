using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoFixture;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace GZCTF.Test.Infrastructure;

/// <summary>
/// Factory for creating test data entities with proper relationships
/// </summary>
public static class TestDataFactory
{
    private static readonly Fixture _fixture = new();

    static TestDataFactory()
    {
        // Configure fixture to handle circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    /// <summary>
    /// Create a test user with default values
    /// </summary>
    public static UserInfo CreateUser(string? userName = null, string? email = null, Role role = Role.User)
    {
        var user = _fixture.Build<UserInfo>()
            .With(u => u.UserName, userName ?? _fixture.Create<string>())
            .With(u => u.Email, email ?? $"{_fixture.Create<string>()}@test.com")
            .With(u => u.Role, role)
            .With(u => u.EmailConfirmed, true)
            .With(u => u.RegisterTimeUtc, DateTimeOffset.UtcNow)
            .Without(u => u.Teams)
            .Without(u => u.Submissions)
            .Create();
        
        return user;
    }

    /// <summary>
    /// Create a test team with captain
    /// </summary>
    public static Team CreateTeam(UserInfo? captain = null, string? teamName = null)
    {
        captain ??= CreateUser();
        
        var team = new Team
        {
            Name = teamName ?? $"TestTeam_{Guid.NewGuid().ToString()[..8]}",
            Captain = captain,
            CaptainId = captain.Id,
            Bio = "Test team bio",
            Locked = false
        };

        return team;
    }

    /// <summary>
    /// Create a test game
    /// </summary>
    public static Game CreateGame(string? title = null, DateTimeOffset? start = null, DateTimeOffset? end = null)
    {
        var now = DateTimeOffset.UtcNow;
        var kpg = new Ed25519KeyPairGenerator();
        kpg.Init(new Org.BouncyCastle.Crypto.KeyGenerationParameters(new SecureRandom(), 256));
        var kp = kpg.GenerateKeyPair();
        var privateKey = (Ed25519PrivateKeyParameters)kp.Private;
        var publicKey = (Ed25519PublicKeyParameters)kp.Public;
        
        var game = new Game
        {
            Title = title ?? $"TestGame_{Guid.NewGuid().ToString()[..8]}",
            StartTimeUtc = start ?? now.AddDays(1),
            EndTimeUtc = end ?? now.AddDays(2),
            PublicKey = Convert.ToBase64String(publicKey.GetEncoded()),
            PrivateKey = Convert.ToBase64String(privateKey.GetEncoded()),
            Hidden = false,
            PracticeMode = true,
            AcceptWithoutReview = true
        };

        return game;
    }

    /// <summary>
    /// Create a test challenge
    /// </summary>
    public static GameChallenge CreateChallenge(Game? game = null, string? title = null, int points = 100)
    {
        game ??= CreateGame();
        
        var challenge = new GameChallenge
        {
            Title = title ?? $"TestChallenge_{Guid.NewGuid().ToString()[..8]}",
            OriginalScore = points,
            Game = game,
            GameId = game.Id,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            Content = "Test challenge content",
            Difficulty = 5.0,
            MinScoreRate = 0.25,
            EnableTrafficCapture = false,
            DisableBloodBonus = false
        };

        return challenge;
    }

    /// <summary>
    /// Create a test flag
    /// </summary>
    public static FlagContext CreateFlag(GameChallenge? challenge = null, string? flag = null)
    {
        challenge ??= CreateChallenge();
        
        var flagContext = new FlagContext
        {
            Flag = flag ?? $"flag{{{Guid.NewGuid().ToString()[..16]}}}",
            Challenge = challenge,
            ChallengeId = challenge.Id,
            IsOccupied = false
        };

        return flagContext;
    }

    /// <summary>
    /// Create a test submission
    /// </summary>
    public static Submission CreateSubmission(
        UserInfo? user = null, 
        Team? team = null, 
        GameChallenge? challenge = null,
        string? answer = null,
        AnswerResult status = AnswerResult.Accepted)
    {
        user ??= CreateUser();
        team ??= CreateTeam(user);
        challenge ??= CreateChallenge();
        
        var submission = new Submission
        {
            Answer = answer ?? "test_answer",
            Status = status,
            SubmitTimeUtc = DateTimeOffset.UtcNow,
            User = user,
            UserId = user.Id,
            Team = team,
            TeamId = team.Id,
            GameChallenge = challenge,
            ChallengeId = challenge.Id,
            GameId = challenge.GameId
        };

        return submission;
    }

    /// <summary>
    /// Create a test participation
    /// </summary>
    public static Participation CreateParticipation(Team? team = null, Game? game = null, ParticipationStatus status = ParticipationStatus.Accepted)
    {
        team ??= CreateTeam();
        game ??= CreateGame();
        
        var participation = new Participation
        {
            Team = team,
            TeamId = team.Id,
            Game = game,
            GameId = game.Id,
            Status = status,
            Token = Guid.NewGuid().ToString()
        };

        return participation;
    }

    /// <summary>
    /// Create a test post
    /// </summary>
    public static Post CreatePost(UserInfo? author = null, string? title = null, string? content = null)
    {
        var post = new Post
        {
            Title = title ?? $"TestPost_{Guid.NewGuid().ToString()[..8]}",
            Content = content ?? "Test post content",
            Author = author,
            AuthorId = author?.Id,
            UpdateTimeUtc = DateTimeOffset.UtcNow,
            IsPinned = false,
            Summary = "Test summary"
        };

        return post;
    }

    /// <summary>
    /// Seed a database context with test data
    /// </summary>
    public static async Task SeedDatabaseAsync(AppDbContext context)
    {
        // Create test users
        var adminUser = CreateUser("admin", "admin@test.com", Role.Admin);
        var normalUser = CreateUser("user", "user@test.com", Role.User);
        
        context.Users.AddRange(adminUser, normalUser);

        // Create test teams
        var team1 = CreateTeam(adminUser, "Team Alpha");
        var team2 = CreateTeam(normalUser, "Team Beta");
        
        context.Teams.AddRange(team1, team2);

        // Create test game
        var game = CreateGame("Test CTF 2024");
        context.Games.Add(game);

        // Create test challenges
        var challenge1 = CreateChallenge(game, "Web Challenge", 100);
        var challenge2 = CreateChallenge(game, "Crypto Challenge", 200);
        
        context.GameChallenges.AddRange(challenge1, challenge2);

        // Create test flags
        var flag1 = CreateFlag(challenge1, "flag{test_web}");
        var flag2 = CreateFlag(challenge2, "flag{test_crypto}");
        
        context.FlagContexts.AddRange(flag1, flag2);

        // Create test participations
        var participation1 = CreateParticipation(team1, game);
        var participation2 = CreateParticipation(team2, game);
        
        context.Participations.AddRange(participation1, participation2);

        // Create test submissions
        var submission1 = CreateSubmission(adminUser, team1, challenge1, "flag{test_web}", AnswerResult.Accepted);
        var submission2 = CreateSubmission(normalUser, team2, challenge2, "wrong_flag", AnswerResult.WrongAnswer);
        
        context.Submissions.AddRange(submission1, submission2);

        await context.SaveChangesAsync();
    }
}