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
        
        var team = _fixture.Build<Team>()
            .With(t => t.Name, teamName ?? _fixture.Create<string>())
            .With(t => t.Captain, captain)
            .With(t => t.CaptainId, captain.Id)
            .Without(t => t.Members)
            .Without(t => t.Participations)
            .Create();

        return team;
    }

    /// <summary>
    /// Create a test game
    /// </summary>
    public static Game CreateGame(string? title = null, DateTimeOffset? start = null, DateTimeOffset? end = null)
    {
        var now = DateTimeOffset.UtcNow;
        
        var game = _fixture.Build<Game>()
            .With(g => g.Title, title ?? _fixture.Create<string>())
            .With(g => g.StartTimeUtc, start ?? now.AddDays(1))
            .With(g => g.EndTimeUtc, end ?? now.AddDays(2))
            .Without(g => g.Challenges)
            .Without(g => g.Participations)
            .Without(g => g.Submissions)
            .Create();

        return game;
    }

    /// <summary>
    /// Create a test challenge
    /// </summary>
    public static GameChallenge CreateChallenge(Game? game = null, string? title = null, int points = 100)
    {
        game ??= CreateGame();
        
        var challenge = _fixture.Build<GameChallenge>()
            .With(c => c.Title, title ?? _fixture.Create<string>())
            .With(c => c.OriginalScore, points)
            .With(c => c.Game, game)
            .With(c => c.GameId, game.Id)
            .With(c => c.Type, ChallengeType.StaticAttachment)
            .With(c => c.IsEnabled, true)
            .Without(c => c.Flags)
            .Without(c => c.Instances)
            .Without(c => c.Submissions)
            .Without(c => c.Attachment)
            .Without(c => c.TestContainer)
            .Create();

        return challenge;
    }

    /// <summary>
    /// Create a test flag
    /// </summary>
    public static FlagContext CreateFlag(GameChallenge? challenge = null, string? flag = null)
    {
        challenge ??= CreateChallenge();
        
        var flagContext = _fixture.Build<FlagContext>()
            .With(f => f.Flag, flag ?? $"flag{{{_fixture.Create<string>()}}}")
            .With(f => f.Challenge, challenge)
            .With(f => f.ChallengeId, challenge.Id)
            .Without(f => f.Attachment)
            .Create();

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
        
        var submission = _fixture.Build<Submission>()
            .With(s => s.Answer, answer ?? _fixture.Create<string>())
            .With(s => s.Status, status)
            .With(s => s.SubmitTimeUtc, DateTimeOffset.UtcNow)
            .With(s => s.User, user)
            .With(s => s.UserId, user.Id)
            .With(s => s.Team, team)
            .With(s => s.TeamId, team.Id)
            .With(s => s.GameChallenge, challenge)
            .With(s => s.ChallengeId, challenge.Id)
            .Create();

        return submission;
    }

    /// <summary>
    /// Create a test participation
    /// </summary>
    public static Participation CreateParticipation(Team? team = null, Game? game = null, ParticipationStatus status = ParticipationStatus.Accepted)
    {
        team ??= CreateTeam();
        game ??= CreateGame();
        
        var participation = _fixture.Build<Participation>()
            .With(p => p.Team, team)
            .With(p => p.TeamId, team.Id)
            .With(p => p.Game, game)
            .With(p => p.GameId, game.Id)
            .With(p => p.Status, status)
            .Without(p => p.Members)
            .Without(p => p.Instances)
            .Without(p => p.Submissions)
            .Without(p => p.Writeup)
            .Create();

        return participation;
    }

    /// <summary>
    /// Create a test post
    /// </summary>
    public static Post CreatePost(UserInfo? author = null, string? title = null, string? content = null)
    {
        var post = _fixture.Build<Post>()
            .With(p => p.Title, title ?? _fixture.Create<string>())
            .With(p => p.Content, content ?? _fixture.Create<string>())
            .With(p => p.Author, author)
            .With(p => p.AuthorId, author?.Id)
            .With(p => p.UpdateTimeUtc, DateTimeOffset.UtcNow)
            .With(p => p.IsPinned, false)
            .Create();

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