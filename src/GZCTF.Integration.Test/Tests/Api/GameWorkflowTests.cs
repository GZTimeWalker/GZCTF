using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Game;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// End-to-end workflow covering team participation in a game.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class GameWorkflowTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task User_Can_Join_Game_and_Interact_With_Challenge()
    {
        var password = "Pl@yHard#2025";
        var userName = TestDataSeeder.RandomName();
        var email = $"{userName}@example.com";

        var seededUser = await TestDataSeeder.CreateUserAsync(factory.Services, userName, email, password);
        var seededTeam = await TestDataSeeder.CreateTeamAsync(factory.Services, seededUser.Id, $"Team {userName}");
        var seededGame = await TestDataSeeder.CreateGameAsync(factory.Services, "Integration Game");
        var seededChallenge = await TestDataSeeder.CreateStaticChallengeAsync(
            factory.Services,
            seededGame.Id,
            "Warmup Binary",
            "flag{warmup}");

        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = seededUser.UserName, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        var profile = await client.GetFromJsonAsync<ProfileUserInfoModel>("/api/Account/Profile");
        Assert.NotNull(profile);
        Assert.Equal(seededUser.UserName, profile!.UserName);

        var joinResponse =
            await client.PostAsJsonAsync($"/api/Game/{seededGame.Id}", new GameJoinModel { TeamId = seededTeam.Id });
        joinResponse.EnsureSuccessStatusCode();

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/Game/{seededGame.Id}/Details");
        Assert.True(detail.ValueKind == JsonValueKind.Object);
        Assert.True(detail.TryGetProperty("rank", out var rankElement));
        Assert.Equal(seededTeam.Id, rankElement.GetProperty("id").GetInt32());
        Assert.True(detail.TryGetProperty("challengeCount", out var challengeCountElement));
        Assert.True(challengeCountElement.GetInt32() >= 1);

        Assert.True(detail.TryGetProperty("challenges", out var challengesElement));
        var challengeIds = challengesElement.EnumerateObject()
            .SelectMany(category => category.Value.EnumerateArray()
                .Select(ch => ch.GetProperty("id").GetInt32()))
            .ToArray();
        Assert.Contains(seededChallenge.Id, challengeIds);

        var challenge = await client.GetFromJsonAsync<ChallengeDetailModel>(
            $"/api/Game/{seededGame.Id}/Challenges/{seededChallenge.Id}");
        Assert.NotNull(challenge);
        Assert.Equal(seededChallenge.Id, challenge!.Id);
        Assert.Equal(ChallengeType.StaticAttachment, challenge.Type);

        var submitResponse = await client.PostAsJsonAsync(
            $"/api/Game/{seededGame.Id}/Challenges/{seededChallenge.Id}",
            new FlagSubmitModel { Flag = seededChallenge.Flag });
        submitResponse.EnsureSuccessStatusCode();

        var submissionId = await submitResponse.Content.ReadFromJsonAsync<int>();
        Assert.True(submissionId > 0);

        var statusResponse = await client.GetAsync(
            $"/api/Game/{seededGame.Id}/Challenges/{seededChallenge.Id}/Status/{submissionId}");
        statusResponse.EnsureSuccessStatusCode();
        var submissionStatus = await statusResponse.Content.ReadFromJsonAsync<AnswerResult>();
        Assert.True(submissionStatus == AnswerResult.FlagSubmitted || submissionStatus == AnswerResult.Accepted,
            $"Expected FlagSubmitted or Accepted, but got {submissionStatus}");

        var scoreboardResponse = await client.GetAsync($"/api/Game/{seededGame.Id}/Scoreboard");
        scoreboardResponse.EnsureSuccessStatusCode();
        var scoreboard = await scoreboardResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(scoreboard.TryGetProperty("items", out var items));
        Assert.Contains(items.EnumerateArray(), item =>
            item.TryGetProperty("id", out var idElement) && idElement.GetInt32() == seededTeam.Id);
    }
}
