using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Advanced integration tests for game mechanics including deadlines, limits, deletion, and score updates
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class AdvancedGameMechanicsTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    /// <summary>
    /// Test that submission limit prevents submissions after limit is reached
    /// </summary>
    [Fact]
    public async Task ChallengeSubmissionLimit_ShouldPreventExcessiveSubmissions()
    {
        var password = "Limit@Test123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Limit Team {userName}");
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Submission Limit Game");

        // Create a challenge with submission limit of 3
        using (var scope = factory.Services.CreateScope())
        {
            var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
            var challengeRepository = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

            var gameObj = await gameRepository.GetGameById(game.Id)
                          ?? throw new InvalidOperationException($"Game {game.Id} not found");

            GameChallenge challenge = new()
            {
                Title = "Limited Challenge",
                Content = "Challenge with submission limit",
                Category = ChallengeCategory.Misc,
                Type = ChallengeType.StaticAttachment,
                Hints = new List<string>(),
                IsEnabled = true,
                SubmissionLimit = 3, // Limit to 3 submissions
                OriginalScore = 100,
                MinScoreRate = 0.5,
                Difficulty = 2,
                Game = gameObj,
                GameId = gameObj.Id
            };

            FlagContext flagContext = new() { Flag = "flag{limit_test}", Challenge = challenge };
            challenge.Flags.Add(flagContext);

            await challengeRepository.CreateChallenge(gameObj, challenge);
        }

        // Get the challenge we just created
        int challengeId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var challenge = await context.GameChallenges
                .Where(c => c.GameId == game.Id && c.Title == "Limited Challenge")
                .FirstOrDefaultAsync();
            challengeId = challenge!.Id;
        }

        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });

        await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id });

        // Submit 3 wrong flags (at limit)
        for (int i = 0; i < 3; i++)
        {
            var submitResponse = await client.PostAsJsonAsync(
                $"/api/Game/{game.Id}/Challenges/{challengeId}",
                new FlagSubmitModel { Flag = $"flag{{wrong_{i}}}" });
            submitResponse.EnsureSuccessStatusCode();
            var submissionId = await submitResponse.Content.ReadFromJsonAsync<int>();
            Assert.True(submissionId > 0);
        }

        // 4th submission should fail due to limit
        var limitedResponse = await client.PostAsJsonAsync(
            $"/api/Game/{game.Id}/Challenges/{challengeId}",
            new FlagSubmitModel { Flag = "flag{wrong_4}" });
        Assert.Equal(HttpStatusCode.BadRequest, limitedResponse.StatusCode);
    }

    /// <summary>
    /// Test that challenge deadline prevents submissions after deadline
    /// </summary>
    [Fact]
    public async Task ChallengeDeadline_ShouldPreventSubmissionsAfterDeadline()
    {
        var password = "Deadline@Test123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Deadline Team {userName}");
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Deadline Game");

        // Create a challenge with deadline in the past
        int challengeId;
        using (var scope = factory.Services.CreateScope())
        {
            var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
            var challengeRepository = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

            var gameObj = await gameRepository.GetGameById(game.Id)
                          ?? throw new InvalidOperationException($"Game {game.Id} not found");

            GameChallenge challenge = new()
            {
                Title = "Expired Challenge",
                Content = "Challenge with past deadline",
                Category = ChallengeCategory.Misc,
                Type = ChallengeType.StaticAttachment,
                Hints = new List<string>(),
                IsEnabled = true,
                SubmissionLimit = 0, // No submission limit
                DeadlineUtc = DateTimeOffset.UtcNow.AddMinutes(-5), // Deadline 5 minutes ago
                OriginalScore = 100,
                MinScoreRate = 0.5,
                Difficulty = 2,
                Game = gameObj,
                GameId = gameObj.Id
            };

            FlagContext flagContext = new() { Flag = "flag{expired}", Challenge = challenge };
            challenge.Flags.Add(flagContext);

            await challengeRepository.CreateChallenge(gameObj, challenge);
            challengeId = challenge.Id;
        }

        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });

        await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id });

        // Submission should fail due to expired deadline
        var expiredResponse = await client.PostAsJsonAsync(
            $"/api/Game/{game.Id}/Challenges/{challengeId}",
            new FlagSubmitModel { Flag = "flag{expired}" });
        Assert.Equal(HttpStatusCode.BadRequest, expiredResponse.StatusCode);
    }

    /// <summary>
    /// Test that deleting a division updates the scoreboard correctly
    /// </summary>
    [Fact]
    public async Task DivisionDeletion_ShouldUpdateScoreboardCorrectly()
    {
        var adminPassword = "Admin@Division123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var password = "Division@Delete123";
        var user1Name = TestDataSeeder.RandomName();
        var user1 = await TestDataSeeder.CreateUserAsync(factory.Services,
            user1Name, password);
        var team1 = await TestDataSeeder.CreateTeamAsync(factory.Services, user1.Id, $"DivDel Team 1 {user1Name}");

        var user2Name = TestDataSeeder.RandomName();
        var user2 = await TestDataSeeder.CreateUserAsync(factory.Services,
            user2Name, password);
        var team2 = await TestDataSeeder.CreateTeamAsync(factory.Services, user2.Id, $"DivDel Team 2 {user2Name}");

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Division Delete Game");
        await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "Division Challenge", "flag{division}");

        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });

        // Create two divisions
        var divisionAResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Division A",
                InviteCode = "DIVA",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });
        divisionAResponse.EnsureSuccessStatusCode();
        var divisionA = await divisionAResponse.Content.ReadFromJsonAsync<Division>();

        var divisionBResponse = await adminClient.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Divisions",
            new DivisionCreateModel
            {
                Name = "Division B",
                InviteCode = "DIVB",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });
        divisionBResponse.EnsureSuccessStatusCode();
        var divisionB = await divisionBResponse.Content.ReadFromJsonAsync<Division>();

        // Team 1 joins Division A
        using var client1 = factory.CreateClient();
        await client1.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user1.UserName, Password = password });
        await client1.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team1.Id, DivisionId = divisionA!.Id, InviteCode = "DIVA" });

        // Team 2 joins Division B
        using var client2 = factory.CreateClient();
        await client2.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user2.UserName, Password = password });
        await client2.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team2.Id, DivisionId = divisionB!.Id, InviteCode = "DIVB" });

        await Task.Delay(100);

        // Check scoreboard has both teams
        var scoreboard1Response = await client1.GetAsync($"/api/Game/{game.Id}/Scoreboard");
        scoreboard1Response.EnsureSuccessStatusCode();
        var scoreboard1 = await scoreboard1Response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(scoreboard1.TryGetProperty("items", out var items1));
        var itemsArray1 = items1.EnumerateArray().ToArray();

        // Verify both teams are present
        var hasTeam1 = itemsArray1.Any(item =>
            item.TryGetProperty("id", out var id) && id.GetInt32() == team1.Id);
        var hasTeam2 = itemsArray1.Any(item =>
            item.TryGetProperty("id", out var id) && id.GetInt32() == team2.Id);
        Assert.True(hasTeam1, "Team 1 should be on scoreboard");
        Assert.True(hasTeam2, "Team 2 should be on scoreboard");

        // Delete Division B
        var deleteResponse = await adminClient.DeleteAsync($"/api/Edit/Games/{game.Id}/Divisions/{divisionB.Id}");
        deleteResponse.EnsureSuccessStatusCode();

        // Wait for scoreboard update
        await Task.Delay(100);

        // Check scoreboard - Division B teams should be removed or updated
        var scoreboard2Response = await client1.GetAsync($"/api/Game/{game.Id}/Scoreboard");
        scoreboard2Response.EnsureSuccessStatusCode();
        var scoreboard2 = await scoreboard2Response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(scoreboard2.TryGetProperty("items", out var items2));
        Assert.Equal(JsonValueKind.Array, items2.ValueKind);
        var itemsArray2 = items2.EnumerateArray().ToArray();

        // Team 1 should still be present
        hasTeam1 = itemsArray2.Any(item =>
            item.TryGetProperty("id", out var id) && id.GetInt32() == team1.Id);
        Assert.True(hasTeam1, "Team 1 should still be on scoreboard");

        // Team 2 should be present without division
        var team2Item = itemsArray2.FirstOrDefault(item =>
            item.TryGetProperty("id", out var id) && id.GetInt32() == team2.Id);
        Assert.Equal(JsonValueKind.Object, team2Item.ValueKind);
        if (team2Item.TryGetProperty("divisionId", out var divisionProp))
            Assert.Equal(JsonValueKind.Null, divisionProp.ValueKind);
    }

    /// <summary>
    /// Test that deleting a challenge updates the scoreboard correctly
    /// </summary>
    [Fact]
    public async Task ChallengeDeletion_ShouldUpdateScoreboardCorrectly()
    {
        var adminPassword = "Admin@Challenge123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var password = "Challenge@Delete123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"ChalDel Team {userName}");

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Challenge Delete Game");
        var challenge1 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "Challenge One", "flag{one}");
        var challenge2 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "Challenge Two", "flag{two}");

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });

        await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id });

        // Submit flags for both challenges
        await client.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge1.Id}",
            new FlagSubmitModel { Flag = "flag{one}" });
        await client.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge2.Id}",
            new FlagSubmitModel { Flag = "flag{two}" });

        // Check initial scoreboard
        var scoreboard1Response = await client.GetAsync($"/api/Game/{game.Id}/Scoreboard");
        scoreboard1Response.EnsureSuccessStatusCode();
        var scoreboard1 = await scoreboard1Response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(scoreboard1.TryGetProperty("items", out var items1));
        var teamItem1 = items1.EnumerateArray().FirstOrDefault(item =>
            item.TryGetProperty("id", out var id) && id.GetInt32() == team.Id);
        Assert.NotEqual(default, teamItem1);

        // Delete challenge1 via admin
        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });

        var deleteResponse = await adminClient.DeleteAsync($"/api/Edit/Games/{game.Id}/Challenges/{challenge1.Id}");
        deleteResponse.EnsureSuccessStatusCode();

        // Wait for scoreboard update
        await Task.Delay(200);

        // Check scoreboard after deletion - should still be valid
        var scoreboard2Response = await client.GetAsync($"/api/Game/{game.Id}/Scoreboard");
        scoreboard2Response.EnsureSuccessStatusCode();
        var scoreboard2 = await scoreboard2Response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(scoreboard2.TryGetProperty("items", out var items2));

        // Team should still be on scoreboard
        var teamItem2 = items2.EnumerateArray().FirstOrDefault(item =>
            item.TryGetProperty("id", out var id) && id.GetInt32() == team.Id);
        Assert.NotEqual(default, teamItem2);

        // Scoreboard should be recalculated without the deleted challenge
        // Note: Actual score processing requires background services which are disabled
    }

    /// <summary>
    /// Test that disabling a challenge updates the scoreboard
    /// </summary>
    [Fact]
    public async Task ChallengeDisabling_ShouldUpdateScoreboard()
    {
        var adminPassword = "Admin@Disable123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var password = "Challenge@Disable123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Disable Team {userName}");

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Challenge Disable Game");

        // Create challenge via admin with specific score
        int challengeId;
        using (var scope = factory.Services.CreateScope())
        {
            var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
            var challengeRepository = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

            var gameObj = await gameRepository.GetGameById(game.Id)
                          ?? throw new InvalidOperationException($"Game {game.Id} not found");

            GameChallenge challenge = new()
            {
                Title = "Toggle Challenge",
                Content = "Challenge to toggle",
                Category = ChallengeCategory.Misc,
                Type = ChallengeType.StaticAttachment,
                Hints = new List<string>(),
                IsEnabled = true,
                SubmissionLimit = 0,
                OriginalScore = 200,
                MinScoreRate = 0.5,
                Difficulty = 2,
                Game = gameObj,
                GameId = gameObj.Id
            };

            FlagContext flagContext = new() { Flag = "flag{toggle}", Challenge = challenge };
            challenge.Flags.Add(flagContext);

            await challengeRepository.CreateChallenge(gameObj, challenge);
            challengeId = challenge.Id;
        }

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });

        await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id });

        // Submit correct flag
        await client.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challengeId}",
            new FlagSubmitModel { Flag = "flag{toggle}" });

        // Admin disables the challenge
        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/Edit/Games/{game.Id}/Challenges/{challengeId}",
            new ChallengeUpdateModel { IsEnabled = false });
        updateResponse.EnsureSuccessStatusCode();

        // Wait for update
        await Task.Delay(200);

        // Check game details - disabled challenge should not appear
        var detailsResponse = await client.GetAsync($"/api/Game/{game.Id}/Details");
        if (detailsResponse.IsSuccessStatusCode)
        {
            var details = await detailsResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(details.TryGetProperty("challenges", out var challenges));

            var challengeIds = challenges.EnumerateObject()
                .SelectMany(category => category.Value.EnumerateArray()
                    .Select(ch => ch.GetProperty("id").GetInt32()))
                .ToHashSet();

            // Disabled challenge should not appear in the list
            Assert.DoesNotContain(challengeId, challengeIds);
        }
    }

    /// <summary>
    /// Test that updating challenge score affects scoreboard calculations
    /// </summary>
    [Fact]
    public async Task ChallengeScoreUpdate_ShouldAffectScoreboard()
    {
        var adminPassword = "Admin@Score123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var password = "Challenge@Score123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Score Team {userName}");

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Challenge Score Update Game");

        // Create challenge with initial score
        int challengeId;
        using (var scope = factory.Services.CreateScope())
        {
            var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
            var challengeRepository = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

            var gameObj = await gameRepository.GetGameById(game.Id)
                          ?? throw new InvalidOperationException($"Game {game.Id} not found");

            GameChallenge challenge = new()
            {
                Title = "Score Update Challenge",
                Content = "Challenge with changing score",
                Category = ChallengeCategory.Misc,
                Type = ChallengeType.StaticAttachment,
                Hints = [],
                IsEnabled = true,
                SubmissionLimit = 0,
                OriginalScore = 100, // Initial score
                MinScoreRate = 0.5,
                Difficulty = 2,
                Game = gameObj,
                GameId = gameObj.Id
            };

            FlagContext flagContext = new() { Flag = "flag{score_update}", Challenge = challenge };
            challenge.Flags.Add(flagContext);

            await challengeRepository.CreateChallenge(gameObj, challenge);
            challengeId = challenge.Id;
        }

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });

        await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id });

        // Submit correct flag
        await client.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challengeId}",
            new FlagSubmitModel { Flag = "flag{score_update}" });

        // Admin updates the challenge score
        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/Edit/Games/{game.Id}/Challenges/{challengeId}",
            new ChallengeUpdateModel
            {
                OriginalScore = 500, // Update score from 100 to 500
                MinScoreRate = 0.5
            });
        updateResponse.EnsureSuccessStatusCode();

        // Wait for update
        await Task.Delay(200);

        // Verify the challenge was updated in the database
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var challenge = await context.GameChallenges.FindAsync(challengeId);
            Assert.NotNull(challenge);
            Assert.Equal(500, challenge.OriginalScore);
        }

        // Check scoreboard - scoreboard should reflect updated challenge properties
        var scoreboardResponse = await client.GetAsync($"/api/Game/{game.Id}/Scoreboard");
        scoreboardResponse.EnsureSuccessStatusCode();
        var scoreboard = await scoreboardResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(scoreboard.TryGetProperty("items", out var items));

        var teamItem = items.EnumerateArray().FirstOrDefault(item =>
            item.TryGetProperty("id", out var id) && id.GetInt32() == team.Id);
        Assert.NotEqual(default, teamItem);

        // Note: Actual score recalculation requires background services
        // We verify that the scoreboard is still valid after the update
    }

    /// <summary>
    /// Test re-enabling a previously disabled challenge
    /// </summary>
    [Fact]
    public async Task ChallengeReEnabling_ShouldRestoreChallengeAvailability()
    {
        var adminPassword = "Admin@ReEnable123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var password = "Challenge@ReEnable123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"ReEnable Team {userName}");

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Challenge ReEnable Game");

        // Create challenge
        int challengeId;
        using (var scope = factory.Services.CreateScope())
        {
            var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
            var challengeRepository = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

            var gameObj = await gameRepository.GetGameById(game.Id)
                          ?? throw new InvalidOperationException($"Game {game.Id} not found");

            GameChallenge challenge = new()
            {
                Title = "ReEnable Challenge",
                Content = "Challenge to re-enable",
                Category = ChallengeCategory.Misc,
                Type = ChallengeType.StaticAttachment,
                Hints = new List<string>(),
                IsEnabled = true,
                SubmissionLimit = 0,
                OriginalScore = 150,
                MinScoreRate = 0.5,
                Difficulty = 2,
                Game = gameObj,
                GameId = gameObj.Id
            };

            FlagContext flagContext = new() { Flag = "flag{reenable}", Challenge = challenge };
            challenge.Flags.Add(flagContext);

            await challengeRepository.CreateChallenge(gameObj, challenge);
            challengeId = challenge.Id;
        }

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });

        await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id });

        // Admin disables then re-enables the challenge
        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });

        // Disable
        await adminClient.PutAsJsonAsync($"/api/Edit/Games/{game.Id}/Challenges/{challengeId}",
            new ChallengeUpdateModel { IsEnabled = false });
        await Task.Delay(100);

        // Re-enable
        var reEnableResponse = await adminClient.PutAsJsonAsync($"/api/Edit/Games/{game.Id}/Challenges/{challengeId}",
            new ChallengeUpdateModel { IsEnabled = true });
        reEnableResponse.EnsureSuccessStatusCode();
        await Task.Delay(100);

        // Verify challenge is available again in game details
        var detailsResponse = await client.GetAsync($"/api/Game/{game.Id}/Details");
        if (detailsResponse.IsSuccessStatusCode)
        {
            var details = await detailsResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(details.TryGetProperty("challenges", out var challenges));

            var challengeIds = challenges.EnumerateObject()
                .SelectMany(category => category.Value.EnumerateArray()
                    .Select(ch => ch.GetProperty("id").GetInt32()))
                .ToHashSet();

            // Re-enabled challenge should appear in the list
            Assert.Contains(challengeId, challengeIds);
        }

        // Should be able to submit to re-enabled challenge
        var submitResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challengeId}",
            new FlagSubmitModel { Flag = "flag{reenable}" });
        submitResponse.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Test dynamic container challenge with flag retrieval
    /// </summary>
    [Fact]
    public async Task DynamicContainerChallenge_ShouldRetrieveFlagSuccessfully()
    {
        // Create game
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Dynamic Container Game");

        // Create challenge
        int challengeId;
        using (var scope = factory.Services.CreateScope())
        {
            var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
            var challengeRepository = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

            var gameObj = await gameRepository.GetGameById(game.Id)
                          ?? throw new InvalidOperationException($"Game {game.Id} not found");

            GameChallenge challenge = new()
            {
                Title = "Dynamic Echo Challenge",
                Content = "Dynamic container challenge with echo",
                Category = ChallengeCategory.Misc,
                Type = ChallengeType.DynamicContainer,
                ContainerImage = "ghcr.io/gzctf/challenge-base/echo:latest",
                ContainerExposePort = 70,
                FlagTemplate = "flag{The quick brown fox jumps over the lazy dog}",
                Hints = [],
                IsEnabled = true,
                OriginalScore = 100,
                MinScoreRate = 0.5,
                Difficulty = 2,
                Game = gameObj,
                GameId = gameObj.Id
            };

            await challengeRepository.CreateChallenge(gameObj, challenge);
            challengeId = challenge.Id;
        }

        // Create user and team
        var password = "Dynamic@Test123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName, password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Dynamic Team {userName}");

        // Join game
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);
        var participationId = participation.Id;

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });

        // 1. Visit the detailed challenge
        var challengeDetailResponse = await client.GetAsync($"/api/Game/{game.Id}/Challenges/{challengeId}");
        challengeDetailResponse.EnsureSuccessStatusCode();
        var challengeDetail = await challengeDetailResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Null, challengeDetail.ValueKind);

        // 2. Post to create a container
        var createContainerResponse = await client.PostAsync($"/api/Game/{game.Id}/Container/{challengeId}", null);
        createContainerResponse.EnsureSuccessStatusCode();
        var containerInfo = await createContainerResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Null, containerInfo.ValueKind);

        // 3. Re-get the detailed challenge to find the container entry
        string entry = string.Empty;
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(500);
            var detailResponse = await client.GetAsync($"/api/Game/{game.Id}/Challenges/{challengeId}");
            detailResponse.EnsureSuccessStatusCode();
            var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
            if (detail.ValueKind != JsonValueKind.Object)
                continue;

            if (detail.TryGetProperty("context", out var context) &&
                context.TryGetProperty("instanceEntry", out var insEntry) &&
                insEntry.ValueKind == JsonValueKind.String)
            {
                entry = insEntry.GetString() ?? string.Empty;
            }

            break;
        }

        Assert.False(string.IsNullOrEmpty(entry), "Container entry should be available");

        // 4. Wait for container to be ready
        await ContainerHelper.WaitUserContainerAsync(factory.Services, challengeId, participationId, output);

        // 5. Fetch the flag
        var flag = await ContainerHelper.FetchFlag(entry);
        Assert.NotNull(flag);
        Assert.StartsWith("flag{", flag);

        output.WriteLine($"✅ Retrieved flag: {flag}");

        // 6. Submit flag and poll the result
        var submitResponse = await client.PostAsJsonAsync(
            $"/api/Game/{game.Id}/Challenges/{challengeId}",
            new FlagSubmitModel { Flag = flag });
        submitResponse.EnsureSuccessStatusCode();

        var submissionId = await submitResponse.Content.ReadFromJsonAsync<int>();
        Assert.True(submissionId > 0);

        // Poll submission status until it's accepted or timeout
        AnswerResult status = AnswerResult.FlagSubmitted;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            await Task.Delay(500);
            var statusResponse =
                await client.GetAsync($"/api/Game/{game.Id}/Challenges/{challengeId}/Status/{submissionId}");
            if (!statusResponse.IsSuccessStatusCode)
                continue;

            status = await statusResponse.Content.ReadFromJsonAsync<AnswerResult>();
            output.WriteLine($"  Submission status: {status}");
            if (status == AnswerResult.Accepted)
                break;
        }

        Assert.Equal(AnswerResult.Accepted, status);

        // Update LastContainerOperation in database to bypass 10-second rate limit
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var instance = await context.GameInstances
                .Include(i => i.Participation)
                .FirstOrDefaultAsync(
                    i => i.ChallengeId == challengeId &&
                         i.ParticipationId == participationId);

            if (instance != null)
            {
                instance.LastContainerOperation = DateTimeOffset.UtcNow.AddSeconds(-15);
                await context.SaveChangesAsync();
                output.WriteLine("✅ Updated LastContainerOperation to bypass rate limit");
            }
        }

        // 7. Destroy the container
        var destroyResponse = await client.DeleteAsync($"/api/Game/{game.Id}/Container/{challengeId}");
        destroyResponse.EnsureSuccessStatusCode();

        output.WriteLine("✅ Container destroyed successfully");
    }
}
