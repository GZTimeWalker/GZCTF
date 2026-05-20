using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Utils;

/// <summary>
/// Idempotent development-only data seeder. Populates enough users, teams,
/// game, challenges, participations, and synthetic activity for manual e2e
/// testing without clicking through registration each time.
///
/// Gated on <c>IHostEnvironment.IsDevelopment()</c> by the caller in
/// <see cref="PrelaunchHelper"/>. Safe to re-run — every step checks first.
/// </summary>
public static class DevDataSeeder
{
    public record DevUser(string UserName, string Email, string Password);

    public static readonly DevUser[] Users =
    [
        new("User1", "user1@dev.local", "User1@2022"),
        new("User2", "user2@dev.local", "User2@2022"),
        new("User3", "user3@dev.local", "User3@2022"),
        new("User4", "user4@dev.local", "User4@2022")
    ];

    public const string TeamAlphaName = "Team Alpha";
    public const string TeamBravoName = "Team Bravo";
    public const string GameTitle = "Dev Test Game";
    public const string StaticAttachmentTitle = "Static Attachment Welcome";
    public const string DynamicAttachmentTitle = "Dynamic Attachment Echo";
    public const string StaticContainerTitle = "Static Container Hello";
    public const string DynamicContainerTitle = "Dynamic Container Solve";
    public const string StaticAttachmentFlag = "flag{dev-static-attachment}";
    public const string StaticContainerFlag = "flag{dev-static-container}";
    public const string ContainerImage = "nginx:alpine";

    public static async Task SeedAsync(IServiceProvider sp, ILogger logger, CancellationToken token)
    {
        var userManager = sp.GetRequiredService<UserManager<UserInfo>>();
        var teamRepo = sp.GetRequiredService<ITeamRepository>();
        var gameRepo = sp.GetRequiredService<IGameRepository>();
        var challengeRepo = sp.GetRequiredService<IGameChallengeRepository>();
        var context = sp.GetRequiredService<AppDbContext>();

        var seeded = false;

        var users = new Dictionary<string, UserInfo>();
        foreach (var spec in Users)
        {
            var existing = await userManager.FindByNameAsync(spec.UserName);
            if (existing is not null)
            {
                users[spec.UserName] = existing;
                continue;
            }

            var user = new UserInfo
            {
                UserName = spec.UserName,
                Email = spec.Email,
                Role = Role.User,
                EmailConfirmed = true,
                RegisterTimeUtc = DateTimeOffset.UtcNow
            };
            var result = await userManager.CreateAsync(user, spec.Password);
            if (!result.Succeeded)
            {
                logger.LogWarning("DevDataSeeder: failed to create {User}: {Error}",
                    spec.UserName, result.Errors.FirstOrDefault()?.Description ?? "unknown");
                return;
            }
            users[spec.UserName] = user;
            seeded = true;
        }

        var teamAlpha = await EnsureTeamAsync(teamRepo, context, TeamAlphaName,
            captain: users["User1"], second: users["User2"], token);
        var teamBravo = await EnsureTeamAsync(teamRepo, context, TeamBravoName,
            captain: users["User3"], second: users["User4"], token);
        if (teamAlpha.justCreated || teamBravo.justCreated) seeded = true;

        var (game, gameJustCreated) = await EnsureGameAsync(gameRepo, context, token);
        if (gameJustCreated) seeded = true;

        var (staticAttachment, sa) = await EnsureChallengeAsync(
            challengeRepo, context, game,
            title: StaticAttachmentTitle,
            type: ChallengeType.StaticAttachment,
            score: 100,
            staticFlag: StaticAttachmentFlag,
            token: token);

        var (dynamicAttachment, da) = await EnsureChallengeAsync(
            challengeRepo, context, game,
            title: DynamicAttachmentTitle,
            type: ChallengeType.DynamicAttachment,
            score: 200,
            staticFlag: null,
            token: token);

        var (staticContainer, sc) = await EnsureChallengeAsync(
            challengeRepo, context, game,
            title: StaticContainerTitle,
            type: ChallengeType.StaticContainer,
            score: 150,
            staticFlag: StaticContainerFlag,
            containerImage: ContainerImage,
            enableTrafficCapture: true,
            token: token);

        var (dynamicContainer, dc) = await EnsureChallengeAsync(
            challengeRepo, context, game,
            title: DynamicContainerTitle,
            type: ChallengeType.DynamicContainer,
            score: 250,
            staticFlag: null,
            containerImage: ContainerImage,
            enableTrafficCapture: true,
            token: token);

        if (sa || da || sc || dc) seeded = true;

        var partAlpha = await EnsureParticipationAsync(context, gameRepo, game, teamAlpha.team,
            [users["User1"], users["User2"]], token);
        var partBravo = await EnsureParticipationAsync(context, gameRepo, game, teamBravo.team,
            [users["User3"], users["User4"]], token);
        if (partAlpha.justCreated || partBravo.justCreated) seeded = true;

        var activitySeeded = await EnsureSyntheticActivityAsync(context, game, staticAttachment,
            dynamicContainer, partAlpha.part, partBravo.part, users, token);
        if (activitySeeded) seeded = true;

        if (seeded)
        {
            logger.LogInformation(
                """

                === Dev seed complete ===
                  Admin: admin@example.invalid / Admin@2022
                  User1: user1@dev.local / User1@2022  ({Alpha}, captain)
                  User2: user2@dev.local / User2@2022  ({Alpha})
                  User3: user3@dev.local / User3@2022  ({Bravo}, captain)
                  User4: user4@dev.local / User4@2022  ({Bravo})
                  Game:  "{Game}" (open)
                =========================
                """,
                TeamAlphaName, TeamAlphaName, TeamBravoName, TeamBravoName, GameTitle);
        }
        else
        {
            logger.LogDebug("DevDataSeeder: dev data already present, skipping.");
        }
    }

