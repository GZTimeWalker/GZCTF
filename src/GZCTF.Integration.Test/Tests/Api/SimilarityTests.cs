using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;
using GZCTF.Extensions;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class SimilarityTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task GetCheatReport_ShouldDetectTimeCorrelation()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Setup Game
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Similarity Game " + TestDataSeeder.RandomName());

        // 2. Setup Challenges (C1, C2, C3, C4)
        var c1 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "C1", "flag{1}");
        var c2 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "C2", "flag{2}");
        var c3 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "C3", "flag{3}");
        var c4 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "C4", "flag{4}");

        // 3. Setup Team A and Team B (Correlated)
        var u1 = await TestDataSeeder.CreateUserAsync(factory.Services, "UserA", "Test@123");
        var t1 = await TestDataSeeder.CreateTeamAsync(factory.Services, u1.Id, "Team A Correlated");
        var p1 = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t1.Id, u1.Id);

        var u2 = await TestDataSeeder.CreateUserAsync(factory.Services, "UserB", "Test@123");
        var t2 = await TestDataSeeder.CreateTeamAsync(factory.Services, u2.Id, "Team B Correlated");
        var p2 = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t2.Id, u2.Id);

        // 4. Submit Correlated Patterns
        // Pattern: Interval of 10s between solves
        var baseTime = DateTimeOffset.UtcNow.AddHours(-1);

        // Team A: T0, T0+10, T0+20, T0+30
        await AddSubmission(context, game.Id, c1.Id, t1.Id, u1.Id, p1.Id, baseTime);
        await AddSubmission(context, game.Id, c2.Id, t1.Id, u1.Id, p1.Id, baseTime.AddSeconds(10));
        await AddSubmission(context, game.Id, c3.Id, t1.Id, u1.Id, p1.Id, baseTime.AddSeconds(20));
        await AddSubmission(context, game.Id, c4.Id, t1.Id, u1.Id, p1.Id, baseTime.AddSeconds(30));

        // Team B: T1, T1+10, T1+20, T1+30 (Different start time, same intervals)
        var baseTimeB = baseTime.AddMinutes(30); 
        await AddSubmission(context, game.Id, c1.Id, t2.Id, u2.Id, p2.Id, baseTimeB);
        await AddSubmission(context, game.Id, c2.Id, t2.Id, u2.Id, p2.Id, baseTimeB.AddSeconds(10));
        await AddSubmission(context, game.Id, c3.Id, t2.Id, u2.Id, p2.Id, baseTimeB.AddSeconds(20));
        await AddSubmission(context, game.Id, c4.Id, t2.Id, u2.Id, p2.Id, baseTimeB.AddSeconds(30));

        await context.SaveChangesAsync();

        // 5. Monitor sets up
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

        Assert.NotNull(report);
        
        // Debug output
        foreach (var seq in report.SequenceSuspects)
        {
            output.WriteLine($"A: {seq.TeamA}, B: {seq.TeamB}, Sim: {seq.Similarity}, TimeCorr: {seq.TimeCorrelation}");
        }

        var suspect = report.SequenceSuspects.FirstOrDefault(s => 
            (s.TeamA == t1.Name && s.TeamB == t2.Name) || 
            (s.TeamA == t2.Name && s.TeamB == t1.Name));

        Assert.NotNull(suspect);
        Assert.Equal(1.0, suspect.Similarity, 3); // Exact sequence match
        Assert.True(suspect.TimeCorrelation > 0.99); // Highly correlated time intervals
    }

    private async Task AddSubmission(AppDbContext context, int gameId, int chalId, int teamId, Guid userId, int partId, DateTimeOffset time)
    {
        var sub = new Submission
        {
            GameId = gameId,
            ChallengeId = chalId,
            TeamId = teamId,
            ParticipationId = partId,
            UserId = userId,
            Answer = "flag",
            Status = AnswerResult.Accepted,
            SubmitTimeUtc = time
        };
        await context.Submissions.AddAsync(sub);
    }
}
