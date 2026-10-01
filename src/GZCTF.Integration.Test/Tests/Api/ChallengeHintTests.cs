using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Edit;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class ChallengeHintTests(GZCTFApplicationFactory factory)
{
    private async Task<(HttpClient Admin, HttpClient Player, int GameId, int ChallengeId)> Setup(
        bool active = true, bool enabled = true)
    {
        const string password = "Hint@Test123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password,
            role: Role.Admin);
        var player = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, player.Id, TestDataSeeder.RandomName());
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Hint release test",
            start: active ? null : DateTimeOffset.UtcNow.AddHours(1));
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Hint challenge",
            "flag{hint_test}");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, player.Id);
        if (!enabled)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.GameChallenges.SingleAsync(c => c.Id == challenge.Id)).IsEnabled = false;
            await db.SaveChangesAsync();
        }
        var adminClient = factory.CreateClient();
        var playerClient = factory.CreateClient();
        (await adminClient.PostAsJsonAsync("/api/account/login",
            new LoginModel { UserName = admin.UserName, Password = password })).EnsureSuccessStatusCode();
        (await playerClient.PostAsJsonAsync("/api/account/login",
            new LoginModel { UserName = player.UserName, Password = password })).EnsureSuccessStatusCode();
        return (adminClient, playerClient, game.Id, challenge.Id);
    }

    private static string EditUrl(int gameId, int challengeId) => $"/api/edit/games/{gameId}/challenges/{challengeId}";
    private static string PlayerUrl(int gameId, int challengeId) => $"/api/game/{gameId}/challenges/{challengeId}";

    private async Task<int> NoticeCount(int gameId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.GameNotices.CountAsync(n => n.GameId == gameId && n.Type == NoticeType.NewHint);
    }

    private static async Task AssertHints(HttpClient player, int gameId, int challengeId, params string[] expected)
    {
        var response = await player.GetAsync(PlayerUrl(gameId, challengeId));
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expected, json.RootElement.GetProperty("hints").EnumerateArray().Select(h => h.GetString()));
        Assert.False(json.RootElement.TryGetProperty("hintEnabled", out _));
    }

    [Fact]
    public async Task HintLifecycle_HidesDraftsNotifiesOnReleaseAndDoesNotRepeatNotifications()
    {
        var (admin, player, gameId, challengeId) = await Setup();
        using (admin)
        using (player)
        {
            var url = EditUrl(gameId, challengeId);
            (await admin.PutAsJsonAsync(url, new ChallengeUpdateModel
                { Hints = ["first secret", "second secret"] })).EnsureSuccessStatusCode();
            await AssertHints(player, gameId, challengeId);
            Assert.Equal(0, await NoticeCount(gameId));

            var edit = await admin.GetFromJsonAsync<ChallengeEditDetailModel>(url);
            Assert.Equal(new[] { false, false }, edit!.HintEnabled);
            Assert.Equal(2, edit.Hints.Count);

            (await admin.PutAsJsonAsync(url, new { hintEnabled = new[] { true, false } })).EnsureSuccessStatusCode();
            await AssertHints(player, gameId, challengeId, "first secret");
            Assert.Equal(1, await NoticeCount(gameId));

            (await admin.PutAsJsonAsync(url, new { hintEnabled = new[] { true, false } })).EnsureSuccessStatusCode();
            (await admin.PutAsJsonAsync(url, new { hints = new[] { "first secret", "edited draft" },
                hintEnabled = new[] { true, false } })).EnsureSuccessStatusCode();
            Assert.Equal(1, await NoticeCount(gameId));
            await AssertHints(player, gameId, challengeId, "first secret");

            (await admin.PutAsJsonAsync(url, new { hintEnabled = new[] { false, false } })).EnsureSuccessStatusCode();
            await AssertHints(player, gameId, challengeId);
            Assert.Equal(1, await NoticeCount(gameId));

            (await admin.PutAsJsonAsync(url, new { hintEnabled = new[] { false, true } })).EnsureSuccessStatusCode();
            await AssertHints(player, gameId, challengeId, "edited draft");
            Assert.Equal(2, await NoticeCount(gameId));

            (await admin.PutAsJsonAsync(url, new { hints = new[] { "first secret", "updated released hint" },
                hintEnabled = new[] { false, true } })).EnsureSuccessStatusCode();
            await AssertHints(player, gameId, challengeId, "updated released hint");
            Assert.Equal(3, await NoticeCount(gameId));

            // Identical text does not prevent each newly enabled hint from being announced.
            (await admin.PutAsJsonAsync(url, new { hints = new[] { "updated released hint", "updated released hint" },
                hintEnabled = new[] { false, true } })).EnsureSuccessStatusCode();
            Assert.Equal(3, await NoticeCount(gameId));
            (await admin.PutAsJsonAsync(url, new { hintEnabled = new[] { true, true } })).EnsureSuccessStatusCode();
            await AssertHints(player, gameId, challengeId, "updated released hint", "updated released hint");
            Assert.Equal(4, await NoticeCount(gameId));
        }
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task InactiveGameOrDisabledChallenge_DoesNotAnnounceHints(bool active, bool enabled)
    {
        var (admin, player, gameId, challengeId) = await Setup(active, enabled);
        using (admin)
        using (player)
        {
            (await admin.PutAsJsonAsync(EditUrl(gameId, challengeId), new
                { hints = new[] { "scheduled hint" }, hintEnabled = new[] { true } })).EnsureSuccessStatusCode();
            Assert.Equal(0, await NoticeCount(gameId));
        }
    }

    [Fact]
    public async Task ReleaseSettings_RejectInvalidInputAndParticipantWrites()
    {
        var (admin, player, gameId, challengeId) = await Setup();
        using (admin)
        using (player)
        {
            var url = EditUrl(gameId, challengeId);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(url,
                new { hints = new[] { "secret" }, hintEnabled = Array.Empty<bool>() })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(url,
                new { hints = new[] { " " }, hintEnabled = new[] { true } })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(url,
                new { hintEnabled = new[] { true } })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await player.PutAsJsonAsync(url,
                new { hints = new[] { "unauthorized" }, hintEnabled = new[] { true } })).StatusCode);
            Assert.Equal(0, await NoticeCount(gameId));
            await AssertHints(player, gameId, challengeId);
        }
    }

    [Fact]
    public async Task LegacyDatabaseHints_KeepVisibilityAndPersistWithdrawal()
    {
        var (admin, player, gameId, challengeId) = await Setup();
        using (admin)
        using (player)
        {
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var challenge = await db.GameChallenges.SingleAsync(c => c.Id == challengeId);
                challenge.Hints = ["legacy visible hint"];
                challenge.HintEnabled = null;
                await db.SaveChangesAsync();
            }
            await AssertHints(player, gameId, challengeId, "legacy visible hint");
            (await admin.PutAsJsonAsync(EditUrl(gameId, challengeId), new
                { hintEnabled = new[] { false } })).EnsureSuccessStatusCode();
            await AssertHints(player, gameId, challengeId);
            using var verification = factory.Services.CreateScope();
            var persisted = await verification.ServiceProvider.GetRequiredService<AppDbContext>()
                .GameChallenges.AsNoTracking().SingleAsync(c => c.Id == challengeId);
            Assert.Equal(new[] { false }, persisted.HintEnabled);
        }
    }

    [Fact]
    public async Task SignalR_PublishesExistingNoticeAfterReleaseIsReadable()
    {
        var (admin, player, gameId, challengeId) = await Setup();
        using (admin)
        using (player)
        {
            using var socket = await factory.Server.CreateWebSocketClient()
                .ConnectAsync(new Uri($"ws://localhost/hub/user?game={gameId}"), CancellationToken.None);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await socket.SendAsync(Encoding.UTF8.GetBytes("{\"protocol\":\"json\",\"version\":1}\u001e"),
                WebSocketMessageType.Text, true, timeout.Token);
            var buffer = new byte[32768];
            await socket.ReceiveAsync(buffer, timeout.Token); // handshake

            (await admin.PutAsJsonAsync(EditUrl(gameId, challengeId), new
                { hints = new[] { "live hint" }, hintEnabled = new[] { true } })).EnsureSuccessStatusCode();
            var receivedNotice = false;
            var pending = "";
            while (!receivedNotice)
            {
                var result = await socket.ReceiveAsync(buffer, timeout.Token);
                Assert.NotEqual(WebSocketMessageType.Close, result.MessageType);
                pending += Encoding.UTF8.GetString(buffer, 0, result.Count);
                var frames = pending.Split('\u001e');
                pending = frames[^1];
                foreach (var frame in frames[..^1].Where(f => !string.IsNullOrWhiteSpace(f)))
                {
                    using var message = JsonDocument.Parse(frame);
                    if (!message.RootElement.TryGetProperty("target", out var target)) continue;
                    Assert.Equal("ReceivedGameNotice", target.GetString());
                    var notice = message.RootElement.GetProperty("arguments")[0];
                    Assert.Equal(nameof(NoticeType.NewHint), notice.GetProperty("type").GetString());
                    receivedNotice = true;
                    await AssertHints(player, gameId, challengeId, "live hint");
                }
            }
            Assert.Equal(1, await NoticeCount(gameId));

            (await admin.PutAsJsonAsync(EditUrl(gameId, challengeId), new
                { hintEnabled = new[] { false } })).EnsureSuccessStatusCode();
            await AssertHints(player, gameId, challengeId);
            Assert.Equal(1, await NoticeCount(gameId));
        }
    }
}