    private static async Task<(Team team, bool justCreated)> EnsureTeamAsync(
        ITeamRepository teamRepo, AppDbContext context, string name,
        UserInfo captain, UserInfo second, CancellationToken token)
    {
        var existing = await context.Teams.Include(t => t.Members).FirstOrDefaultAsync(t => t.Name == name, token);
        if (existing is not null)
            return (existing, false);

        var team = await teamRepo.CreateTeam(new TeamUpdateModel { Name = name }, captain, token);
        team.Members.Add(second);
        await context.SaveChangesAsync(token);
        return (team, true);
    }

    private static async Task<(Game game, bool justCreated)> EnsureGameAsync(
        IGameRepository gameRepo, AppDbContext context, CancellationToken token)
    {
        var existing = await context.Games.FirstOrDefaultAsync(g => g.Title == GameTitle, token);
        if (existing is not null)
            return (existing, false);

        var now = DateTimeOffset.UtcNow;
        var game = new Game
        {
            Title = GameTitle,
            Summary = "Auto-seeded game for local dev. Safe to delete.",
            Content = "This game is created automatically when running in Development environment.",
            Hidden = false,
            PracticeMode = true,
            AcceptWithoutReview = true,
            StartTimeUtc = now.AddHours(-1),
            EndTimeUtc = now.AddDays(30),
            WriteupDeadline = now.AddDays(30),
            TeamMemberCountLimit = 0,
            ContainerCountLimit = 3,
            WriteupNote = string.Empty
        };
        var created = await gameRepo.CreateGame(game, token);
        return (created ?? game, true);
    }

    private static async Task<(GameChallenge challenge, bool justCreated)> EnsureChallengeAsync(
        IGameChallengeRepository challengeRepo, AppDbContext context, Game game,
        string title, ChallengeType type, int score, string? staticFlag,
        CancellationToken token,
        string? containerImage = null, bool enableTrafficCapture = false)
    {
        var existing = await context.GameChallenges
            .Include(c => c.Flags)
            .FirstOrDefaultAsync(c => c.GameId == game.Id && c.Title == title, token);
        if (existing is not null)
            return (existing, false);

        var challenge = new GameChallenge
        {
            Title = title,
            Content = $"Auto-seeded {type} challenge for local dev.",
            Category = ChallengeCategory.Misc,
            Type = type,
            OriginalScore = score,
            MinScoreRate = 0.25,
            Difficulty = 5,
            IsEnabled = true,
            FlagTemplate = type.IsDynamic() ? "flag{[GUID]}" : null
        };

        if (type.IsContainer())
        {
            challenge.ContainerImage = containerImage ?? ContainerImage;
            challenge.ExposePort = 80;
            challenge.MemoryLimit = 64;
            challenge.StorageLimit = 256;
            challenge.CPUCount = 1;
            challenge.EnableTrafficCapture = enableTrafficCapture;
        }

        if (staticFlag is not null)
        {
            challenge.Flags.Add(new FlagContext { Flag = staticFlag });
        }

        var created = await challengeRepo.CreateChallenge(game, challenge, token);
        return (created, true);
    }

