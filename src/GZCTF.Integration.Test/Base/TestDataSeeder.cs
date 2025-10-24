using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GZCTF.Integration.Test.Base;

public static class TestDataSeeder
{
    public static async Task<SeededUser> CreateUserAsync(IServiceProvider services, string userName, string email,
        string password, Role role = Role.User, CancellationToken token = default)
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

        return new SeededUser(user.Id, user.UserName!, user.Email!, password, role);
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
        DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken token = default)
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
            AcceptWithoutReview = true,
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
        string title, string flag, CancellationToken token = default)
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
            OriginalScore = 1000,
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

    private static string NormalizeTeamName(string teamName)
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
}
