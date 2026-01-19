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
        Assert.Contains(report.AbnormalSolves, s => s.TeamId == team.Id && s.Type == "FastSolve-Open");
    }

    [Fact]
    public async Task GetCheatReport_ShouldPopulateDetailedSolves_ForSuspiciousSequence()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Setup Game
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Sequence Game " + TestDataSeeder.RandomName());

        // 2. Setup Challenges
        var c1 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Chal 1", "flag{1}");
        var c2 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Chal 2", "flag{2}");
        var c3 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Chal 3", "flag{3}");

        // 3. Setup Teams
        var u1 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t1 = await TestDataSeeder.CreateTeamAsync(factory.Services, u1.Id, "Team A " + TestDataSeeder.RandomName());
        var p1 = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t1.Id, u1.Id);

        var u2 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t2 = await TestDataSeeder.CreateTeamAsync(factory.Services, u2.Id, "Team B " + TestDataSeeder.RandomName());
        var p2 = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t2.Id, u2.Id);

        // 4. Add Submissions (Same Sequence)
        var timeBase = DateTimeOffset.UtcNow.AddHours(-1);

        // Team A Solves
        await context.Submissions.AddRangeAsync(
            CreateSub(game.Id, c1.Id, t1.Id, p1.Id, u1.Id, timeBase),
            CreateSub(game.Id, c2.Id, t1.Id, p1.Id, u1.Id, timeBase.AddMinutes(5)),
            CreateSub(game.Id, c3.Id, t1.Id, p1.Id, u1.Id, timeBase.AddMinutes(10))
        );

        // Team B Solves (Same order, slightly different times)
        await context.Submissions.AddRangeAsync(
            CreateSub(game.Id, c1.Id, t2.Id, p2.Id, u2.Id, timeBase.AddSeconds(30)),
            CreateSub(game.Id, c2.Id, t2.Id, p2.Id, u2.Id, timeBase.AddMinutes(5).AddSeconds(30)),
            CreateSub(game.Id, c3.Id, t2.Id, p2.Id, u2.Id, timeBase.AddMinutes(10).AddSeconds(30))
        );

        await context.SaveChangesAsync();

        // 5. Monitor Login
        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        // 6. Get Report
        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateTimeOffsetJsonConverter());
        options.Converters.Add(new IPAddressJsonConverter());
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(options);

        // 7. Verify
        Assert.NotNull(report);
        Assert.NotEmpty(report.SequenceSuspects);
        var suspect = report.SequenceSuspects.FirstOrDefault();
        Assert.NotNull(suspect);
        Assert.Equal(3, suspect.CommonSolves);
        Assert.NotNull(suspect.DetailedSolves);
        Assert.Equal(3, suspect.DetailedSolves.Count);
        
        var detail = suspect.DetailedSolves.FirstOrDefault(d => d.ChallengeName == "Chal 1");
        Assert.NotNull(detail);
        Assert.True(detail.TimeDiff >= 29 && detail.TimeDiff <= 31); // expects ~30s
    }

    private Submission CreateSub(int gid, int cid, int tid, int pid, Guid uid, DateTimeOffset time)
    {
        return new Submission
        {
            GameId = gid,
            ChallengeId = cid,
            TeamId = tid,
            ParticipationId = pid,
            UserId = uid,
            Answer = "flag",
            Status = AnswerResult.Accepted,
            SubmitTimeUtc = time
        };
    }
}