    private static async Task<(Participation part, bool justCreated)> EnsureParticipationAsync(
        AppDbContext context, IGameRepository gameRepo, Game game, Team team,
        UserInfo[] members, CancellationToken token)
    {
        var existing = await context.Participations
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.TeamId == team.Id && p.GameId == game.Id, token);
        if (existing is not null)
            return (existing, false);

        var part = new Participation
        {
            Status = ParticipationStatus.Accepted,
            Token = gameRepo.GetToken(game, team),
            TeamId = team.Id,
            GameId = game.Id
        };
        foreach (var user in members)
            part.Members.Add(new UserParticipation(user, game, team));

        await context.Participations.AddAsync(part, token);
        await context.SaveChangesAsync(token);
        return (part, true);
    }

    private static async Task<bool> EnsureSyntheticActivityAsync(
        AppDbContext context, Game game, GameChallenge staticChallenge,
        GameChallenge dynamicChallenge,
        Participation partAlpha, Participation partBravo,
        IReadOnlyDictionary<string, UserInfo> users, CancellationToken token)
    {
        if (await context.Submissions.AnyAsync(s => s.GameId == game.Id, token))
            return false;

        var now = DateTimeOffset.UtcNow;

        var acceptedSubmission = new Submission
        {
            Answer = StaticAttachmentFlag,
            Status = AnswerResult.Accepted,
            SubmitTimeUtc = now.AddMinutes(-30),
            UserId = users["User1"].Id,
            TeamId = partAlpha.TeamId,
            ParticipationId = partAlpha.Id,
            GameId = game.Id,
            ChallengeId = staticChallenge.Id
        };
        var wrongSubmission = new Submission
        {
            Answer = "flag{nope}",
            Status = AnswerResult.WrongAnswer,
            SubmitTimeUtc = now.AddMinutes(-20),
            UserId = users["User3"].Id,
            TeamId = partBravo.TeamId,
            ParticipationId = partBravo.Id,
            GameId = game.Id,
            ChallengeId = staticChallenge.Id
        };
        await context.Submissions.AddRangeAsync([acceptedSubmission, wrongSubmission], token);

        var accessAlpha = new ContainerAccessEvent
        {
            GameId = game.Id,
            ChallengeId = dynamicChallenge.Id,
            ContainerOwnerParticipationId = partAlpha.Id,
            ContainerId = Guid.NewGuid(),
            AccessingUserId = users["User1"].Id,
            AccessingUserName = users["User1"].UserName,
            AccessingParticipationId = partAlpha.Id,
            RemoteIp = "127.0.0.1",
            UserAgent = "dev-seed",
            ConnectedAtUtc = now.AddMinutes(-25)
        };
        var accessBravo = new ContainerAccessEvent
        {
            GameId = game.Id,
            ChallengeId = dynamicChallenge.Id,
            ContainerOwnerParticipationId = partBravo.Id,
            ContainerId = Guid.NewGuid(),
            AccessingUserId = users["User3"].Id,
            AccessingUserName = users["User3"].UserName,
            AccessingParticipationId = partBravo.Id,
            RemoteIp = "127.0.0.1",
            UserAgent = "dev-seed",
            ConnectedAtUtc = now.AddMinutes(-25)
        };
        await context.AddRangeAsync(new object[] { accessAlpha, accessBravo }, token);

        var suspicion = new SuspicionEvent
        {
            ParticipationId = partAlpha.Id,
            GameId = game.Id,
            Type = SuspicionType.SharedIP,
            ScoreDelta = 10,
            Details = "Seeded synthetic suspicion for dev UI",
            TimeUtc = now.AddMinutes(-15)
        };
        await context.AddAsync(suspicion, token);

        await context.SaveChangesAsync(token);
        return true;
    }
}
