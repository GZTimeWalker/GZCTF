using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace GZCTF.Integration.Test.Base;

public static class TestDataSeeder
{
    public static async Task<SeededUser> CreateUserAsync(IServiceProvider services, string userName,
        string password, string? email = null, Role role = Role.User, CancellationToken token = default)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserInfo>>();

        var normalizedUserName = NormalizeUserName(userName);
        var normalizedEmail = string.IsNullOrWhiteSpace(email)
            ? $"{normalizedUserName}@example.local"
            : email;

        var user = await userManager.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.UserName == normalizedUserName, token);

        if (user is null)
        {
            user = new UserInfo
            {
                UserName = normalizedUserName,
                Email = normalizedEmail,
                EmailConfirmed = true,
                Role = role
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create user {normalizedUserName}: " +
                                                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        else
        {
            user.UserName = normalizedUserName;
            user.Email = normalizedEmail;
            user.EmailConfirmed = true;
            user.Role = role;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException($"Failed to update user {normalizedUserName}: " +
                                                    string.Join(", ", updateResult.Errors.Select(e => e.Description)));
        }

        return new SeededUser(user.Id, user.UserName, user.Email!, password, role);
    }

    public static async Task<SeededTeam> CreateTeamAsync(IServiceProvider services, Guid ownerId, string name,
        string? bio = null, CancellationToken token = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var normalizedName = NormalizeTeamName(name);

        var owner = await context.Users.FirstOrDefaultAsync(u => u.Id == ownerId, token)
                    ?? throw new InvalidOperationException($"Owner user {ownerId} not found");

        Team team = new()
        {
            Name = normalizedName,
            Bio = bio,
            Captain = owner,
            CaptainId = owner.Id,
            Locked = false
        };

        team.Members.Add(owner);

        await context.Teams.AddAsync(team, token);
        await context.SaveChangesAsync(token);

        return new SeededTeam(team.Id, team.Name, owner.Id);
    }

    public static async Task<SeededGame> CreateGameAsync(IServiceProvider services, string title,
        DateTimeOffset? start = null, DateTimeOffset? end = null, bool acceptWithoutReview = true,
        CancellationToken token = default)
    {
        using var scope = services.CreateScope();
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        var now = DateTimeOffset.UtcNow;
        var game = new Game
        {
            Title = title,
            Summary = "Test game summary",
            Content = "Test game content",
            Hidden = false,
            PracticeMode = false,
            AcceptWithoutReview = acceptWithoutReview,
            WriteupRequired = false,
            TeamMemberCountLimit = 0,
            ContainerCountLimit = 3,
            StartTimeUtc = start ?? now.AddMinutes(-5),
            EndTimeUtc = end ?? now.AddHours(2),
            WriteupDeadline = end ?? now.AddHours(2),
            WriteupNote = ""
        };

        var created = await gameRepository.CreateGame(game, token)
                      ?? throw new InvalidOperationException($"Failed to create game {title}");

        return new SeededGame(created.Id, created.Title, created.StartTimeUtc, created.EndTimeUtc);
    }

    public static async Task<SeededChallenge> CreateStaticChallengeAsync(IServiceProvider services, int gameId,
        string title, string flag, int originalScore = 1000, CancellationToken token = default)
    {
        using var scope = services.CreateScope();
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var challengeRepository = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

        var game = await gameRepository.GetGameById(gameId, token)
                   ?? throw new InvalidOperationException($"Game {gameId} not found");

        GameChallenge challenge = new()
        {
            Title = title,
            Content = "Static challenge content",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.StaticAttachment,
            Hints = [],
            IsEnabled = true,
            SubmissionLimit = 0,
            OriginalScore = originalScore,
            MinScoreRate = 0.8,
            Difficulty = 5,
            Game = game,
            GameId = game.Id
        };

        FlagContext flagContext = new() { Flag = flag, Challenge = challenge };
        challenge.Flags.Add(flagContext);

        await challengeRepository.CreateChallenge(game, challenge, token);

        return new SeededChallenge(challenge.Id, challenge.Title, flag);
    }

    public static async Task<SeededParticipation> JoinGameAsync(IServiceProvider services, int gameId, int teamId,
        Guid userId, int? divisionId = null, CancellationToken token = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participationRepository = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();

        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId, token)
                   ?? throw new InvalidOperationException($"Game {gameId} not found");

        var team = await context.Teams.Include(t => t.Members)
                       .FirstOrDefaultAsync(t => t.Id == teamId, token)
                   ?? throw new InvalidOperationException($"Team {teamId} not found");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, token)
                   ?? throw new InvalidOperationException($"User {userId} not found");

        // Check if already participating
        var existingPart = await context.Participations
            .FirstOrDefaultAsync(p => p.GameId == gameId && p.TeamId == teamId, token);

        if (existingPart is not null)
        {
            // Add user to participation if not already a member
            if (!existingPart.Members.Any(m => m.UserId == userId))
            {
                existingPart.Members.Add(new UserParticipation
                {
                    GameId = gameId,
                    TeamId = teamId,
                    UserId = userId,
                    User = user
                });
                await context.SaveChangesAsync(token);
            }

            return new SeededParticipation(existingPart.Id, existingPart.GameId, existingPart.TeamId,
                existingPart.Status);
        }

        // Load division if specified
        Division? division = null;
        if (divisionId.HasValue)
        {
            division = await context.Divisions.FirstOrDefaultAsync(d => d.Id == divisionId.Value, token);
        }

        // Determine participation status based on division and game settings
        var divWithoutReview = division is null || !division.DefaultPermissions.HasFlag(GamePermission.RequireReview);
        var status = divWithoutReview && game.AcceptWithoutReview
            ? ParticipationStatus.Accepted
            : ParticipationStatus.Pending;

        // Create new participation
        var participation = new Participation
        {
            GameId = gameId,
            TeamId = teamId,
            DivisionId = divisionId,
            Status = status
        };

        participation.Members.Add(new UserParticipation
        {
            GameId = gameId,
            TeamId = teamId,
            UserId = userId,
            User = user
        });

        await context.Participations.AddAsync(participation, token);
        await context.SaveChangesAsync(token);

        // If participation is accepted, create game instances for all enabled challenges
        if (status == ParticipationStatus.Accepted)
        {
            var gameInstances = context.GameChallenges
                .Where(c => c.GameId == gameId && c.IsEnabled)
                .Select(c => new GameInstance { ParticipationId = participation.Id, ChallengeId = c.Id })
                .ToList();

            if (gameInstances.Any())
            {
                await context.GameInstances.AddRangeAsync(gameInstances, token);
                await context.SaveChangesAsync(token);
            }
        }

        return new SeededParticipation(participation.Id, participation.GameId, participation.TeamId,
            participation.Status);
    }

    public static string RandomName(int length = 15)
    {
        Span<byte> bytes = stackalloc byte[length * 6 / 8 + 1];
        Random.Shared.NextBytes(bytes);
        return SimpleBase.Base62.Default.Encode(bytes)[..length];
    }

    private static string NormalizeUserName(string? userName)
    {
        var trimmed = (userName ?? string.Empty).Trim();

        if (trimmed.Length > Limits.MaxUserNameLength)
            trimmed = trimmed[..Limits.MaxUserNameLength];

        if (trimmed.Length < Limits.MinUserNameLength)
            trimmed = trimmed.PadRight(Limits.MinUserNameLength, '0');

        return trimmed;
    }

    private static string NormalizeTeamName(string? teamName)
    {
        var trimmed = (teamName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
            trimmed = "team";

        if (trimmed.Length > Limits.MaxTeamNameLength)
            trimmed = trimmed[..Limits.MaxTeamNameLength];

        return trimmed;
    }

    public record SeededUser(Guid Id, string UserName, string Email, string Password, Role Role);

    public record SeededTeam(int Id, string Name, Guid OwnerId);

    public record SeededGame(int Id, string Title, DateTimeOffset Start, DateTimeOffset End);

    public record SeededChallenge(int Id, string Title, string Flag);

    public record SeededParticipation(int Id, int GameId, int TeamId, ParticipationStatus Status);
}
