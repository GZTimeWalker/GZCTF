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
using System.Text.Json.Serialization;
using GZCTF.Extensions;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class CheatReportCompareTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    private JsonSerializerOptions GetJsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateTimeOffsetJsonConverter());
        options.Converters.Add(new IPAddressJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
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

    [Fact]
    public async Task Compare_ShouldReturnCorrectRSI_AndDetails()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Setup Game
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "CompareGame " + TestDataSeeder.RandomName());

        // 2. Setup Challenges
        var c1 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Chal 1", "flag{1}");
        var c2 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Chal 2", "flag{2}");
        var c3 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Chal 3", "flag{3}");

        // 3. Setup Teams
        var u1 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t1 = await TestDataSeeder.CreateTeamAsync(factory.Services, u1.Id, "Team A");
        var p1 = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t1.Id, u1.Id);

        var u2 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t2 = await TestDataSeeder.CreateTeamAsync(factory.Services, u2.Id, "Team B");
        var p2 = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t2.Id, u2.Id);

        // 4. Add Submissions
        // Team A: 1, 2, 3
        // Team B: 1, 3 (Missed 2)
        var timeBase = DateTimeOffset.UtcNow.AddHours(-1);

        // Team A Solves
        await context.Submissions.AddRangeAsync(
            CreateSub(game.Id, c1.Id, t1.Id, p1.Id, u1.Id, timeBase),
            CreateSub(game.Id, c2.Id, t1.Id, p1.Id, u1.Id, timeBase.AddMinutes(5)),
            CreateSub(game.Id, c3.Id, t1.Id, p1.Id, u1.Id, timeBase.AddMinutes(10))
        );

        // Team B Solves
        await context.Submissions.AddRangeAsync(
            CreateSub(game.Id, c1.Id, t2.Id, p2.Id, u2.Id, timeBase.AddSeconds(10)), // 10s diff
            CreateSub(game.Id, c3.Id, t2.Id, p2.Id, u2.Id, timeBase.AddMinutes(10).AddSeconds(20)) // 20s diff
        );

        await context.SaveChangesAsync();

        // 5. Monitor Login
        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        // 6. Call Compare Endpoint
        var url = $"/api/game/{game.Id}/cheatreport/compare?participationA={p1.Id}&participationB={p2.Id}";
        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
             var err = await response.Content.ReadAsStringAsync();
             output.WriteLine($"Error: {err}");
        }
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CollusionCompareResult>(GetJsonOptions());

        // 7. Verification
        Assert.NotNull(result);
        
        // Jaccard: Intersection(1,3) = 2. Union(1,2,3) = 3. Jaccard = 2/3 = 0.666...
        // LCS: SeqA=[1,2,3], SeqB=[1,3]. LCS=[1,3] (Len 2). MinLen=2. LCS Score = 2/2 = 1.0.
        // RSI = (2/3 * 0.7) + (1.0 * 0.3) = 0.4666... + 0.3 = 0.7666...
        
        Assert.True(result.RSI > 0.76 && result.RSI < 0.77, $"Expected RSI ~0.766, got {result.RSI}");
        Assert.Equal(2, result.Details.Count); // Should list Chal 1 and Chal 3
        
        var d1 = result.Details.FirstOrDefault(d => d.ChallengeName == "Chal 1");
        Assert.NotNull(d1);
        Assert.Equal(10, d1.TimeDiff, 1); // 10s diff +/- 1s precision

        var d2 = result.Details.FirstOrDefault(d => d.ChallengeName == "Chal 3");
        Assert.NotNull(d2);
        Assert.Equal(20, d2.TimeDiff, 1); // 20s diff +/- 1s precision
    }

    [Fact]
    public async Task GetCheatReport_ShouldReturnParticipationId_InCollusionGroups()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Setup Game
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "CollusionPID Game " + TestDataSeeder.RandomName());

        // 2. Setup Challenges
        var c1 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "C1", "flag{1}");
        var c2 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "C2", "flag{2}");
        var c3 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "C3", "flag{3}");

        // 3. Setup Teams and Participations
        // Team A (PID ?)
        var u1 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t1 = await TestDataSeeder.CreateTeamAsync(factory.Services, u1.Id, "Team X");
        var p1 = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t1.Id, u1.Id);

        // Team B (PID ?)
        var u2 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t2 = await TestDataSeeder.CreateTeamAsync(factory.Services, u2.Id, "Team Y");
        var p2 = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t2.Id, u2.Id);

        // 4. Create IDENTICAL Sequence to ensure 100% RSI and Trigger Collusion Detection
        var timeBase = DateTimeOffset.UtcNow.AddHours(-1);

        // Team A Solves
        await context.Submissions.AddRangeAsync(
            CreateSub(game.Id, c1.Id, t1.Id, p1.Id, u1.Id, timeBase),
            CreateSub(game.Id, c2.Id, t1.Id, p1.Id, u1.Id, timeBase.AddMinutes(5)),
            CreateSub(game.Id, c3.Id, t1.Id, p1.Id, u1.Id, timeBase.AddMinutes(10))
        );

        // Team B Solves (Same order, very close time)
        await context.Submissions.AddRangeAsync(
            CreateSub(game.Id, c1.Id, t2.Id, p2.Id, u2.Id, timeBase.AddSeconds(5)),
            CreateSub(game.Id, c2.Id, t2.Id, p2.Id, u2.Id, timeBase.AddMinutes(5).AddSeconds(5)),
            CreateSub(game.Id, c3.Id, t2.Id, p2.Id, u2.Id, timeBase.AddMinutes(10).AddSeconds(5))
        );

        await context.SaveChangesAsync();

        // 5. Monitor Admin Login
        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        // 6. Request Cheat Report
        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.NotEmpty(report.CollusionGroups);

        var group = report.CollusionGroups.First();
        Assert.Contains(group.Teams, t => t.Name == "Team X");
        Assert.Contains(group.Teams, t => t.Name == "Team Y");

        // 7. Verify ParticipationId is populated
        var teamXInfo = group.Teams.First(t => t.Name == "Team X");
        var teamYInfo = group.Teams.First(t => t.Name == "Team Y");

        Assert.Equal(p1.Id, teamXInfo.ParticipationId);
        Assert.Equal(p2.Id, teamYInfo.ParticipationId);
        
        Assert.NotEqual(0, teamXInfo.ParticipationId);
        Assert.NotEqual(0, teamYInfo.ParticipationId);
    }
}
