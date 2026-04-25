using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Repositories.Interface;
using GZCTF.Storage.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for traffic capture functionality.
/// Verifies that when PlatformProxy + EnableTrafficCapture are enabled (local Docker mode),
/// connecting through the WebSocket proxy produces pcap files in blob storage.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class TrafficCaptureTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    /// <summary>
    /// End-to-end test: create a container challenge with traffic capture enabled,
    /// connect through the proxy, fetch the flag, then verify pcap files were persisted.
    /// </summary>
    [Fact]
    public async Task TrafficCapture_ShouldProducePcapFile_WhenProxyIsUsed()
    {
        // Arrange: admin creates a game + dynamic container challenge with traffic capture
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Traffic Capture Test");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gameEntity = await context.Games.FirstOrDefaultAsync(g => g.Id == game.Id);
        Assert.NotNull(gameEntity);

        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var challenge = await challengeRepo.CreateChallenge(gameEntity,
            new GameChallenge
            {
                Title = "Echo Capture",
                Type = ChallengeType.DynamicContainer,
                GameId = game.Id,
                IsEnabled = true,
                ContainerImage = "ghcr.io/gzctf/challenge-base/echo:latest",
                ExposePort = 70,
                MemoryLimit = 64,
                CPUCount = 1,
                StorageLimit = 256,
                EnableTrafficCapture = true
            }, CancellationToken.None);

        // Create a regular user + team, join the game
        var userPassword = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, userName, userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Capture Team {userName}");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });

        // Act: create the container
        var createResponse = await userClient.PostAsync($"/api/Game/{game.Id}/Container/{challenge.Id}", null);
        createResponse.EnsureSuccessStatusCode();

        // Wait for the container to be ready
        await ContainerHelper.WaitUserContainerAsync(factory.Services, challenge.Id, participation.Id, output);

        // Get the container entry (should be a GUID in PlatformProxy mode)
        string entry;
        {
            var detailResponse = await userClient.GetAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}");
            detailResponse.EnsureSuccessStatusCode();
            var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
            var ctx = detail.GetProperty("context");
            entry = ctx.GetProperty("instanceEntry").GetString()!;
        }

        Assert.NotEmpty(entry);
        output.WriteLine($"Container entry: {entry}");

        // The entry should be a GUID (PlatformProxy mode)
        Assert.True(Guid.TryParse(entry, out var containerId), "Entry should be a GUID in PlatformProxy mode");

        // Fetch the flag through the WebSocket proxy — this generates traffic
        var flag = await ContainerHelper.FetchFlag(entry, factory);
        Assert.NotNull(flag);
        Assert.StartsWith("flag{", flag);
        output.WriteLine($"Retrieved flag via proxy: {flag}");

        // Destroy the container — this triggers TrafficRecorderRegistry.RemoveAsync
        // which flushes the pcap file to storage
        var destroyResponse = await userClient.DeleteAsync($"/api/Game/{game.Id}/Container/{challenge.Id}");
        Assert.NotEqual(HttpStatusCode.NotFound, destroyResponse.StatusCode);

        // Allow a moment for async flush to complete
        await Task.Delay(1000);

        // Assert: verify pcap files were created in blob storage
        var storage = factory.Services.GetRequiredService<IBlobStorage>();
        var trafficDir = StoragePath.Combine("capture",
            challenge.Id.ToString(),
            participation.Id.ToString());

        var files = await storage.ListAsync(trafficDir);
        Assert.NotEmpty(files);

        var pcapFiles = files.Where(f => f.Name.EndsWith(".pcap")).ToList();
        Assert.NotEmpty(pcapFiles);

        foreach (var pcap in pcapFiles)
        {
            output.WriteLine($"Found pcap: {pcap.Name} ({pcap.Size} bytes)");
            Assert.True(pcap.Size > 0, $"pcap file {pcap.Name} should not be empty");

            // File name should contain the container short ID
            var shortId = containerId.ToString("N")[..12];
            Assert.Contains(shortId, pcap.Name);
        }

        output.WriteLine($"Total pcap files: {pcapFiles.Count}");
    }

    /// <summary>
    /// Verify that multiple connections to the same container produce a single (or few) pcap file(s)
    /// rather than one per connection, which was the motivation for the shared recorder design.
    /// </summary>
    [Fact]
    public async Task TrafficCapture_ShouldMergeConnections_IntoSharedPcapFile()
    {
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Merge Capture Test");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gameEntity = await context.Games.FirstOrDefaultAsync(g => g.Id == game.Id);
        Assert.NotNull(gameEntity);

        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var challenge = await challengeRepo.CreateChallenge(gameEntity,
            new GameChallenge
            {
                Title = "Merge Capture Challenge",
                Type = ChallengeType.DynamicContainer,
                GameId = game.Id,
                IsEnabled = true,
                ContainerImage = "ghcr.io/gzctf/challenge-base/echo:latest",
                ExposePort = 70,
                MemoryLimit = 64,
                CPUCount = 1,
                StorageLimit = 256,
                EnableTrafficCapture = true
            }, CancellationToken.None);

        var userPassword = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, userName, userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Merge Team {userName}");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });

        var createResponse = await userClient.PostAsync($"/api/Game/{game.Id}/Container/{challenge.Id}", null);
        createResponse.EnsureSuccessStatusCode();

        await ContainerHelper.WaitUserContainerAsync(factory.Services, challenge.Id, participation.Id, output);

        string entry;
        {
            var detailResponse = await userClient.GetAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}");
            detailResponse.EnsureSuccessStatusCode();
            var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
            var ctx = detail.GetProperty("context");
            entry = ctx.GetProperty("instanceEntry").GetString()!;
        }

        Assert.True(Guid.TryParse(entry, out var containerId));

        // Connect multiple times in quick succession — all should share the same pcap file
        const int connectionCount = 5;
        for (var i = 0; i < connectionCount; i++)
        {
            var result = await ContainerHelper.FetchFlag(entry, factory);
            Assert.NotNull(result);
            Assert.StartsWith("flag{", result);
        }

        output.WriteLine($"Completed {connectionCount} connections through proxy");

        // Destroy container to flush
        await userClient.DeleteAsync($"/api/Game/{game.Id}/Container/{challenge.Id}");
        await Task.Delay(1000);

        // Verify: should have significantly fewer pcap files than connection count
        var storage = factory.Services.GetRequiredService<IBlobStorage>();
        var trafficDir = StoragePath.Combine("capture",
            challenge.Id.ToString(),
            participation.Id.ToString());

        var files = await storage.ListAsync(trafficDir);
        var pcapFiles = files.Where(f => f.Name.EndsWith(".pcap")).ToList();

        Assert.NotEmpty(pcapFiles);

        // The old implementation would produce 5 files (one per connection).
        // The new shared recorder should produce far fewer — typically 1 since
        // all connections happen within the idle timeout window.
        output.WriteLine($"Pcap files for {connectionCount} connections: {pcapFiles.Count}");
        Assert.True(pcapFiles.Count < connectionCount,
            $"Expected fewer pcap files ({pcapFiles.Count}) than connections ({connectionCount}) " +
            "due to shared recorder merging");
    }
}
