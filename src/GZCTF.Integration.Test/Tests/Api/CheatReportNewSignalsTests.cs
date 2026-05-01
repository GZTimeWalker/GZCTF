using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class CheatReportNewSignalsTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    private JsonSerializerOptions GetJsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateTimeOffsetJsonConverter());
        options.Converters.Add(new IPAddressJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private Submission CreateSub(int gid, int cid, int tid, int pid, Guid uid, DateTimeOffset time) =>
        new Submission
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

    private Submission CreateWrongSub(int gid, int cid, int tid, int pid, Guid uid, DateTimeOffset time,
        string answer = "wrong") =>
        new Submission
        {
            GameId = gid,
            ChallengeId = cid,
            TeamId = tid,
            ParticipationId = pid,
            UserId = uid,
            Answer = answer,
            Status = AnswerResult.WrongAnswer,
            SubmitTimeUtc = time
        };

    [Fact]
    public async Task ShouldDetect_ZeroWrongAttempts_ForDynamicChallenge()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            "ZeroWrong Dynamic " + TestDataSeeder.RandomName());
        var chal = await TestDataSeeder.CreateDynamicChallengeAsync(factory.Services, game.Id,
            "ZWA Chal " + TestDataSeeder.RandomName());

        // Create 13 teams — all join
        var teams = new List<(int TeamId, int PartId, Guid UserId)>();
        for (var i = 0; i < 13; i++)
        {
            var u = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
            var t = await TestDataSeeder.CreateTeamAsync(factory.Services, u.Id,
                "ZWA Team " + TestDataSeeder.RandomName());
            var p = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t.Id, u.Id);
            teams.Add((t.Id, p.Id, u.Id));
        }

        var timeBase = DateTimeOffset.UtcNow.AddMinutes(-30);

        // Team 0 (suspicious): 0 wrong answers, 1 correct → ZeroWrongAttempts
        var (tid0, pid0, uid0) = teams[0];
        await context.Submissions.AddAsync(CreateSub(game.Id, chal.Id, tid0, pid0, uid0, timeBase.AddMinutes(1)));

        // Teams 1–4: 1 wrong before correct → should NOT trigger
        for (var i = 1; i <= 4; i++)
        {
            var (tid, pid, uid) = teams[i];
            await context.Submissions.AddAsync(
                CreateWrongSub(game.Id, chal.Id, tid, pid, uid, timeBase.AddMinutes(i * 5),
                    $"wrong_{i}_{TestDataSeeder.RandomName()}"));
            await context.Submissions.AddAsync(
                CreateSub(game.Id, chal.Id, tid, pid, uid, timeBase.AddMinutes(i * 5 + 2)));
        }

        // Teams 5–12: filler, no submissions
        await context.SaveChangesAsync();

        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123",
            role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login",
            new { UserName = admin.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves,
            s => s.TeamId == tid0 && s.Type == SuspicionType.ZeroWrongAttempts);
        for (var i = 1; i <= 4; i++)
        {
            var (tid, _, _) = teams[i];
            Assert.DoesNotContain(report.AbnormalSolves,
                s => s.TeamId == tid && s.Type == SuspicionType.ZeroWrongAttempts);
        }
    }

    [Fact]
    public async Task ShouldNotDetect_ZeroWrongAttempts_ForEasyChallenge()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            "ZeroWrong Easy " + TestDataSeeder.RandomName());
        var chal = await TestDataSeeder.CreateDynamicChallengeAsync(factory.Services, game.Id,
            "Easy ZWA Chal " + TestDataSeeder.RandomName());

        // Create 12 teams — all join
        var teams = new List<(int TeamId, int PartId, Guid UserId)>();
        for (var i = 0; i < 12; i++)
        {
            var u = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
            var t = await TestDataSeeder.CreateTeamAsync(factory.Services, u.Id,
                "Easy ZWA Team " + TestDataSeeder.RandomName());
            var p = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t.Id, u.Id);
            teams.Add((t.Id, p.Id, u.Id));
        }

        var timeBase = DateTimeOffset.UtcNow.AddMinutes(-30);

        // 5 teams solve with 0 wrong attempts → solve rate = 5/12 = 41.7% > 40% → IsChallengeEasy
        for (var i = 0; i < 5; i++)
        {
            var (tid, pid, uid) = teams[i];
            await context.Submissions.AddAsync(
                CreateSub(game.Id, chal.Id, tid, pid, uid, timeBase.AddMinutes(i + 1)));
        }

        await context.SaveChangesAsync();

        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123",
            role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login",
            new { UserName = admin.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.DoesNotContain(report.AbnormalSolves, s => s.Type == SuspicionType.ZeroWrongAttempts);
    }

    [Fact]
    public async Task ShouldDetect_WrongFlagLeakage()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            "WrongFlagLeakage " + TestDataSeeder.RandomName());
        var chal = await TestDataSeeder.CreateDynamicChallengeAsync(factory.Services, game.Id,
            "WFL Chal " + TestDataSeeder.RandomName());

        var uA = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tA = await TestDataSeeder.CreateTeamAsync(factory.Services, uA.Id,
            "WFL TeamA " + TestDataSeeder.RandomName());
        var pA = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tA.Id, uA.Id);

        var uB = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tB = await TestDataSeeder.CreateTeamAsync(factory.Services, uB.Id,
            "WFL TeamB " + TestDataSeeder.RandomName());
        var pB = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tB.Id, uB.Id);

        const string leakedFlag = "flag{secret_team_a_leakage}";

        // Create FlagContext for Team A's flag
        var flagCtx = new FlagContext { Flag = leakedFlag, ChallengeId = chal.Id };
        context.FlagContexts.Add(flagCtx);
        await context.SaveChangesAsync();

        // Associate with Team A's GameInstance
        var instance = await context.GameInstances.FirstAsync(i =>
            i.ParticipationId == pA.Id && i.ChallengeId == chal.Id);
        instance.FlagId = flagCtx.Id;
        await context.SaveChangesAsync();

        // Team B submits Team A's flag as a wrong answer
        await context.Submissions.AddAsync(
            CreateWrongSub(game.Id, chal.Id, tB.Id, pB.Id, uB.Id,
                DateTimeOffset.UtcNow.AddMinutes(-5), leakedFlag));
        await context.SaveChangesAsync();

        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123",
            role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login",
            new { UserName = admin.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves,
            s => s.TeamId == tB.Id && s.Type == SuspicionType.WrongFlagLeakage);
    }

    [Fact]
    public async Task ShouldDetect_SolutionRelay_WithConstantLag()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            "SolutionRelay " + TestDataSeeder.RandomName());

        var chals = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var c = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
                $"Relay Chal {i} " + TestDataSeeder.RandomName(),
                $"flag{{relay_{i}_{TestDataSeeder.RandomName()}}}");
            chals.Add(c.Id);
        }

        var uA = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tA = await TestDataSeeder.CreateTeamAsync(factory.Services, uA.Id,
            "Relay TeamA " + TestDataSeeder.RandomName());
        var pA = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tA.Id, uA.Id);

        var uB = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tB = await TestDataSeeder.CreateTeamAsync(factory.Services, uB.Id,
            "Relay TeamB " + TestDataSeeder.RandomName());
        var pB = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tB.Id, uB.Id);

        var gameStart = game.Start;

        // Team A solves at +10, +20, +30, +40, +50, +60 min
        // Team B solves at +15, +25, +35, +45, +55, +65 min (constant 5 min lag)
        for (var i = 0; i < 6; i++)
        {
            var tATime = gameStart.AddMinutes(10 + i * 10);
            var tBTime = gameStart.AddMinutes(15 + i * 10);
            await context.Submissions.AddAsync(
                CreateSub(game.Id, chals[i], tA.Id, pA.Id, uA.Id, tATime));
            await context.Submissions.AddAsync(
                CreateSub(game.Id, chals[i], tB.Id, pB.Id, uB.Id, tBTime));
        }

        await context.SaveChangesAsync();

        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123",
            role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login",
            new { UserName = admin.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.IpAnalysis,
            i => i.TeamId == tB.Id && i.Type == SuspicionType.SolutionRelay);
    }

    [Fact]
    public async Task ShouldNotDetect_SolutionRelay_WithVariableLag()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            "SolutionRelayVar " + TestDataSeeder.RandomName());

        var chals = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var c = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
                $"VarRelay Chal {i} " + TestDataSeeder.RandomName(),
                $"flag{{varrelay_{i}_{TestDataSeeder.RandomName()}}}");
            chals.Add(c.Id);
        }

        var uA = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tA = await TestDataSeeder.CreateTeamAsync(factory.Services, uA.Id,
            "VarRelay TeamA " + TestDataSeeder.RandomName());
        var pA = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tA.Id, uA.Id);

        var uB = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tB = await TestDataSeeder.CreateTeamAsync(factory.Services, uB.Id,
            "VarRelay TeamB " + TestDataSeeder.RandomName());
        var pB = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tB.Id, uB.Id);

        var gameStart = game.Start;

        // Team A solves at +10, +20, +30, +40, +50, +60 min
        var tAOffsets = new[] { 10, 20, 30, 40, 50, 60 };
        // Team B solves at +12, +55, +32, +75, +52, +65 min — lags: 2, 35, 2, 35, 2, 5 → stddev >> 5
        var tBOffsets = new[] { 12, 55, 32, 75, 52, 65 };

        for (var i = 0; i < 6; i++)
        {
            await context.Submissions.AddAsync(
                CreateSub(game.Id, chals[i], tA.Id, pA.Id, uA.Id, gameStart.AddMinutes(tAOffsets[i])));
            await context.Submissions.AddAsync(
                CreateSub(game.Id, chals[i], tB.Id, pB.Id, uB.Id, gameStart.AddMinutes(tBOffsets[i])));
        }

        await context.SaveChangesAsync();

        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123",
            role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login",
            new { UserName = admin.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.DoesNotContain(report.IpAnalysis, i => i.Type == SuspicionType.SolutionRelay);
    }

    [Fact]
    public async Task ShouldDetect_HighWrongRate_WhenNoBurstSolve()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            "HighWrongRate " + TestDataSeeder.RandomName());
        var chal = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "HWR Chal " + TestDataSeeder.RandomName(),
            $"flag{{hwr_{TestDataSeeder.RandomName()}}}");

        var u = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t = await TestDataSeeder.CreateTeamAsync(factory.Services, u.Id,
            "HWR Team " + TestDataSeeder.RandomName());
        var p = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t.Id, u.Id);

        var burstStart = DateTimeOffset.UtcNow.AddMinutes(-10);

        // 45 wrong answers within 30 seconds
        var wrongs = Enumerable.Range(0, 45)
            .Select(i => CreateWrongSub(game.Id, chal.Id, t.Id, p.Id, u.Id,
                burstStart.AddMilliseconds(i * 650),
                $"wrong_{i}_{TestDataSeeder.RandomName()}"))
            .ToList();

        await context.Submissions.AddRangeAsync(wrongs);
        await context.SaveChangesAsync();

        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123",
            role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login",
            new { UserName = admin.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves,
            s => s.TeamId == t.Id && s.Type == SuspicionType.HighWrongRate);
    }

    [Fact]
    public async Task ShouldNotDetect_HighWrongRate_WhenSolvedAfterBurst()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            "HighWrongRate Solved " + TestDataSeeder.RandomName());
        var chal = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "HWR Solved Chal " + TestDataSeeder.RandomName(),
            $"flag{{hwr_solved_{TestDataSeeder.RandomName()}}}");

        var u = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t = await TestDataSeeder.CreateTeamAsync(factory.Services, u.Id,
            "HWR Solved Team " + TestDataSeeder.RandomName());
        var p = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t.Id, u.Id);

        var burstStart = DateTimeOffset.UtcNow.AddMinutes(-10);

        // 45 wrong answers within 30 seconds
        var wrongs = Enumerable.Range(0, 45)
            .Select(i => CreateWrongSub(game.Id, chal.Id, t.Id, p.Id, u.Id,
                burstStart.AddMilliseconds(i * 650),
                $"wrong_s_{i}_{TestDataSeeder.RandomName()}"))
            .ToList();

        await context.Submissions.AddRangeAsync(wrongs);

        // Solved within 3 minutes of burst start → suppressed (< 5 min threshold)
        await context.Submissions.AddAsync(
            CreateSub(game.Id, chal.Id, t.Id, p.Id, u.Id, burstStart.AddMinutes(3)));

        await context.SaveChangesAsync();

        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123",
            role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login",
            new { UserName = admin.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.DoesNotContain(report.AbnormalSolves,
            s => s.TeamId == t.Id && s.Type == SuspicionType.HighWrongRate);
    }

    [Fact]
    public async Task ShouldDetect_AutomatedPattern_ForMachineSpeedSubmissions()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            "AutomatedPattern " + TestDataSeeder.RandomName());
        var chal = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "Auto Chal " + TestDataSeeder.RandomName(),
            $"flag{{auto_{TestDataSeeder.RandomName()}}}");

        var u = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t = await TestDataSeeder.CreateTeamAsync(factory.Services, u.Id,
            "Auto Team " + TestDataSeeder.RandomName());
        var p = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t.Id, u.Id);

        var baseTime = DateTimeOffset.UtcNow.AddMinutes(-15);

        // 11 wrong answers at exactly 1-second intervals → 10 intervals of 1.0s < 2.0s
        var wrongs = Enumerable.Range(0, 11)
            .Select(i => CreateWrongSub(game.Id, chal.Id, t.Id, p.Id, u.Id,
                baseTime.AddSeconds(i),
                $"auto_{i}_{TestDataSeeder.RandomName()}"))
            .ToList();

        await context.Submissions.AddRangeAsync(wrongs);
        await context.SaveChangesAsync();

        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123",
            role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login",
            new { UserName = admin.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves,
            s => s.TeamId == t.Id && s.Type == SuspicionType.AutomatedPattern);
    }

    [Fact]
    public async Task TierGating_SoftSignalDoesNotScore_WithoutStrongSignal()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            "TierGate NegSubnet " + TestDataSeeder.RandomName());

        var uA = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tA = await TestDataSeeder.CreateTeamAsync(factory.Services, uA.Id,
            "Subnet TeamA " + TestDataSeeder.RandomName());
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tA.Id, uA.Id);

        var uB = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tB = await TestDataSeeder.CreateTeamAsync(factory.Services, uB.Id,
            "Subnet TeamB " + TestDataSeeder.RandomName());
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tB.Id, uB.Id);

        // Both teams in same /28 subnet (192.168.0.0/28 covers .0–.15)
        var userA = await context.Users.FirstAsync(u => u.Id == uA.Id);
        userA.IP = IPAddress.Parse("192.168.0.1");

        var userB = await context.Users.FirstAsync(u => u.Id == uB.Id);
        userB.IP = IPAddress.Parse("192.168.0.14");

        await context.SaveChangesAsync();

        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123",
            role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login",
            new { UserName = admin.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.DoesNotContain(report.SuspicionList, r => r.TeamId == tA.Id);
        Assert.DoesNotContain(report.SuspicionList, r => r.TeamId == tB.Id);
        Assert.DoesNotContain(report.IpAnalysis,
            i => i.Type == SuspicionType.SubnetOverlap &&
                 (i.TeamId == tA.Id || i.TeamId == tB.Id));
    }

    [Fact]
    public async Task TierGating_SoftSignalScores_WhenStrongSignalPresent()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            "TierGate PosSubnet " + TestDataSeeder.RandomName());
        var chal = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "TierGate Chal " + TestDataSeeder.RandomName(),
            $"flag{{tiergating_{TestDataSeeder.RandomName()}}}");

        var uA = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tA = await TestDataSeeder.CreateTeamAsync(factory.Services, uA.Id,
            "TierGate TeamA " + TestDataSeeder.RandomName());
        var pA = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tA.Id, uA.Id);

        var uB = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tB = await TestDataSeeder.CreateTeamAsync(factory.Services, uB.Id,
            "TierGate TeamB " + TestDataSeeder.RandomName());
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tB.Id, uB.Id);

        // Same /28 subnet
        var userA = await context.Users.FirstAsync(u => u.Id == uA.Id);
        userA.IP = IPAddress.Parse("192.168.0.1");

        var userB = await context.Users.FirstAsync(u => u.Id == uB.Id);
        userB.IP = IPAddress.Parse("192.168.0.14");

        await context.SaveChangesAsync();

        // Team A submits 45 wrong answers quickly → triggers HighWrongRate (Strong signal)
        var burstStart = DateTimeOffset.UtcNow.AddMinutes(-20);
        var wrongs = Enumerable.Range(0, 45)
            .Select(i => CreateWrongSub(game.Id, chal.Id, tA.Id, pA.Id, uA.Id,
                burstStart.AddMilliseconds(i * 650),
                $"tg_wrong_{i}_{TestDataSeeder.RandomName()}"))
            .ToList();

        await context.Submissions.AddRangeAsync(wrongs);
        await context.SaveChangesAsync();

        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123",
            role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login",
            new { UserName = admin.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves,
            s => s.TeamId == tA.Id && s.Type == SuspicionType.HighWrongRate);
        Assert.Contains(report.IpAnalysis,
            i => i.TeamId == tA.Id && i.Type == SuspicionType.SubnetOverlap);
        Assert.Contains(report.SuspicionList, r => r.TeamId == tA.Id && r.Score > 0);
    }

    [Fact]
    public async Task ShouldDetect_AdaptiveFastSolve_ForHardChallenge()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var gameStart = DateTimeOffset.UtcNow.AddHours(-4);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            "AdaptiveFastSolve " + TestDataSeeder.RandomName(),
            start: gameStart);

        var chal = await TestDataSeeder.CreateDynamicChallengeAsync(factory.Services, game.Id,
            "AFS Chal " + TestDataSeeder.RandomName());

        // Second static challenge to give Team X a Strong signal (HighWrongRate)
        var chalStrong = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "AFS Strong Chal " + TestDataSeeder.RandomName(),
            $"flag{{afs_strong_{TestDataSeeder.RandomName()}}}");

        // 23 teams total: 14 filler + 8 community solvers + 1 suspicious (Team X)
        var fillerTeams = new List<(int TeamId, int PartId, Guid UserId)>();
        for (var i = 0; i < 14; i++)
        {
            var u = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
            var t = await TestDataSeeder.CreateTeamAsync(factory.Services, u.Id,
                "AFS Filler " + TestDataSeeder.RandomName());
            var p = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t.Id, u.Id);
            fillerTeams.Add((t.Id, p.Id, u.Id));
        }

        // 8 community teams solve at +90, +100, +110, +120, +130, +140, +150, +160 min
        // Median ≈ 125 min > 60 min threshold.
        // Each community team has 1 wrong answer before their correct one so that
        // zeroAttemptRate = 1/9 (only Team X) = 11% < 30% → challenge is NOT easy.
        var communityOffsets = new[] { 90, 100, 110, 120, 130, 140, 150, 160 };
        for (var i = 0; i < 8; i++)
        {
            var u = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
            var t = await TestDataSeeder.CreateTeamAsync(factory.Services, u.Id,
                "AFS Community " + TestDataSeeder.RandomName());
            var p = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t.Id, u.Id);
            // Wrong attempt 1 minute before the correct solve
            await context.Submissions.AddAsync(
                CreateWrongSub(game.Id, chal.Id, t.Id, p.Id, u.Id,
                    gameStart.AddMinutes(communityOffsets[i] - 1)));
            await context.Submissions.AddAsync(
                CreateSub(game.Id, chal.Id, t.Id, p.Id, u.Id,
                    gameStart.AddMinutes(communityOffsets[i])));
        }

        // Team X (suspicious): solves at +2 min (far below 5% of ~125 min = 6.25 min)
        var uX = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tX = await TestDataSeeder.CreateTeamAsync(factory.Services, uX.Id,
            "AFS SuspiciousX " + TestDataSeeder.RandomName());
        var pX = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tX.Id, uX.Id);

        await context.Submissions.AddAsync(
            CreateSub(game.Id, chal.Id, tX.Id, pX.Id, uX.Id, gameStart.AddMinutes(2)));

        // Team X also submits 45 wrong answers for the Strong signal challenge → HighWrongRate
        var burstStart = DateTimeOffset.UtcNow.AddMinutes(-30);
        var wrongs = Enumerable.Range(0, 45)
            .Select(i => CreateWrongSub(game.Id, chalStrong.Id, tX.Id, pX.Id, uX.Id,
                burstStart.AddMilliseconds(i * 650),
                $"afs_wrong_{i}_{TestDataSeeder.RandomName()}"))
            .ToList();

        await context.Submissions.AddRangeAsync(wrongs);
        await context.SaveChangesAsync();

        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123",
            role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login",
            new { UserName = admin.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves,
            s => s.TeamId == tX.Id && s.Type == SuspicionType.AdaptiveFastSolve);
    }

    [Fact]
    public async Task Performance_CheatReportHandlesLargeGame()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            "Perf Large Game " + TestDataSeeder.RandomName(),
            start: DateTimeOffset.UtcNow.AddHours(-2));

        // 15 challenges
        var chals = new List<int>();
        for (var i = 0; i < 15; i++)
        {
            var c = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
                $"Perf Chal {i} " + TestDataSeeder.RandomName(),
                $"flag{{perf_{i}_{TestDataSeeder.RandomName()}}}");
            chals.Add(c.Id);
        }

        // 30 teams
        var timeBase = DateTimeOffset.UtcNow.AddMinutes(-90);
        for (var t = 0; t < 30; t++)
        {
            var u = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
            var team = await TestDataSeeder.CreateTeamAsync(factory.Services, u.Id,
                "Perf Team " + TestDataSeeder.RandomName());
            var part = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, u.Id);

            // Each team solves 12 of 15 challenges with staggered times
            for (var c = 0; c < 12; c++)
            {
                await context.Submissions.AddAsync(
                    CreateSub(game.Id, chals[c], team.Id, part.Id, u.Id,
                        timeBase.AddMinutes(t * 2 + c * 3)));
            }
        }

        await context.SaveChangesAsync();

        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123",
            role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login",
            new { UserName = admin.UserName, Password = "Test@123" });

        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        sw.Stop();

        output.WriteLine($"Cheat report elapsed: {sw.ElapsedMilliseconds} ms");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10),
            $"Cheat report took {sw.Elapsed.TotalSeconds:F2}s, expected < 10s");

        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());
        Assert.NotNull(report);
        _ = report.SuspicionList;
    }
}
