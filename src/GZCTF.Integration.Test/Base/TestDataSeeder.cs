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
    // Shared game instances for test reuse
    private static int _sharedBasicGameId;
    private static int _sharedInviteGameId;
    private static int _sharedPracticeModeGameId;
    private static int _sharedWithReviewGameId;

    private static readonly SemaphoreSlim _basicGameLock = new(1, 1);
    private static readonly SemaphoreSlim _inviteGameLock = new(1, 1);
    private static readonly SemaphoreSlim _practiceModeGameLock = new(1, 1);
    private static readonly SemaphoreSlim _withReviewGameLock = new(1, 1);

    /// <summary>
    /// Get or create a shared basic game (no special configuration)
    /// Suitable for: basic join, permission tests, multi-user scenarios
    /// </summary>
    public static async Task<int> GetOrCreateBasicGameAsync(IServiceProvider services)
    {
        if (_sharedBasicGameId > 0)
            return _sharedBasicGameId;

        await _basicGameLock.WaitAsync();
        try
        {
            if (_sharedBasicGameId > 0)
                return _sharedBasicGameId;

            var game = await CreateGameAsync(services, "Shared Basic Game");
            _sharedBasicGameId = game.Id;
            return _sharedBasicGameId;
        }
        finally
        {
            _basicGameLock.Release();
        }
    }

    /// <summary>
    /// Get or create a shared game with invite code (InviteCode = "SHARED_INVITE_2025")
    /// Suitable for: invite code validation tests
    /// </summary>
    public static async Task<int> GetOrCreateInviteGameAsync(IServiceProvider services)
    {
        if (_sharedInviteGameId > 0)
            return _sharedInviteGameId;

        await _inviteGameLock.WaitAsync();
        try
        {
            if (_sharedInviteGameId > 0)
                return _sharedInviteGameId;

            var game = await CreateGameAsync(services, "Shared Invite Game");

            // Set game invite code
            using var scope = services.CreateScope();
            var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
            var gameEntity = await gameRepo.GetGameById(game.Id, default);
            if (gameEntity != null)
            {
                gameEntity.InviteCode = "SHARED_INVITE_2025";
                await gameRepo.SaveAsync(default);
            }

            _sharedInviteGameId = game.Id;
            return _sharedInviteGameId;
        }
        finally
        {
            _inviteGameLock.Release();
        }
    }

    /// <summary>
    /// Get or create a shared practice mode game
    /// Suitable for: practice mode tests, deadline-related tests
    /// </summary>
    public static async Task<int> GetOrCreatePracticeModeGameAsync(IServiceProvider services)
    {
        if (_sharedPracticeModeGameId > 0)
            return _sharedPracticeModeGameId;

        await _practiceModeGameLock.WaitAsync();
        try
        {
            if (_sharedPracticeModeGameId > 0)
                return _sharedPracticeModeGameId;

            var game = await CreateGameAsync(services, "Shared Practice Mode Game",
                practiceMode: true);
            _sharedPracticeModeGameId = game.Id;
            return _sharedPracticeModeGameId;
        }
        finally
        {
            _practiceModeGameLock.Release();
        }
    }

    /// <summary>
    /// Get or create a shared game with review requirement (AcceptWithoutReview = false)
    /// Suitable for: participation status tests, review workflow tests
    /// </summary>
    public static async Task<int> GetOrCreateWithReviewGameAsync(IServiceProvider services)
    {
        if (_sharedWithReviewGameId > 0)
            return _sharedWithReviewGameId;

        await _withReviewGameLock.WaitAsync();
        try
        {
            if (_sharedWithReviewGameId > 0)
                return _sharedWithReviewGameId;

            var game = await CreateGameAsync(services, "Shared With Review Game",
                acceptWithoutReview: false);
            _sharedWithReviewGameId = game.Id;
            return _sharedWithReviewGameId;
        }
        finally
        {
            _withReviewGameLock.Release();
        }
    }

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
        bool practiceMode = false, CancellationToken token = default)
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
            PracticeMode = practiceMode,
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
