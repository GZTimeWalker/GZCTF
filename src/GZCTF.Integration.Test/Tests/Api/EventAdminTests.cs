using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Game;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class EventAdminTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task EventAdmin_ShouldBeAbleTo_ManageOwnGame()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Setup Data
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "EventAdmin Game " + TestDataSeeder.RandomName());
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.User);
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);

        // 2. Assign Event Admin
        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/Login", new { UserName = admin.UserName, Password = "Test@123" });
        var assignResponse = await adminClient.PostAsync($"/api/Edit/Games/{game.Id}/Admins/{user.Id}", null);
        assignResponse.EnsureSuccessStatusCode();

        // 3. User Login
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = user.UserName, Password = "Test@123" });

        // 4. Try to Update Game (Should Succeed)
        var updateModel = new GameInfoModel
        {
            Title = "Updated Title",
            StartTimeUtc = DateTimeOffset.UtcNow,
            EndTimeUtc = DateTimeOffset.UtcNow.AddDays(1),
            WriteupDeadline = DateTimeOffset.UtcNow.AddDays(2)
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/Edit/Games/{game.Id}", updateModel);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // 5. Try to Add Challenge (Should Succeed)
        var chalResponse = await client.PostAsJsonAsync($"/api/Edit/Games/{game.Id}/Challenges", new ChallengeInfoModel { Title = "New Chal", Type = ChallengeType.StaticAttachment, Category = ChallengeCategory.Misc });
        Assert.Equal(HttpStatusCode.OK, chalResponse.StatusCode);

        // 6. Try to Access Global Admin Endpoint (Should Fail)
        var postResponse = await client.PostAsJsonAsync("/api/Edit/Posts", new PostEditModel { Title = "Fail" });
        Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
    }

    [Fact]
    public async Task EventAdmin_ShouldNotAccess_OtherGames()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Setup Two Games
        var game1 = await TestDataSeeder.CreateGameAsync(factory.Services, "Game 1");
        var game2 = await TestDataSeeder.CreateGameAsync(factory.Services, "Game 2");
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.User);
        
        // 2. Make User Admin of Game 1
        context.EventManagers.Add(new EventManager { GameId = game1.Id, UserId = user.Id });
        await context.SaveChangesAsync();

        // 3. Login
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = user.UserName, Password = "Test@123" });

        // 4. Try to access Game 1 (Success)
        var res1 = await client.GetAsync($"/api/Edit/Games/{game1.Id}");
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        // 5. Try to access Game 2 (Forbidden)
        var res2 = await client.GetAsync($"/api/Edit/Games/{game2.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, res2.StatusCode);
    }
    [Fact]
    public async Task EventAdmin_Should_SeeOnlyAssignedGames()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Setup Data
        var g1 = await TestDataSeeder.CreateGameAsync(factory.Services, "My Game");
        var g2 = await TestDataSeeder.CreateGameAsync(factory.Services, "Other Game");
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.User);

        // 2. Assign User to G1
        context.EventManagers.Add(new EventManager { GameId = g1.Id, UserId = user.Id });
        await context.SaveChangesAsync();

        // 3. Login
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = user.UserName, Password = "Test@123" });

        // 4. Get Games List
        var response = await client.GetAsync("/api/Edit/Games");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var games = await response.Content.ReadFromJsonAsync<ArrayResponseDto<GameInfoModelDto>>();
        Assert.NotNull(games);
        
        // 5. Verify Only G1 is present
        Assert.Contains(games.Data, g => g.Id == g1.Id);
        Assert.DoesNotContain(games.Data, g => g.Id == g2.Id);
        Assert.Equal(1, games.Total);
    }

    private record ArrayResponseDto<T>(T[] Data, int Total);
    private record GameInfoModelDto(int Id);
}
