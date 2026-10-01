using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class ChoiceExamTests(GZCTFApplicationFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    { Converters = { new DateTimeOffsetJsonConverter() } };

    private async Task<(HttpClient Admin, HttpClient Player, int GameId, int TeamId)> Setup()
    {
        const string password = "Choice@Test123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password, role: Role.Admin);
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, TestDataSeeder.RandomName());
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Choice test");
        var adminClient = factory.CreateClient();
        var client = factory.CreateClient();
        (await adminClient.PostAsJsonAsync("/api/account/login", new LoginModel { UserName = admin.UserName, Password = password })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/account/login", new LoginModel { UserName = user.UserName, Password = password })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/game/{game.Id}", new GameJoinModel { TeamId = team.Id })).EnsureSuccessStatusCode();
        var config = new ChoiceExamConfigModel
        {
            Enabled = true, SingleCount = 1, MultipleCount = 1, SingleScore = 5, MultipleScore = 10,
            Questions =
            [
                new() { Type = ChoiceQuestionType.Single, Content = "Single", Options = ["A", "B"], CorrectAnswers = [0] },
                new() { Type = ChoiceQuestionType.Multiple, Content = "Multiple", Options = ["A", "B", "C"], CorrectAnswers = [0, 2] }
            ]
        };
        (await adminClient.PutAsJsonAsync($"/api/game/{game.Id}/choice/config", config)).EnsureSuccessStatusCode();
        return (adminClient, client, game.Id, team.Id);
    }

    private static async Task<ChoiceAttemptModel> Read(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("correctAnswers", json);
        return JsonSerializer.Deserialize<ChoiceAttemptModel>(json, JsonOptions)!;
    }

    private static async Task<ChoiceAttemptModel> Complete(HttpClient client, string url)
    {
        var attempt = await Read(await client.PostAsync(url + "/attempt", null));
        attempt = await Read(await client.PutAsJsonAsync(url + "/answers/1", new ChoiceAnswerModel { Version = attempt.Version, SelectedOptions = [0] }));
        return await Read(await client.PutAsJsonAsync(url + "/answers/2", new ChoiceAnswerModel { Version = attempt.Version, SelectedOptions = [2, 0] }));
    }

    private static async Task<JsonElement> WaitForScoreboard(HttpClient client, int gameId, int teamId, int score)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (true)
        {
            using var response = await client.GetAsync($"/api/game/{gameId}/scoreboard");
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var item = json.RootElement.GetProperty("items").EnumerateArray()
                .FirstOrDefault(i => i.GetProperty("id").GetInt32() == teamId);
            if (item.ValueKind != JsonValueKind.Undefined && item.GetProperty("score").GetInt32() == score)
                return json.RootElement.Clone();

            if (DateTimeOffset.UtcNow >= deadline)
            {
                Assert.NotEqual(JsonValueKind.Undefined, item.ValueKind);
                Assert.Equal(score, item.GetProperty("score").GetInt32());
            }
            await Task.Delay(100);
        }
    }

    [Fact]
    public async Task CachedScoreboard_CombinesCtfAndChoiceScoresAndRanksByTheirTotal()
    {
        var (admin, client, gameId, teamId) = await Setup();
        using var adminScope = admin;
        using var playerScope = client;
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, gameId,
            "Fixed score challenge", "flag{combined_score}", originalScore: 100);
        (await admin.PutAsJsonAsync($"/api/edit/games/{gameId}/challenges/{challenge.Id}",
            new { minScoreRate = 1, disableBloodBonus = true, isEnabled = true })).EnsureSuccessStatusCode();

        const string password = "Scoreboard@Test123";
        var rival = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var rivalTeam = await TestDataSeeder.CreateTeamAsync(factory.Services, rival.Id, TestDataSeeder.RandomName());
        await TestDataSeeder.JoinGameAsync(factory.Services, gameId, rivalTeam.Id, rival.Id);
        using var rivalClient = factory.CreateClient();
        (await rivalClient.PostAsJsonAsync("/api/account/login",
            new LoginModel { UserName = rival.UserName, Password = password })).EnsureSuccessStatusCode();

        // Warm the HTTP scoreboard cache before either score changes.
        await WaitForScoreboard(client, gameId, teamId, 0);
        var flagUrl = $"/api/game/{gameId}/challenges/{challenge.Id}";
        (await rivalClient.PostAsJsonAsync(flagUrl, new FlagSubmitModel { Flag = challenge.Flag })).EnsureSuccessStatusCode();
        await WaitForScoreboard(client, gameId, rivalTeam.Id, 100);
        (await client.PostAsJsonAsync(flagUrl, new FlagSubmitModel { Flag = challenge.Flag })).EnsureSuccessStatusCode();
        var ctfBoard = await WaitForScoreboard(client, gameId, teamId, 100);
        var ctfItem = ctfBoard.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetInt32() == teamId);
        Assert.Equal(0, ctfItem.GetProperty("choiceScore").GetInt32());
        Assert.Equal(1, ctfItem.GetProperty("solvedCount").GetInt32());
        Assert.Equal(2, ctfItem.GetProperty("rank").GetInt32());

        var url = $"/api/game/{gameId}/choice";
        var attempt = await Complete(client, url);
        var draftBoard = await WaitForScoreboard(client, gameId, teamId, 100);
        Assert.Equal(0, draftBoard.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("id").GetInt32() == teamId).GetProperty("choiceScore").GetInt32());
        var final = await Read(await client.PostAsJsonAsync(url + "/submit", new ChoiceSubmitModel { Version = attempt.Version }));
        Assert.Equal(15, final.Score);

        var board = await WaitForScoreboard(client, gameId, teamId, 115);
        var winner = board.GetProperty("items")[0];
        Assert.Equal(teamId, winner.GetProperty("id").GetInt32());
        Assert.Equal(1, winner.GetProperty("rank").GetInt32());
        Assert.Equal(15, winner.GetProperty("choiceScore").GetInt32());
        Assert.Equal(1, winner.GetProperty("solvedCount").GetInt32());
        Assert.Equal(100, board.GetProperty("items")[1].GetProperty("score").GetInt32());
        var timeline = board.GetProperty("timelines").EnumerateArray()
            .Single(t => t.GetProperty("divisionId").GetInt32() == 0).GetProperty("teams").EnumerateArray()
            .Single(t => t.GetProperty("id").GetInt32() == teamId).GetProperty("items");
        Assert.Equal(115, timeline[timeline.GetArrayLength() - 1].GetProperty("score").GetInt32());

        await Read(await client.PostAsJsonAsync(url + "/submit", new ChoiceSubmitModel { Version = attempt.Version }));
        await WaitForScoreboard(client, gameId, teamId, 115);
    }

    [Fact]
    public async Task Workflow_RestoresLocksAndAddsFinalScoreOnlyOnce()
    {
        var (admin, client, gameId, teamId) = await Setup();
        using var adminScope = admin;
        using var playerScope = client;
        var url = $"/api/game/{gameId}/choice";
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync(url + "/attempt")).StatusCode);
        var attempt = await Read(await client.PostAsync(url + "/attempt", null));
        Assert.Null(attempt.Score);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url + "/submit", new ChoiceSubmitModel { Version = attempt.Version })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(url + "/config")).StatusCode);
        var config = await admin.GetFromJsonAsync<ChoiceExamConfigModel>(url + "/config");
        Assert.True(config!.Locked);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync(url + "/config", config)).StatusCode);

        attempt = await Complete(client, url);
        var restored = await Read(await client.GetAsync(url + "/attempt"));
        Assert.Equal(attempt.Version, restored.Version);
        Assert.Equal(new[] { 0, 2 }, restored.Answers[2]);
        Assert.Null(restored.Score);

        var final = await Read(await client.PostAsJsonAsync(url + "/submit", new ChoiceSubmitModel { Version = attempt.Version }));
        Assert.Equal(15, final.Score);
        var retry = await Read(await client.PostAsJsonAsync(url + "/submit", new ChoiceSubmitModel { Version = attempt.Version }));
        Assert.Equal(final.Version, retry.Version);
        Assert.Equal(final.SubmittedAt, retry.SubmittedAt);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(url + "/answers/1", new ChoiceAnswerModel { Version = final.Version, SelectedOptions = [1] })).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var game = await repo.GetGameById(gameId);
        var board = await repo.GenScoreboard(game!);
        Assert.Equal(15, board.Items[teamId].Score);
        Assert.Equal(15, board.Items[teamId].ChoiceScore);
        Assert.Equal(15, board.TimeLines[0].Single(t => t.Id == teamId).Items.Last().Score);
        var cachedBoard = await WaitForScoreboard(client, gameId, teamId, 15);
        var cachedItem = cachedBoard.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetInt32() == teamId);
        Assert.Equal(15, cachedItem.GetProperty("choiceScore").GetInt32());
        Assert.Equal(0, cachedItem.GetProperty("solvedCount").GetInt32());
    }

    [Fact]
    public async Task ConcurrentSaveAndSubmit_OnlyOneVersionCanWin()
    {
        var (admin, client, gameId, _) = await Setup();
        using var adminScope = admin;
        using var playerScope = client;
        var url = $"/api/game/{gameId}/choice";
        var attempt = await Complete(client, url);
        var responses = await Task.WhenAll(
            client.PutAsJsonAsync(url + "/answers/1", new ChoiceAnswerModel { Version = attempt.Version, SelectedOptions = [1] }),
            client.PostAsJsonAsync(url + "/submit", new ChoiceSubmitModel { Version = attempt.Version }));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        var restored = await Read(await client.GetAsync(url + "/attempt"));
        if (restored.SubmittedAt.HasValue) Assert.Equal(15, restored.Score);
        else
        {
            Assert.Equal(new[] { 1 }, restored.Answers[1]);
            var final = await Read(await client.PostAsJsonAsync(url + "/submit", new ChoiceSubmitModel { Version = restored.Version }));
            Assert.Equal(10, final.Score);
        }
    }

    [Fact]
    public async Task ConcurrentFirstStart_ReturnsTheSamePaperAndVersion()
    {
        var (admin, client, gameId, _) = await Setup();
        using var adminScope = admin;
        using var playerScope = client;
        var url = $"/api/game/{gameId}/choice/attempt";
        var responses = await Task.WhenAll(client.PostAsync(url, null), client.PostAsync(url, null));
        var first = await Read(responses[0]);
        var second = await Read(responses[1]);
        Assert.Equal(first.Version, second.Version);
        Assert.Equal(JsonSerializer.Serialize(first.Questions), JsonSerializer.Serialize(second.Questions));
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<AppDbContext>().ChoiceAttempts.CountAsync(a => a.GameId == gameId));
    }

    [Fact]
    public async Task OtherTeamsCannotReadDraftAndClosedGameRejectsWrites()
    {
        var (admin, client, gameId, _) = await Setup();
        using var adminScope = admin;
        using var playerScope = client;
        var url = $"/api/game/{gameId}/choice";
        var attempt = await Complete(client, url);
        // An authenticated nonparticipant cannot read or edit another team's paper.
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync(url + "/attempt")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync(url + "/answers/1", new ChoiceAnswerModel { Version = attempt.Version, SelectedOptions = [0] })).StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var game = await db.Games.FindAsync(gameId);
            game!.EndTimeUtc = DateTimeOffset.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(url + "/answers/1", new ChoiceAnswerModel { Version = attempt.Version, SelectedOptions = [1] })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url + "/submit", new ChoiceSubmitModel { Version = attempt.Version })).StatusCode);
        Assert.Equal(attempt.Version, (await Read(await client.GetAsync(url + "/attempt"))).Version);
    }

    [Fact]
    public async Task InvalidConfigImportIsAtomic()
    {
        var (admin, client, gameId, _) = await Setup();
        using var adminScope = admin;
        using var playerScope = client;
        var url = $"/api/game/{gameId}/choice/config";
        var config = await admin.GetFromJsonAsync<ChoiceExamConfigModel>(url);
        var version = config!.Version;
        config.Questions[1].CorrectAnswers = [0, 99];
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(url, config)).StatusCode);
        var unchanged = await admin.GetFromJsonAsync<ChoiceExamConfigModel>(url);
        Assert.Equal(version, unchanged!.Version);
        Assert.Equal(new[] { 0, 2 }, unchanged.Questions[1].CorrectAnswers);
    }
}
