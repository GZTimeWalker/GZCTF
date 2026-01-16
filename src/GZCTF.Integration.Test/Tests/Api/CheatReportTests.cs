using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

using System.Text.Json;
using GZCTF.Extensions;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class CheatReportTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task GetCheatReport_ShouldNotCrash_WithDuplicateUserInTeams()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Setup Game
        var gameId = await TestDataSeeder.GetOrCreateBasicGameAsync(factory.Services);
        
        // 2. Setup Monitor User (Admin/Monitor)
        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        // 3. Setup Two Teams
        var captain1 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team1 = await TestDataSeeder.CreateTeamAsync(factory.Services, captain1.Id, "Team A " + TestDataSeeder.RandomName());
        
        var captain2 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team2 = await TestDataSeeder.CreateTeamAsync(factory.Services, captain2.Id, "Team B " + TestDataSeeder.RandomName());

        // 4. Setup "Cheater" User
        var cheater = await TestDataSeeder.CreateUserAsync(factory.Services, "apaseh_test", "Test@123");

        // 5. Force Add Cheater to BOTH Teams (Simulate the bug)
        var t1 = await context.Teams.Include(t => t.Members).FirstAsync(t => t.Id == team1.Id);
        var t2 = await context.Teams.Include(t => t.Members).FirstAsync(t => t.Id == team2.Id);
        var u = await context.Users.FirstAsync(u => u.Id == cheater.Id);

        t1.Members.Add(u);
        t2.Members.Add(u);
        
        // 6. Join Game
        await TestDataSeeder.JoinGameAsync(factory.Services, gameId, team1.Id, captain1.Id);
        await TestDataSeeder.JoinGameAsync(factory.Services, gameId, team2.Id, captain2.Id);
        
        await context.SaveChangesAsync();

        // 7. Request Cheat Report
        var response = await client.GetAsync($"/api/game/{gameId}/cheatreport");
        
        output.WriteLine($"Detailed Status: {response.StatusCode}");
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            output.WriteLine($"Error: {err}");
        }

        response.EnsureSuccessStatusCode();
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateTimeOffsetJsonConverter());
        options.Converters.Add(new IPAddressJsonConverter());
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(options);
        
        Assert.NotNull(report);
        // Ensure request completed without "ArgumentException: Key already added"
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectFastSolve()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Setup Game
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Fast Solve Game " + TestDataSeeder.RandomName());
        
        // 2. Setup Challenge
        var chal = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Fast Chal", "flag{fast}");

        // 3. Setup Team & Participation
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Speedrunners");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        // 4. Log "ChallengeOpened" Event at T
        var openTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        var solveTime = openTime.AddSeconds(5); // 5 seconds later -> Fast Solve (<20s)

        var eventRepo = scope.ServiceProvider.GetRequiredService<GZCTF.Repositories.Interface.IGameEventRepository>();
        // We can't use interface easily if it doesn't expose AddEvent with time.
        // Direct Context Add
        var openEvent = new GameEvent
        {
            GameId = game.Id,
            Type = EventType.ChallengeOpened,
            TeamId = team.Id,
            UserId = user.Id,
            PublishTimeUtc = openTime,
            Values = [chal.Id.ToString(), chal.Title]
        };
        await context.GameEvents.AddAsync(openEvent);

        // 5. Add Submission at T + 5s
        var submission = new Submission
        {
            GameId = game.Id,
            ChallengeId = chal.Id,
            TeamId = team.Id,
            ParticipationId = participation.Id,
            UserId = user.Id,
            Answer = "flag{fast}",
            Status = AnswerResult.Accepted,
            SubmitTimeUtc = solveTime
        };
        await context.Submissions.AddAsync(submission);
        await context.SaveChangesAsync();

        // 6. Monitor sets up
        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        // 7. Get Report
        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateTimeOffsetJsonConverter());
        options.Converters.Add(new IPAddressJsonConverter());
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(options);

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves, s => s.TeamId == team.Id && s.Type == "FastSolve");
    }
}
