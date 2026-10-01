using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Game;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class WriteupExampleTests(GZCTFApplicationFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    { Converters = { new DateTimeOffsetJsonConverter() } };

    private async Task<(HttpClient Admin, HttpClient Player, int GameId)> Setup()
    {
        const string password = "Example@Test123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password, role: Role.Admin);
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, TestDataSeeder.RandomName());
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Writeup samples test");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Games.FindAsync(game.Id);
            entity!.WriteupRequired = true;
            entity.WriteupDeadline = DateTimeOffset.UtcNow.AddHours(3);
            await db.SaveChangesAsync();
        }
        var adminClient = factory.CreateClient();
        var playerClient = factory.CreateClient();
        (await adminClient.PostAsJsonAsync("/api/account/login", new LoginModel { UserName = admin.UserName, Password = password })).EnsureSuccessStatusCode();
        (await playerClient.PostAsJsonAsync("/api/account/login", new LoginModel { UserName = user.UserName, Password = password })).EnsureSuccessStatusCode();
        (await playerClient.PostAsJsonAsync($"/api/game/{game.Id}", new GameJoinModel { TeamId = team.Id })).EnsureSuccessStatusCode();
        return (adminClient, playerClient, game.Id);
    }

    private static MultipartFormDataContent Form(string name, byte[] bytes, string contentType = "application/octet-stream")
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", name);
        return form;
    }

    private static async Task<WriteupExampleModel> Upload(HttpClient admin, int gameId, string name, byte[] bytes)
    {
        using var form = Form(name, bytes);
        using var response = await admin.PostAsync($"/api/admin/writeups/{gameId}/examples", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WriteupExampleModel>())!;
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".DOC")]
    [InlineData(".docx")]
    [InlineData(".odt")]
    public async Task UploadedDocument_IsListedAndDownloadableAfterGameEnds(string extension)
    {
        var (admin, player, gameId) = await Setup();
        using var adminScope = admin;
        using var playerScope = player;
        var name = "示例 # " + Guid.NewGuid() + extension;
        var bytes = Encoding.UTF8.GetBytes("Sample content " + Guid.NewGuid());
        var example = await Upload(admin, gameId, name, bytes);
        Assert.Equal(name, example.Name);
        Assert.Equal(bytes.Length, example.FileSize);

        // Writeup submission remains accessible after the contest ends.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Games.FindAsync(gameId))!.EndTimeUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        var info = await player.GetFromJsonAsync<BasicWriteupInfoModel>($"/api/game/{gameId}/writeup");
        Assert.False(info!.Submitted);
        Assert.Equal(example.Id, Assert.Single(info.Examples).Id);
        var adminInfo = await admin.GetFromJsonAsync<WriteupInfoModel>($"/api/admin/writeups/{gameId}", JsonOptions);
        Assert.Empty(adminInfo!.Writeups);
        Assert.Equal(name, Assert.Single(adminInfo.Examples).Name);

        using var download = await player.GetAsync(example.Url);
        download.EnsureSuccessStatusCode();
        Assert.Equal(bytes, await download.Content.ReadAsByteArrayAsync());
        Assert.Equal(name, download.Content.Headers.ContentDisposition!.FileNameStar);
    }

    [Fact]
    public async Task Examples_AreSeparateFromSubmittedWriteupsAndTheirArchive()
    {
        var (admin, player, gameId) = await Setup();
        using var adminScope = admin;
        using var playerScope = player;
        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4\nSample PDF " + Guid.NewGuid());
        var example = await Upload(admin, gameId, "sample.pdf", pdfBytes);
        await Upload(admin, gameId, "template.docx", Encoding.UTF8.GetBytes("Sample Word " + Guid.NewGuid()));
        using var form = Form("submission.pdf", pdfBytes, "application/pdf");
        (await player.PostAsync($"/api/game/{gameId}/writeup", form)).EnsureSuccessStatusCode();

        var info = await admin.GetFromJsonAsync<WriteupInfoModel>($"/api/admin/writeups/{gameId}", JsonOptions);
        Assert.Single(info!.Writeups);
        Assert.Equal(2, info.Examples.Count);
        using var archiveResponse = await admin.GetAsync($"/api/admin/writeups/{gameId}/all");
        archiveResponse.EnsureSuccessStatusCode();
        await using var archive = await archiveResponse.Content.ReadAsStreamAsync();
        await using var gzip = new GZipStream(archive, CompressionMode.Decompress);
        await using var tar = new TarReader(gzip);
        var entry = await tar.GetNextEntryAsync();
        Assert.NotNull(entry);
        Assert.StartsWith($"Writeup-{gameId}-", Path.GetFileName(entry.Name));
        Assert.Null(await tar.GetNextEntryAsync());

        (await admin.DeleteAsync($"/api/edit/games/{gameId}/writeups")).EnsureSuccessStatusCode();
        var cleared = await admin.GetFromJsonAsync<WriteupInfoModel>($"/api/admin/writeups/{gameId}", JsonOptions);
        Assert.Empty(cleared!.Writeups);
        Assert.Equal(2, cleared.Examples.Count);
        Assert.Equal("sample.pdf", cleared.Examples[0].Name);
        Assert.Equal(pdfBytes, await player.GetByteArrayAsync(example.Url));
    }

    [Fact]
    public async Task UploadingAnExampleWithSubmittedContent_DoesNotRenameTheSubmission()
    {
        var (admin, player, gameId) = await Setup();
        using var adminScope = admin;
        using var playerScope = player;
        var bytes = Encoding.UTF8.GetBytes("%PDF-1.4\nShared sample " + Guid.NewGuid());
        using var form = Form("submission.pdf", bytes, "application/pdf");
        (await player.PostAsync($"/api/game/{gameId}/writeup", form)).EnsureSuccessStatusCode();
        var original = await player.GetFromJsonAsync<BasicWriteupInfoModel>($"/api/game/{gameId}/writeup");
        var example = await Upload(admin, gameId, "template.pdf", bytes);
        var updated = await player.GetFromJsonAsync<BasicWriteupInfoModel>($"/api/game/{gameId}/writeup");
        Assert.Equal(original!.Name, updated!.Name);
        Assert.Equal("template.pdf", Assert.Single(updated.Examples).Name);

        (await admin.DeleteAsync($"/api/admin/writeups/{gameId}/examples/{example.Id}")).EnsureSuccessStatusCode();
        Assert.True((await player.GetFromJsonAsync<BasicWriteupInfoModel>($"/api/game/{gameId}/writeup"))!.Submitted);
        using var download = await player.GetAsync(example.Url);
        download.EnsureSuccessStatusCode();
        Assert.Equal(bytes, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Deletion_ReleasesSharedBlobReferencesAndPreservesEachOriginalName()
    {
        var (admin, player, gameId) = await Setup();
        using var adminScope = admin;
        using var playerScope = player;
        var otherGame = await TestDataSeeder.CreateGameAsync(factory.Services, "Other sample game");
        var bytes = Encoding.UTF8.GetBytes("Shared document " + Guid.NewGuid());
        var first = await Upload(admin, gameId, "first.pdf", bytes);
        var second = await Upload(admin, otherGame.Id, "second.pdf", bytes);
        var hash = first.Url.Split('/')[2];
        Assert.Equal(hash, second.Url.Split('/')[2]);
        var info = await player.GetFromJsonAsync<BasicWriteupInfoModel>($"/api/game/{gameId}/writeup");
        Assert.Equal("first.pdf", Assert.Single(info!.Examples).Name);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/writeups/{otherGame.Id}/examples/{first.Id}")).StatusCode);
        (await admin.DeleteAsync($"/api/admin/writeups/{gameId}/examples/{first.Id}")).EnsureSuccessStatusCode();
        Assert.Empty((await player.GetFromJsonAsync<BasicWriteupInfoModel>($"/api/game/{gameId}/writeup"))!.Examples);
        Assert.Equal(bytes, await player.GetByteArrayAsync(second.Url));
        await using (var scope = factory.Services.CreateAsyncScope())
            Assert.Equal(1u, (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Files.SingleAsync(f => f.Hash == hash)).ReferenceCount);

        // Deleting the second game must release its remaining reference as well.
        (await admin.DeleteAsync($"/api/edit/games/{otherGame.Id}")).EnsureSuccessStatusCode();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Files.AnyAsync(f => f.Hash == hash));
            Assert.False(await db.WriteupExamples.AnyAsync(e => e.GameId == otherGame.Id));
        }
        // Assets are normally cached for a week; bypass that cache to verify physical deletion.
        using var request = new HttpRequestMessage(HttpMethod.Get, second.Url);
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
        Assert.Equal(HttpStatusCode.NotFound, (await player.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task UploadAndDelete_RequireAdministratorAndAnExistingGame()
    {
        var (admin, player, gameId) = await Setup();
        using var adminScope = admin;
        using var playerScope = player;
        using var anonymous = factory.CreateClient();
        var bytes = Encoding.UTF8.GetBytes("Permission sample " + Guid.NewGuid());
        using var playerForm = Form("sample.pdf", bytes);
        Assert.Equal(HttpStatusCode.Forbidden, (await player.PostAsync($"/api/admin/writeups/{gameId}/examples", playerForm)).StatusCode);
        using var anonymousForm = Form("sample.pdf", bytes);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync($"/api/admin/writeups/{gameId}/examples", anonymousForm)).StatusCode);
        using var missingForm = Form("sample.pdf", bytes);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync("/api/admin/writeups/2147483647/examples", missingForm)).StatusCode);

        var example = await Upload(admin, gameId, "sample.pdf", bytes);
        Assert.Equal(HttpStatusCode.Forbidden, (await player.DeleteAsync($"/api/admin/writeups/{gameId}/examples/{example.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.DeleteAsync($"/api/admin/writeups/{gameId}/examples/{example.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/writeups/{gameId}/examples/2147483647")).StatusCode);
        (await admin.DeleteAsync($"/api/admin/writeups/{gameId}/examples/{example.Id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await player.GetAsync(example.Url)).StatusCode);
    }

    [Theory]
    [InlineData("sample.exe", 1)]
    [InlineData("empty.pdf", 0)]
    [InlineData("oversized.docx", 20 * 1024 * 1024 + 1)]
    public async Task InvalidUploads_DoNotCreateExamples(string name, int size)
    {
        var (admin, player, gameId) = await Setup();
        using var adminScope = admin;
        using var playerScope = player;
        using var form = Form(name, new byte[size]);
        using var response = await admin.PostAsync($"/api/admin/writeups/{gameId}/examples", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty((await player.GetFromJsonAsync<BasicWriteupInfoModel>($"/api/game/{gameId}/writeup"))!.Examples);
    }
}
