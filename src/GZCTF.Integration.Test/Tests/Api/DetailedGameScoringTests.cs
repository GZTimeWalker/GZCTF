using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Game;
using GZCTF.Services.Cache;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for detailed game and scoring scenarios
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class DetailedGameScoringTests(GZCTFApplicationFactory factory)
{
    /// <summary>
    /// Test multiple teams competing in the same game
    /// </summary>
    [Fact]
    public async Task MultipleTeams_ShouldCompete_InSameGame()
    {
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Multi-Team Competition", token: TestContext.Current.CancellationToken);
        var challenge1 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Challenge One", "flag{one}", token: TestContext.Current.CancellationToken);
        var challenge2 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Challenge Two", "flag{two}", token: TestContext.Current.CancellationToken);

        // Create 3 teams
        var password = "Multi@Team123";
        var teams = new List<(TestDataSeeder.SeededUser user, TestDataSeeder.SeededTeam team)>();

        for (var i = 1; i <= 3; i++)
        {
            var userName = TestDataSeeder.RandomName();
            var user = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, token: TestContext.Current.CancellationToken);
            var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {i}", token: TestContext.Current.CancellationToken);
            teams.Add((user, team));
        }

        // Each team joins and solves different challenges
        // Team 1: solves both
        using var client1 = factory.CreateClient();
        using (var loginResponse1 = await client1.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = teams[0].user.UserName, Password = password }, cancellationToken: TestContext.Current.CancellationToken))
        {
            loginResponse1.EnsureSuccessStatusCode();
        }

        using (var joinResponse1 = await client1.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = teams[0].team.Id }, cancellationToken: TestContext.Current.CancellationToken))
        {
            joinResponse1.EnsureSuccessStatusCode();
        }

        using (var submission1 = await client1.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge1.Id}", new FlagSubmitModel { Flag = "flag{one}" }, cancellationToken: TestContext.Current.CancellationToken))
        {
            submission1.EnsureSuccessStatusCode();
        }

        using (var submission2 = await client1.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge2.Id}", new FlagSubmitModel { Flag = "flag{two}" }, cancellationToken: TestContext.Current.CancellationToken))
        {
            submission2.EnsureSuccessStatusCode();
        }

        // Team 2: solves only challenge1
        using var client2 = factory.CreateClient();
        using (var loginResponse2 = await client2.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = teams[1].user.UserName, Password = password }, cancellationToken: TestContext.Current.CancellationToken))
        {
            loginResponse2.EnsureSuccessStatusCode();
        }

        using (var joinResponse2 = await client2.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = teams[1].team.Id }, cancellationToken: TestContext.Current.CancellationToken))
        {
            joinResponse2.EnsureSuccessStatusCode();
        }

        using (var submission2 = await client2.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge1.Id}", new FlagSubmitModel { Flag = "flag{one}" }, cancellationToken: TestContext.Current.CancellationToken))
        {
            submission2.EnsureSuccessStatusCode();
        }

        // Team 3: doesn't solve any
        using var client3 = factory.CreateClient();
        using (var loginResponse3 = await client3.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = teams[2].user.UserName, Password = password }, cancellationToken: TestContext.Current.CancellationToken))
        {
            loginResponse3.EnsureSuccessStatusCode();
        }

        using (var joinResponse3 = await client3.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = teams[2].team.Id }, cancellationToken: TestContext.Current.CancellationToken))
        {
            joinResponse3.EnsureSuccessStatusCode();
        }

        await FlushScoreboardCache(game.Id);

        // Verify scoreboard contains all teams
        var scoreboard = await WaitForScoreboard(client1, game.Id, expectedTeamCount: teams.Count);

        Assert.True(scoreboard.TryGetProperty("items", out var items));
        var scoreItems = items.EnumerateArray().ToArray();
        Assert.True(scoreItems.Length >= 3, $"Expected at least 3 teams, but got {scoreItems.Length}");

        // Verify all teams are present
        foreach (var (_, team) in teams)
        {
            Assert.Contains(scoreItems, item =>
                item.TryGetProperty("id", out var id) && id.GetInt32() == team.Id);
        }
    }

    /// <summary>
    /// Test game details structure and data completeness
    /// </summary>
    [Fact]
    public async Task GameDetails_ShouldContain_CompleteInformation()
    {
        const string password = "Game@Details123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Details Team", token: TestContext.Current.CancellationToken);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Detailed Game", token: TestContext.Current.CancellationToken);
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Test Challenge", "flag{test}", token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();

        using (var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = password }, cancellationToken: TestContext.Current.CancellationToken))
        {
            loginResponse.EnsureSuccessStatusCode();
        }

        using (var joinResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id }, cancellationToken: TestContext.Current.CancellationToken))
        {
            joinResponse.EnsureSuccessStatusCode();
        }

        // Get game details
        var detailsResponse = await client.GetAsync($"/api/Game/{game.Id}/Details", TestContext.Current.CancellationToken);
        detailsResponse.EnsureSuccessStatusCode();
        var details = await detailsResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);

        // Verify structure contains expected fields
        Assert.True(details.TryGetProperty("rank", out _), "Details should contain 'rank'");
        Assert.True(details.TryGetProperty("challenges", out _), "Details should contain 'challenges'");
        Assert.True(details.TryGetProperty("challengeCount", out _), "Details should contain 'challengeCount'");

        // Verify rank contains team information
        Assert.True(details.TryGetProperty("rank", out var rank));
        Assert.True(rank.TryGetProperty("id", out var teamId));
        Assert.Equal(team.Id, teamId.GetInt32());
        Assert.True(rank.TryGetProperty("name", out _), "Rank should contain team name");
    }

    /// <summary>
    /// Test game participation without joining should be restricted
    /// </summary>
    [Fact]
    public async Task GameAccess_WithoutJoining_ShouldBeRestricted()
    {
        var password = "Game@Access123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, token: TestContext.Current.CancellationToken);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Restricted Game", token: TestContext.Current.CancellationToken);
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Locked Challenge", "flag{locked}", token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();

        using (var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = password }, cancellationToken: TestContext.Current.CancellationToken))
        {
            loginResponse.EnsureSuccessStatusCode();
        }

        // Try to access challenge without joining game
        var challengeResponse = await client.GetAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}", TestContext.Current.CancellationToken);
        // Should fail because user hasn't joined the game
        Assert.Equal(HttpStatusCode.BadRequest, challengeResponse.StatusCode);
    }

    /// <summary>
    /// Test scoreboard pagination and filtering
    /// </summary>
    [Fact]
    public async Task Scoreboard_ShouldHandle_MultipleParticipants()
    {
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Scoreboard Test", token: TestContext.Current.CancellationToken);
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Scoreboard Challenge", "flag{scoreboard}", token: TestContext.Current.CancellationToken);

        const string password = "Scoreboard@Test123";
        var teams = new List<TestDataSeeder.SeededTeam>();

        // Create 5 teams
        for (var i = 1; i <= 5; i++)
        {
            var userName = TestDataSeeder.RandomName();
            var user = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, token: TestContext.Current.CancellationToken);
            var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Scoreboard Team {i}", token: TestContext.Current.CancellationToken);
            teams.Add(team);

            using var client = factory.CreateClient();
            using (var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = password }, cancellationToken: TestContext.Current.CancellationToken))
            {
                loginResponse.EnsureSuccessStatusCode();
            }

            using (var joinResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id }, cancellationToken: TestContext.Current.CancellationToken))
            {
                joinResponse.EnsureSuccessStatusCode();
            }
        }

        await FlushScoreboardCache(game.Id);

        // Retrieve scoreboard
        using var scoreboardClient = factory.CreateClient();
        var scoreboard = await WaitForScoreboard(scoreboardClient, game.Id, expectedTeamCount: teams.Count);

        Assert.True(scoreboard.TryGetProperty("items", out var items));
        var itemCount = items.EnumerateArray().Count();
        Assert.True(itemCount >= 5, $"Expected at least 5 teams on scoreboard, but got {itemCount}");
    }

    /// <summary>
    /// Test recent games API endpoint
    /// </summary>
    [Fact]
    public async Task RecentGames_ShouldReturn_GamesList()
    {
        // Create a recent game
        await TestDataSeeder.CreateGameAsync(factory.Services, "Recent Game Test", token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();

        // Get recent games (no auth required)
        var recentResponse = await client.GetAsync("/api/Game/Recent?limit=10", TestContext.Current.CancellationToken);
        recentResponse.EnsureSuccessStatusCode();
        var games = await recentResponse.Content.ReadFromJsonAsync<JsonElement[]>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(games);
        Assert.NotEmpty(games);

        // Verify the structure of game info
        var firstGame = games[0];
        Assert.True(firstGame.TryGetProperty("id", out _), "Game should have 'id'");
        Assert.True(firstGame.TryGetProperty("title", out _), "Game should have 'title'");
    }

    private async Task<JsonElement> WaitForScoreboard(
        HttpClient client,
        int gameId,
        int expectedTeamCount,
        int maxAttempts = 20,
        int delayMilliseconds = 100)
    {
        JsonElement scoreboard = default;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var response = await client.GetAsync($"/api/Game/{gameId}/Scoreboard");
            response.EnsureSuccessStatusCode();
            scoreboard = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (scoreboard.TryGetProperty("items", out var items))
            {
                var itemArray = items.EnumerateArray().ToArray();
                if (itemArray.Length >= expectedTeamCount)
                    return scoreboard;
            }

            await Task.Delay(delayMilliseconds);
        }

        Assert.Fail(
            $"Scoreboard for game {gameId} did not include {expectedTeamCount} teams after {maxAttempts} attempts.");
        return scoreboard;
    }

    private async Task FlushScoreboardCache(int gameId)
    {
        using var scope = factory.Services.CreateScope();
        var cacheHelper = scope.ServiceProvider.GetRequiredService<CacheHelper>();
        await cacheHelper.FlushScoreboardCache(gameId, CancellationToken.None);
    }

    /// <summary>
    /// Test games list with pagination
    /// </summary>
    [Fact]
    public async Task GamesList_ShouldSupport_Pagination()
    {
        using var client = factory.CreateClient();

        // Test pagination parameters
        var response = await client.GetAsync("/api/Game?count=5&skip=0", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.TryGetProperty("data", out var data));
        Assert.True(result.TryGetProperty("total", out var total));
        Assert.True(result.TryGetProperty("length", out var length));

        // Verify pagination fields exist
        var dataArray = data.EnumerateArray().ToArray();
        Assert.True(dataArray.Length <= 5, "Should respect count parameter");
    }

    /// <summary>
    /// Test challenge detail retrieval for different challenge types
    /// </summary>
    [Fact]
    public async Task ChallengeDetails_ShouldContain_RequiredFields()
    {
        const string password = "Challenge@Detail123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Detail Team", token: TestContext.Current.CancellationToken);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Challenge Detail Game", token: TestContext.Current.CancellationToken);
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Detailed Challenge", "flag{detailed}", token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();

        using (var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = password }, cancellationToken: TestContext.Current.CancellationToken))
        {
            loginResponse.EnsureSuccessStatusCode();
        }

        using (var joinResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team.Id }, cancellationToken: TestContext.Current.CancellationToken))
        {
            joinResponse.EnsureSuccessStatusCode();
        }

        // Get challenge details
        var detailResponse = await client.GetFromJsonAsync<ChallengeDetailModel>($"/api/Game/{game.Id}/Challenges/{challenge.Id}", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(detailResponse);
        Assert.Equal(challenge.Id, detailResponse.Id);
        Assert.Equal("Detailed Challenge", detailResponse.Title);
        Assert.Equal(ChallengeType.StaticAttachment, detailResponse.Type);
    }

    /// <summary>
    /// Test unauthorized access to game endpoints
    /// </summary>
    [Fact]
    public async Task GameEndpoints_ShouldEnforce_Authentication()
    {
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Auth Test Game", token: TestContext.Current.CancellationToken);
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Auth Challenge", "flag{auth}", token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();

        // Unauthenticated access to join game should fail
        var joinResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = 1 }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, joinResponse.StatusCode);

        // Unauthenticated access to submit flag should fail
        var submitResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}", new FlagSubmitModel { Flag = "flag{auth}" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, submitResponse.StatusCode);
    }
}
