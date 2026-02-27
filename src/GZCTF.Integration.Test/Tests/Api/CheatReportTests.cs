using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Game;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GZCTF.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

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
        Assert.NotEmpty(report.CollusionGroups);
        var suspect = report.CollusionGroups.FirstOrDefault();
        Assert.NotNull(suspect);
        Assert.Equal(3, suspect.CommonSolves.Count);
        Assert.NotNull(suspect.DetailedSolves);
        Assert.Equal(3, suspect.DetailedSolves.Count);
        Assert.Contains(report.IpAnalysis, i => i.Type == SuspicionType.SequenceSimilarity && i.TeamId == t1.Id);
        Assert.Contains(report.IpAnalysis, i => i.Type == SuspicionType.SequenceSimilarity && i.TeamId == t2.Id);
        
        var detail = suspect.DetailedSolves.FirstOrDefault(d => d.ChallengeName == "Chal 1");
        Assert.NotNull(detail);
        Assert.True(detail.TimeDiff >= 29 && detail.TimeDiff <= 31); // expects ~30s
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectFastSolve_Download()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "FS Download " + TestDataSeeder.RandomName());
        var chalSeeded = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Att Chal", "flag{dl}");
        
        var chal = await context.GameChallenges.FindAsync(chalSeeded.Id);
        // Mock attachment
        var attachment = new Attachment 
        { 
            Type = FileType.Local, 
            LocalFile = new LocalFile 
            { 
               Name = "file.txt",
               FileSize = 100,
               Hash = "dummyhash"
            }
        };
        chal!.Attachment = attachment;
        await context.SaveChangesAsync();

        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Downloaders");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        var timeBase = DateTimeOffset.UtcNow.AddMinutes(-10);
        
        // Log Download at T
        await context.GameEvents.AddAsync(new GameEvent
        {
            GameId = game.Id,
            Type = EventType.Download,
            TeamId = team.Id,
            UserId = user.Id,
            PublishTimeUtc = timeBase,
            Values = [chal.Id.ToString(), chal.Type.ToString(), $"Download {chal.Title}.", "127.0.0.1"]
        });

        // Solve at T + 5s
        await context.Submissions.AddAsync(CreateSub(game.Id, chal.Id, team.Id, participation.Id, user.Id, timeBase.AddSeconds(5)));
        await context.SaveChangesAsync();

        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves, s => s.TeamId == team.Id && s.Type == "FastSolve-Download");
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectFastSolve_Container()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "FS Container " + TestDataSeeder.RandomName());
        // Create container challenge (Standard type usually implies container if configured, or DynamicContainer)
        var chalSeeded = await TestDataSeeder.CreateDynamicChallengeAsync(factory.Services, game.Id, "Cont Chal");
        var chal = await context.GameChallenges.FindAsync(chalSeeded.Id);
        
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "ContainerRunners");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        var timeBase = DateTimeOffset.UtcNow.AddMinutes(-10);
        
        // Log Container Start at T
        await context.GameEvents.AddAsync(new GameEvent
        {
            GameId = game.Id,
            Type = EventType.ContainerStart,
            TeamId = team.Id,
            UserId = user.Id,
            PublishTimeUtc = timeBase,
            Values = [chal!.Id.ToString()]
        });

        // Solve at T + 5s
        await context.Submissions.AddAsync(CreateSub(game.Id, chal.Id, team.Id, participation.Id, user.Id, timeBase.AddSeconds(5)));
        await context.SaveChangesAsync();

        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves, s => s.TeamId == team.Id && s.Type == "FastSolve-Container");
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectHoarding()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Hoarding Game " + TestDataSeeder.RandomName());
        var chalSeeded = await TestDataSeeder.CreateDynamicChallengeAsync(factory.Services, game.Id, "Hoard Chal");
        var chal = await context.GameChallenges.FindAsync(chalSeeded.Id);
        
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Hoarders");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        var start = DateTimeOffset.UtcNow.AddHours(-3);
        var destroy = start.AddMinutes(30);
        var solve = destroy.AddMinutes(61); // > 60 mins after destroy

        await context.GameEvents.AddRangeAsync(
            new GameEvent { GameId = game.Id, Type = EventType.ContainerStart, TeamId = team.Id, UserId = user.Id, PublishTimeUtc = start, Values = [chal!.Id.ToString()] },
            new GameEvent { GameId = game.Id, Type = EventType.ContainerDestroy, TeamId = team.Id, UserId = user.Id, PublishTimeUtc = destroy, Values = [chal!.Id.ToString()] }
        );

        await context.Submissions.AddAsync(CreateSub(game.Id, chal.Id, team.Id, participation.Id, user.Id, solve));
        await context.SaveChangesAsync();

        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves, s => s.TeamId == team.Id && s.Type == "Hoarding");
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectNoDownload()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "NoDL Game " + TestDataSeeder.RandomName());
        var chalSeeded = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Att Chal 2", "flag{nodl}");
        
        var chal = await context.GameChallenges.FindAsync(chalSeeded.Id);
        chal!.Attachment = new Attachment 
        { 
            Type = FileType.Local, 
            LocalFile = new LocalFile 
            { 
               Name = "file.txt",
               FileSize = 100,
               Hash = "dummyhash"
            }
        };
        await context.SaveChangesAsync();

        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Psychics");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        // Solve without download event
        await context.Submissions.AddAsync(CreateSub(game.Id, chal.Id, team.Id, participation.Id, user.Id, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();

        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves, s => s.TeamId == team.Id && s.Type == "NoDownload");
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectNoDownload_DynamicAttachment()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var blobService = scope.ServiceProvider.GetRequiredService<GZCTF.Repositories.Interface.IBlobRepository>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "NoDL DynamicAttachment " + TestDataSeeder.RandomName());
        var gameEntity = await context.Games.FirstAsync(g => g.Id == game.Id);

        using var ms = new MemoryStream(Guid.NewGuid().ToByteArray());
        var file = new Microsoft.AspNetCore.Http.FormFile(ms, 0, ms.Length, "file", "dyn-nodl.bin");
        var blob = await blobService.CreateOrUpdateBlob(file, "dyn-nodl.bin", CancellationToken.None);

        var challenge = new GameChallenge
        {
            Title = "Dynamic NoDownload",
            Content = "dynamic no download regression",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.DynamicAttachment,
            Hints = [],
            IsEnabled = true,
            SubmissionLimit = 0,
            OriginalScore = 1000,
            MinScoreRate = 0.8,
            Difficulty = 5,
            FileName = "dyn-nodl.bin",
            GameId = gameEntity.Id,
            Game = gameEntity
        };

        challenge.Flags.Add(new FlagContext
        {
            Flag = "flag{dynamic-nodl}",
            Attachment = new Attachment
            {
                Type = FileType.Local,
                LocalFile = blob,
                LocalFileId = blob.Id
            }
        });

        await context.GameChallenges.AddAsync(challenge);
        await context.SaveChangesAsync();

        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "DynPsychics");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        await context.Submissions.AddAsync(CreateSub(game.Id, challenge.Id, team.Id, participation.Id, user.Id, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();

        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves,
            s => s.TeamId == team.Id && s.ChallengeId == challenge.Id && s.Type == SuspicionType.NoDownload);
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectBurst()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Burst Game " + TestDataSeeder.RandomName());
        var c1 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Burst 1", "flag{1}");
        var c2 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Burst 2", "flag{2}");
        var c3 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Burst 3", "flag{3}");

        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Bursters");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        var timeBase = DateTimeOffset.UtcNow;
        // 3 solves in 10 seconds
        await context.Submissions.AddRangeAsync(
            CreateSub(game.Id, c1.Id, team.Id, participation.Id, user.Id, timeBase),
            CreateSub(game.Id, c2.Id, team.Id, participation.Id, user.Id, timeBase.AddSeconds(5)),
            CreateSub(game.Id, c3.Id, team.Id, participation.Id, user.Id, timeBase.AddSeconds(10))
        );
        await context.SaveChangesAsync();

        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.AbnormalSolves, s => s.TeamId == team.Id && s.Type == "Burst");
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectNoDownload_Container()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "NoDL Container " + TestDataSeeder.RandomName());
        
        // Dynamic Challenge usually means Container
        var chalSeeded = await TestDataSeeder.CreateDynamicChallengeAsync(factory.Services, game.Id, "Mix Chal");
        var chal = await context.GameChallenges.FindAsync(chalSeeded.Id);
        
        // Add attachment to Container challenge
        chal!.Attachment = new Attachment 
        { 
            Type = FileType.Local, 
            LocalFile = new LocalFile 
            { 
               Name = "source.zip",
               FileSize = 200,
               Hash = "sourcehash"
            }
        };
        await context.SaveChangesAsync();

        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "MixSolvers");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        var timeBase = DateTimeOffset.UtcNow;

        // Container Start Logged
        await context.GameEvents.AddAsync(new GameEvent
        {
            GameId = game.Id,
            Type = EventType.ContainerStart,
            TeamId = team.Id,
            UserId = user.Id,
            PublishTimeUtc = timeBase.AddMinutes(-5),
            Values = [chal.Id.ToString()]
        });

        // Solve WITHOUT Download Log
        await context.Submissions.AddAsync(CreateSub(game.Id, chal.Id, team.Id, participation.Id, user.Id, timeBase));
        await context.SaveChangesAsync();

        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        // This is expected to FAIL before the fix
        Assert.Contains(report.AbnormalSolves, s => s.TeamId == team.Id && s.Type == "NoDownload");
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectSharedIP()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "SharedIP Game " + TestDataSeeder.RandomName());
        
        var u1 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t1 = await TestDataSeeder.CreateTeamAsync(factory.Services, u1.Id, "Team A");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t1.Id, u1.Id);

        var u2 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t2 = await TestDataSeeder.CreateTeamAsync(factory.Services, u2.Id, "Team B");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t2.Id, u2.Id);

        // Simulate logs with same IP
        var ip = "192.168.1.100";
        var time = DateTimeOffset.UtcNow;
        
        await context.Logs.AddRangeAsync(
            new LogModel { Level = "Info", Logger = "AccountController", Message = "Login", TimeUtc = time, UserName = u1.UserName, RemoteIP = System.Net.IPAddress.Parse(ip) },
            new LogModel { Level = "Info", Logger = "AccountController", Message = "Login", TimeUtc = time.AddMinutes(1), UserName = u2.UserName, RemoteIP = System.Net.IPAddress.Parse(ip) }
        );
        await context.SaveChangesAsync();

        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.IpAnalysis, i => i.Type == "SharedIP" && i.Ip == ip);
    }

    [Fact]
    public async Task GetCheatReport_ShouldIgnoreLoginLogsAfterGameEnd()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "LateLogWindow Game " + TestDataSeeder.RandomName());
        var gameEntity = await context.Games.FirstAsync(g => g.Id == game.Id);
        gameEntity.PracticeMode = false;
        gameEntity.StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-4);
        gameEntity.EndTimeUtc = DateTimeOffset.UtcNow.AddHours(-2);
        await context.SaveChangesAsync();

        var u1 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t1 = await TestDataSeeder.CreateTeamAsync(factory.Services, u1.Id, "Window Team A");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t1.Id, u1.Id);

        var u2 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t2 = await TestDataSeeder.CreateTeamAsync(factory.Services, u2.Id, "Window Team B");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t2.Id, u2.Id);

        var sharedLateIp = "203.0.113.88";
        await context.Logs.AddRangeAsync(
            new LogModel
            {
                Level = "Info",
                Logger = "AccountController",
                Message = "Login",
                TimeUtc = gameEntity.StartTimeUtc.AddMinutes(5),
                UserName = u1.UserName,
                RemoteIP = IPAddress.Parse("10.31.0.1")
            },
            new LogModel
            {
                Level = "Info",
                Logger = "AccountController",
                Message = "Login",
                TimeUtc = gameEntity.StartTimeUtc.AddMinutes(10),
                UserName = u2.UserName,
                RemoteIP = IPAddress.Parse("10.31.0.2")
            },
            new LogModel
            {
                Level = "Info",
                Logger = "AccountController",
                Message = "Login",
                TimeUtc = gameEntity.EndTimeUtc.AddMinutes(5),
                UserName = u1.UserName,
                RemoteIP = IPAddress.Parse(sharedLateIp)
            },
            new LogModel
            {
                Level = "Info",
                Logger = "AccountController",
                Message = "Login",
                TimeUtc = gameEntity.EndTimeUtc.AddMinutes(6),
                UserName = u2.UserName,
                RemoteIP = IPAddress.Parse(sharedLateIp)
            }
        );
        await context.SaveChangesAsync();

        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.DoesNotContain(report.IpAnalysis, i => i.Type == SuspicionType.SharedIP && i.Ip == sharedLateIp);
    }

    [Fact]
    public async Task GetCheatReport_ShouldIncludeUserNames_InSharedFingerprintLog()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "SharedFingerprint Game " + TestDataSeeder.RandomName());

        var u1 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t1 = await TestDataSeeder.CreateTeamAsync(factory.Services, u1.Id, "Fp Team A");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t1.Id, u1.Id);

        var u2 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var t2 = await TestDataSeeder.CreateTeamAsync(factory.Services, u2.Id, "Fp Team B");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, t2.Id, u2.Id);

        var fingerprint = "fp-shared-123";
        var time = DateTimeOffset.UtcNow;

        await context.Logs.AddRangeAsync(
            new LogModel
            {
                Level = "Info",
                Logger = "AccountController",
                Message = "Login",
                TimeUtc = time,
                UserName = u1.UserName,
                RemoteIP = System.Net.IPAddress.Parse("10.11.0.1"),
                BrowserFingerprint = fingerprint
            },
            new LogModel
            {
                Level = "Info",
                Logger = "AccountController",
                Message = "Login",
                TimeUtc = time.AddMinutes(1),
                UserName = u2.UserName,
                RemoteIP = System.Net.IPAddress.Parse("10.11.0.2"),
                BrowserFingerprint = fingerprint
            }
        );
        await context.SaveChangesAsync();

        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);

        var teamARecord = report.IpAnalysis.FirstOrDefault(i => i.Type == "SharedFingerprint" && i.TeamId == t1.Id && i.Ip == fingerprint);
        Assert.NotNull(teamARecord);
        Assert.Contains(u1.UserName, teamARecord.UserNames);
        Assert.Contains(u2.UserName, teamARecord.RelatedUsers);

        var teamBRecord = report.IpAnalysis.FirstOrDefault(i => i.Type == "SharedFingerprint" && i.TeamId == t2.Id && i.Ip == fingerprint);
        Assert.NotNull(teamBRecord);
        Assert.Contains(u2.UserName, teamBRecord.UserNames);
        Assert.Contains(u1.UserName, teamBRecord.RelatedUsers);
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectTokenAbuse()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "TokenAbuse Game " + TestDataSeeder.RandomName());
        var chalSeeded = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Victim Chal", "flag{token}");
        
        var chal = await context.GameChallenges.FindAsync(chalSeeded.Id);
        chal!.Attachment = new Attachment 
        { 
            Type = FileType.Local, 
            LocalFile = new LocalFile { Name = "secret.txt", FileSize = 10, Hash = "secrethash" }
        };
        await context.SaveChangesAsync();
        
        // Victim Team
        var uVictim = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tVictim = await TestDataSeeder.CreateTeamAsync(factory.Services, uVictim.Id, "Victims");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tVictim.Id, uVictim.Id);

        // Attacker Team
        var uAttacker = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tAttacker = await TestDataSeeder.CreateTeamAsync(factory.Services, uAttacker.Id, "Attackers");
        var pAttacker = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tAttacker.Id, uAttacker.Id);

        var timeBase = DateTimeOffset.UtcNow.AddMinutes(-10);

        // Simulate Download by Attacker using Victim's token
        // AssetsController would log this with the abuse tag
        await context.GameEvents.AddAsync(new GameEvent
        {
            GameId = game.Id,
            Type = EventType.Download,
            TeamId = tAttacker.Id, // Attributed to Attacker
            UserId = uAttacker.Id,
            PublishTimeUtc = timeBase,
            Values = [
                chal.Id.ToString(), 
                "Attachment Download", 
                $"User {uAttacker.UserName} from team {tAttacker.Name} downloaded attachment for challenge {chal.Title}. [Token Source: Team {tVictim.Name}]", 
                "10.0.0.99"
            ]
        });

        // Attacker Solves
        await context.Submissions.AddAsync(CreateSub(game.Id, chal.Id, tAttacker.Id, pAttacker.Id, uAttacker.Id, timeBase.AddMinutes(5)));
        await context.SaveChangesAsync();

        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        var response = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        
        // 1. Verify Token Abuse is detected
        Assert.Contains(report.IpAnalysis, i => i.TeamId == tAttacker.Id && i.Type == "TokenAbuse" && i.Details.Contains(tVictim.Name));
        
        // 2. Verify NO "NoDownload" flag for Attacker (Logic Correctness Check)
        // Since the download was attributed to Attacker (despite using stolen token), they shouldn't be flagged for NoDownload.
        Assert.DoesNotContain(report.AbnormalSolves, s => s.TeamId == tAttacker.Id && s.Type == "NoDownload");
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectTokenAbuse_SecureToken()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dpProvider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var protector = dpProvider.CreateProtector("GZCTF.Assets.Download");

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "SecureToken Game " + TestDataSeeder.RandomName());
        var chalSeeded = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Secure Chal", "flag{secure}");
        
        var chal = await context.GameChallenges.FindAsync(chalSeeded.Id);
        // Ensure file hash exists in storage or mock, but for AssetsController logic we mainly need DB entry
        // To actuall call the API, the file must exist in IBlobStorage or we get 404.
        // We can mock blob storage or just create the file on disk if using LocalStorage.
        // For this test, verifying the LOGIC via DB insertion might be easier, but AssetsController is what writes to DB.
        // We will try to call the API but ensure IBlobStorage check passes? 
        // Actually, if file assumes missing, it returns NotFound, BUT does it log?
        // AssetsController: LogDownloadAsync is called AFTER storage.ExistsAsync check.
        // So we MUST ensure file "exists".
        // Integration tests use "LocalStorage" usually? Check appsettings or Startup.
        // Assuming we can just create the file.
        
        var fileHash = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789"; 
        chal!.Attachment = new Attachment 
        { 
            Type = FileType.Local, 
            LocalFile = new LocalFile 
            { 
               Name = "secure.txt", 
               FileSize = 10, 
               Hash = fileHash 
            }
        };
        await context.SaveChangesAsync();

        // Create dummy file on disk to pass ExistsAsync check
        // Storage path: Uploads/{hash[..2]}/{hash[2..4]}/{hash}
        // We need to know where PathHelper.Uploads points. usually ./uploads
        // Test environment might differ.
        // Alternate strategy: We can't easily mock storage in this full integration test without replacing service.
        // We can skip the API call and invoke LogDownloadAsync via reflection? No, too hacky.
        // We can just rely on the fact that if we use a REAL hash from a previous test helper (or CreateBlob), it works.
        // Let's use `blobService.CreateOrUpdateBlob` to create a real file.
        
        var blobService = scope.ServiceProvider.GetRequiredService<GZCTF.Repositories.Interface.IBlobRepository>();
        // Need a dummy file stream
        using var ms = new MemoryStream([1, 2, 3]);
        var formFile = new Microsoft.AspNetCore.Http.FormFile(ms, 0, 3, "file", "secure.txt");
        var blobRes = await blobService.CreateOrUpdateBlob(formFile, "secure.txt", CancellationToken.None);
        fileHash = blobRes.Hash;

        // Update challenge attachment with REAL hash
        chal.Attachment.LocalFile.Hash = fileHash;
        await context.SaveChangesAsync();

        // Victim Team
        var uVictim = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tVictim = await TestDataSeeder.CreateTeamAsync(factory.Services, uVictim.Id, "Victims");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tVictim.Id, uVictim.Id);

        // Attacker Team
        var uAttacker = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tAttacker = await TestDataSeeder.CreateTeamAsync(factory.Services, uAttacker.Id, "Attackers");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tAttacker.Id, uAttacker.Id);

        // Generate Secure Token for VICTIM
        var expiry = DateTimeOffset.UtcNow.AddHours(1).Ticks;
        var payload = $"v1|{fileHash}|{uVictim.Id}|{expiry}";
        var tokenBytes = protector.Protect(System.Text.Encoding.UTF8.GetBytes(payload));
        var secureToken = WebEncoders.Base64UrlEncode(tokenBytes);

        // Attacker Login
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = uAttacker.UserName, Password = "Test@123" });

        // Attacker accesses file with VICTIM'S token
        var url = $"/Assets/{fileHash}/s/{secureToken}/secure.txt";
        var fileResponse = await client.GetAsync(url);
        
        // Assert File Download Success (200)
        Assert.Equal(HttpStatusCode.OK, fileResponse.StatusCode);

        // Verify Event Log
        var evt = await context.GameEvents
            .Where(e => e.GameId == game.Id && e.Type == EventType.Download)
            .OrderByDescending(e => e.PublishTimeUtc)
            .FirstOrDefaultAsync();

        Assert.NotNull(evt);
        Assert.Equal(tAttacker.Id, evt.TeamId); // SHOULD be Attacker
        Assert.Equal(uAttacker.Id, evt.UserId); // SHOULD be Attacker (Not Victim!)
        Assert.NotNull(evt.Values);
        Assert.Contains("Token Source:", evt.Values[2]);
        Assert.Contains(uVictim.UserName, evt.Values[2]);
        Assert.Contains(tVictim.Name, evt.Values[2]);
        var metadata = ParseDownloadMetadata(evt);
        Assert.True(metadata.TokenAbuse);
        Assert.Equal("secure", metadata.TokenType);
        Assert.Equal(chal.Id, metadata.ChallengeId);
        Assert.Equal(tAttacker.Id, metadata.ActorTeamId);
        Assert.Equal(tVictim.Id, metadata.TokenSourceTeamId);
        Assert.Equal(tVictim.Name, metadata.TokenSourceTeamName);
        Assert.Equal(uVictim.UserName, metadata.TokenSourceUserName);

        // Verify Cheat Report
        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        // Relogin as admin
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });
        
        var reportResponse = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        reportResponse.EnsureSuccessStatusCode();
        var report = await reportResponse.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.IpAnalysis, i => i.TeamId == tAttacker.Id && i.Type == "TokenAbuse" && i.Details.Contains(tVictim.Name));
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectTokenAbuse_SecureToken_DynamicAttachment()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var blobService = scope.ServiceProvider.GetRequiredService<GZCTF.Repositories.Interface.IBlobRepository>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "DynamicTokenAbuse Game " + TestDataSeeder.RandomName());
        var gameEntity = await context.Games.FirstAsync(g => g.Id == game.Id);

        using var ms1 = new MemoryStream(Guid.NewGuid().ToByteArray());
        using var ms2 = new MemoryStream(Guid.NewGuid().ToByteArray());
        var file1 = new Microsoft.AspNetCore.Http.FormFile(ms1, 0, ms1.Length, "file", "dyn-1.bin");
        var file2 = new Microsoft.AspNetCore.Http.FormFile(ms2, 0, ms2.Length, "file", "dyn-2.bin");
        var blob1 = await blobService.CreateOrUpdateBlob(file1, "dyn-1.bin", CancellationToken.None);
        var blob2 = await blobService.CreateOrUpdateBlob(file2, "dyn-2.bin", CancellationToken.None);

        var challenge = new GameChallenge
        {
            Title = "Dynamic Secure Attachment",
            Content = "dynamic attachment test",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.DynamicAttachment,
            Hints = [],
            IsEnabled = true,
            SubmissionLimit = 0,
            OriginalScore = 1000,
            MinScoreRate = 0.8,
            Difficulty = 5,
            FileName = "dynamic.zip",
            GameId = gameEntity.Id,
            Game = gameEntity
        };

        challenge.Flags.Add(new FlagContext
        {
            Flag = "flag{dyn-1}",
            Attachment = new Attachment
            {
                Type = FileType.Local,
                LocalFile = blob1,
                LocalFileId = blob1.Id
            }
        });
        challenge.Flags.Add(new FlagContext
        {
            Flag = "flag{dyn-2}",
            Attachment = new Attachment
            {
                Type = FileType.Local,
                LocalFile = blob2,
                LocalFileId = blob2.Id
            }
        });

        await context.GameChallenges.AddAsync(challenge);
        await context.SaveChangesAsync();

        var uVictim = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tVictim = await TestDataSeeder.CreateTeamAsync(factory.Services, uVictim.Id, "DynVictims");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tVictim.Id, uVictim.Id);

        var uAttacker = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tAttacker = await TestDataSeeder.CreateTeamAsync(factory.Services, uAttacker.Id, "DynAttackers");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tAttacker.Id, uAttacker.Id);

        using var victimClient = factory.CreateClient();
        await victimClient.PostAsJsonAsync("/api/Account/Login", new { UserName = uVictim.UserName, Password = "Test@123" });
        var victimDetail = await victimClient.GetFromJsonAsync<ChallengeDetailModel>($"/api/Game/{game.Id}/Challenges/{challenge.Id}");
        var stolenUrl = victimDetail?.Context.Url;

        Assert.False(string.IsNullOrWhiteSpace(stolenUrl));
        Assert.Contains("/assets/", stolenUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/s/", stolenUrl, StringComparison.OrdinalIgnoreCase);

        using var attackerClient = factory.CreateClient();
        await attackerClient.PostAsJsonAsync("/api/Account/Login", new { UserName = uAttacker.UserName, Password = "Test@123" });
        var downloadResponse = await attackerClient.GetAsync(stolenUrl);
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);

        context.ChangeTracker.Clear();
        var downloadEvents = await context.GameEvents
            .Where(e => e.GameId == game.Id && e.Type == EventType.Download)
            .OrderByDescending(e => e.PublishTimeUtc)
            .ToListAsync();

        var evt = downloadEvents
            .FirstOrDefault(e => e.Values is { Count: > 0 } && e.Values[0] == challenge.Id.ToString());

        Assert.NotNull(evt);
        Assert.Equal(tAttacker.Id, evt.TeamId);
        Assert.Equal(uAttacker.Id, evt.UserId);
        Assert.NotNull(evt.Values);
        Assert.Contains("Token Source:", evt.Values[2]);
        Assert.Contains(uVictim.UserName, evt.Values[2]);
        Assert.Contains(tVictim.Name, evt.Values[2]);
        var metadata = ParseDownloadMetadata(evt);
        Assert.True(metadata.TokenAbuse);
        Assert.Equal("secure", metadata.TokenType);
        Assert.Equal(challenge.Id, metadata.ChallengeId);
        Assert.Equal(tAttacker.Id, metadata.ActorTeamId);
        Assert.Equal(tVictim.Id, metadata.TokenSourceTeamId);
        Assert.Equal(tVictim.Name, metadata.TokenSourceTeamName);
        Assert.Equal(uVictim.UserName, metadata.TokenSourceUserName);

        using var monitorClient = factory.CreateClient();
        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        await monitorClient.PostAsJsonAsync("/api/Account/Login", new { UserName = monitorUser.UserName, Password = "Test@123" });

        var reportResponse = await monitorClient.GetAsync($"/api/game/{game.Id}/cheatreport");
        reportResponse.EnsureSuccessStatusCode();
        var report = await reportResponse.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        Assert.Contains(report.IpAnalysis, i => i.TeamId == tAttacker.Id && i.Type == "TokenAbuse" && i.Details.Contains(tVictim.Name));
    }

    [Fact]
    public async Task AttachmentDownload_ShouldRejectInvalidSecureToken()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var blobService = scope.ServiceProvider.GetRequiredService<GZCTF.Repositories.Interface.IBlobRepository>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "InvalidSecureToken Game " + TestDataSeeder.RandomName());
        var chalSeeded = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Secure Attachment", "flag{secure-token}");
        var chal = await context.GameChallenges.FindAsync(chalSeeded.Id);
        Assert.NotNull(chal);

        using var ms = new MemoryStream(Guid.NewGuid().ToByteArray());
        var formFile = new Microsoft.AspNetCore.Http.FormFile(ms, 0, ms.Length, "file", "secure-token.bin");
        var blobRes = await blobService.CreateOrUpdateBlob(formFile, "secure-token.bin", CancellationToken.None);

        chal!.Attachment = new Attachment
        {
            Type = FileType.Local,
            LocalFile = blobRes,
            LocalFileId = blobRes.Id
        };
        await context.SaveChangesAsync();

        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "SecureTokenTeam");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = user.UserName, Password = "Test@123" });

        var invalidToken = WebEncoders.Base64UrlEncode(Guid.NewGuid().ToByteArray());
        var response = await client.GetAsync($"/Assets/{blobRes.Hash}/s/{invalidToken}/secure-token.bin");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        context.ChangeTracker.Clear();
        var downloadEvents = await context.GameEvents
            .Where(e => e.GameId == game.Id && e.Type == EventType.Download)
            .ToListAsync();
        var hasDownloadLog = downloadEvents.Any(e => e.Values is { Count: > 0 } && e.Values[0] == chal.Id.ToString());
        Assert.False(hasDownloadLog);
    }

    [Fact]
    public async Task AttachmentDownload_ShouldRejectAnonymousAccess_ForChallengeAttachment()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var blobService = scope.ServiceProvider.GetRequiredService<GZCTF.Repositories.Interface.IBlobRepository>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "AnonymousAttachmentAccess Game " + TestDataSeeder.RandomName());
        var chalSeeded = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Anon Protected", "flag{anon}");
        var chal = await context.GameChallenges.FindAsync(chalSeeded.Id);
        Assert.NotNull(chal);

        using var ms = new MemoryStream(Guid.NewGuid().ToByteArray());
        var formFile = new Microsoft.AspNetCore.Http.FormFile(ms, 0, ms.Length, "file", "anon.bin");
        var blobRes = await blobService.CreateOrUpdateBlob(formFile, "anon.bin", CancellationToken.None);

        chal!.Attachment = new Attachment
        {
            Type = FileType.Local,
            LocalFile = blobRes,
            LocalFileId = blobRes.Id
        };
        await context.SaveChangesAsync();

        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/Assets/{blobRes.Hash}/anon.bin");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AttachmentDownload_ShouldRequireOwnerIdentity_ForDynamicAttachmentWithoutToken()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var blobService = scope.ServiceProvider.GetRequiredService<GZCTF.Repositories.Interface.IBlobRepository>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "DynamicOwnerCheck Game " + TestDataSeeder.RandomName());
        var gameEntity = await context.Games.FirstAsync(g => g.Id == game.Id);

        using var ms = new MemoryStream(Guid.NewGuid().ToByteArray());
        var file = new Microsoft.AspNetCore.Http.FormFile(ms, 0, ms.Length, "file", "dyn-owner.bin");
        var blob = await blobService.CreateOrUpdateBlob(file, "dyn-owner.bin", CancellationToken.None);

        var challenge = new GameChallenge
        {
            Title = "Dynamic Owner Protected",
            Content = "dynamic owner enforcement",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.DynamicAttachment,
            Hints = [],
            IsEnabled = true,
            SubmissionLimit = 0,
            OriginalScore = 1000,
            MinScoreRate = 0.8,
            Difficulty = 5,
            FileName = "dynamic-owner.bin",
            GameId = gameEntity.Id,
            Game = gameEntity
        };

        challenge.Flags.Add(new FlagContext
        {
            Flag = "flag{dyn-owner}",
            Attachment = new Attachment
            {
                Type = FileType.Local,
                LocalFile = blob,
                LocalFileId = blob.Id
            }
        });

        await context.GameChallenges.AddAsync(challenge);
        await context.SaveChangesAsync();

        var victimUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var victimTeam = await TestDataSeeder.CreateTeamAsync(factory.Services, victimUser.Id, "OwnerVictimTeam");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, victimTeam.Id, victimUser.Id);

        var attackerUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var attackerTeam = await TestDataSeeder.CreateTeamAsync(factory.Services, attackerUser.Id, "OwnerAttackerTeam");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, attackerTeam.Id, attackerUser.Id);

        using var victimClient = factory.CreateClient();
        await victimClient.PostAsJsonAsync("/api/Account/Login", new { UserName = victimUser.UserName, Password = "Test@123" });
        var victimDetail = await victimClient.GetFromJsonAsync<ChallengeDetailModel>($"/api/Game/{game.Id}/Challenges/{challenge.Id}");
        var secureUrl = victimDetail?.Context.Url;

        Assert.False(string.IsNullOrWhiteSpace(secureUrl));
        Assert.Contains("/s/", secureUrl, StringComparison.OrdinalIgnoreCase);

        using var attackerClient = factory.CreateClient();
        await attackerClient.PostAsJsonAsync("/api/Account/Login", new { UserName = attackerUser.UserName, Password = "Test@123" });

        var plainUrl = Regex.Replace(secureUrl!, "/s/[^/]+/", "/", RegexOptions.IgnoreCase);
        var plainResponse = await attackerClient.GetAsync(plainUrl);
        Assert.Equal(HttpStatusCode.Forbidden, plainResponse.StatusCode);

        var tamperedUrl = Regex.Replace(secureUrl!, "/s/[^/]+/", "/s/not-a-valid-token/", RegexOptions.IgnoreCase);
        var tamperedResponse = await attackerClient.GetAsync(tamperedUrl);
        Assert.Equal(HttpStatusCode.Forbidden, tamperedResponse.StatusCode);
    }

    [Fact]
    public async Task GetChallenge_ShouldGenerateSecureUrl_ForFilenameContainingSlash()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var blobService = scope.ServiceProvider.GetRequiredService<GZCTF.Repositories.Interface.IBlobRepository>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "EscapedFilename Game " + TestDataSeeder.RandomName());
        var chalSeeded = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Escaped Filename Chal", "flag{escaped-name}");
        var chal = await context.GameChallenges.FindAsync(chalSeeded.Id);
        Assert.NotNull(chal);

        const string uploadName = "nested/path with spaces.txt";
        using var ms = new MemoryStream(Guid.NewGuid().ToByteArray());
        var formFile = new Microsoft.AspNetCore.Http.FormFile(ms, 0, ms.Length, "file", uploadName);
        var blobRes = await blobService.CreateOrUpdateBlob(formFile, uploadName, CancellationToken.None);

        chal!.Attachment = new Attachment
        {
            Type = FileType.Local,
            LocalFile = blobRes,
            LocalFileId = blobRes.Id
        };
        await context.SaveChangesAsync();

        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "EscapedFilenameTeam");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = user.UserName, Password = "Test@123" });

        var detail = await client.GetFromJsonAsync<ChallengeDetailModel>($"/api/Game/{game.Id}/Challenges/{chal.Id}");
        Assert.NotNull(detail);
        Assert.NotNull(detail!.Context);
        Assert.NotNull(detail.Context.Url);
        Assert.Contains("/s/", detail.Context.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%2F", detail.Context.Url, StringComparison.OrdinalIgnoreCase);

        var downloadResponse = await client.GetAsync(detail.Context.Url);
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
    }

    [Fact]
    public async Task GetCheatReport_ShouldNotFlagLegitimateDownloads()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dpProvider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var protector = dpProvider.CreateProtector("GZCTF.Assets.Download");

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "LegitDownload Game " + TestDataSeeder.RandomName());
        var chal = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Legit Chal", "flag{legit}");
        
        // Setup Team and User
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "LegitTeam");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        // Upload File
        // Upload File
        using var ms = new MemoryStream(Guid.NewGuid().ToByteArray());
        var formFile = new Microsoft.AspNetCore.Http.FormFile(ms, 0, ms.Length, "file", "legit.txt");
        var blobService = scope.ServiceProvider.GetRequiredService<GZCTF.Repositories.Interface.IBlobRepository>();
        var blobRes = await blobService.CreateOrUpdateBlob(formFile, "legit.txt", CancellationToken.None);
        var fileHash = blobRes.Hash;
        
        var attachment = new Attachment 
        { 
            LocalFile = blobRes, 
            LocalFileId = blobRes.Id,
            Type = FileType.Local,
            RemoteUrl = null
        };
        context.Attachments.Add(attachment);
        await context.SaveChangesAsync();
        
        var dbChal = await context.GameChallenges.FindAsync(chal.Id);
        dbChal!.AttachmentId = attachment.Id;
        await context.SaveChangesAsync();

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = user.UserName, Password = "Test@123" });

        // Scenario 1: Normal Download (No Token, Cookie Auth)
        var res1 = await client.GetAsync($"/Assets/{fileHash}/legit.txt");
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        // Scenario 2: Static Team Token
        var participation = await context.Participations.FirstOrDefaultAsync(p => p.TeamId == team.Id && p.GameId == game.Id);
        var res2 = await client.GetAsync($"/Assets/{fileHash}/legit.txt?token={participation!.Token}");
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);

        // Scenario 3: Secure Owner Token
        var expiry = DateTimeOffset.UtcNow.AddHours(1).Ticks;
        var payload = $"v1|{fileHash}|{user.Id}|{expiry}";
        var tokenBytes = protector.Protect(System.Text.Encoding.UTF8.GetBytes(payload));
        var secureToken = WebEncoders.Base64UrlEncode(tokenBytes);
        
        var res3 = await client.GetAsync($"/Assets/{fileHash}/s/{secureToken}/legit.txt");
        Assert.Equal(HttpStatusCode.OK, res3.StatusCode);

        // Login as Admin to view Cheat Report
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = admin.UserName, Password = "Test@123" });

        // Verify NO Abuse Logs
        var reportResponse = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        reportResponse.EnsureSuccessStatusCode();
        var report = await reportResponse.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        // Should be no TokenAbuse entries for this team
        Assert.DoesNotContain(report.IpAnalysis, i => i.TeamId == team.Id && i.Type == "TokenAbuse");
    }

    [Fact]
    public async Task GetCheatReport_ShouldDetectTokenAbuse_UnknownIP()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "UnknownIP Game " + TestDataSeeder.RandomName());
        var chal = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Chal", "flag{test}");
        
        var uVictim = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tVictim = await TestDataSeeder.CreateTeamAsync(factory.Services, uVictim.Id, "VictimTeam");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tVictim.Id, uVictim.Id);

        var uAttacker = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var tAttacker = await TestDataSeeder.CreateTeamAsync(factory.Services, uAttacker.Id, "AttackerTeam");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, tAttacker.Id, uAttacker.Id);

        var timeBase = DateTimeOffset.UtcNow.AddMinutes(-10);

        // Simulate Download with "Unknown" IP
        await context.GameEvents.AddAsync(new GameEvent
        {
            GameId = game.Id,
            Type = EventType.Download,
            TeamId = tAttacker.Id,
            UserId = uAttacker.Id,
            PublishTimeUtc = timeBase,
            Values = [
                chal.Id.ToString(), 
                "Attachment Download", 
                $"User {uAttacker.UserName} from team {tAttacker.Name} downloaded attachment for challenge {chal.Title}. [Token Source: Team {tVictim.Name}]", 
                "Unknown" // <--- IP IS UNKNOWN
            ]
        });
        await context.SaveChangesAsync();

        using var client = factory.CreateClient();
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = admin.UserName, Password = "Test@123" });

        var reportResponse = await client.GetAsync($"/api/game/{game.Id}/cheatreport");
        reportResponse.EnsureSuccessStatusCode();
        var report = await reportResponse.Content.ReadFromJsonAsync<CheatReport>(GetJsonOptions());

        Assert.NotNull(report);
        // Should STILL detect TokenAbuse
        Assert.Contains(report.IpAnalysis, i => i.TeamId == tAttacker.Id && i.Type == "TokenAbuse" && i.Details.Contains(tVictim.Name));
    }

    private JsonSerializerOptions GetJsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateTimeOffsetJsonConverter());
        options.Converters.Add(new IPAddressJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static DownloadEventLogMetadata ParseDownloadMetadata(GameEvent evt)
    {
        Assert.NotNull(evt.Values);
        Assert.True(evt.Values.Count > DownloadEventLogMetadata.ValuesIndex);
        Assert.True(DownloadEventLogMetadata.TryParse(evt.Values[DownloadEventLogMetadata.ValuesIndex], out var metadata));
        return metadata;
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
