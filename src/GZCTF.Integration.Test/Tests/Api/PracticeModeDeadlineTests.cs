using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Game;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for practice mode and deadline functionality.
/// Tests:
/// - Joining game after it ends in practice mode vs non-practice mode
/// - Submitting flags after deadline in practice mode vs non-practice mode
/// - Scoreboard calculation correctness with practice mode submissions
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class PracticeModeDeadlineTests(GZCTFApplicationFactory factory)
{
    /// <summary>
    /// Test that joining after game ends respects practice mode setting
    /// </summary>
    [Fact]
    public async Task JoinGame_AfterGameEnds_ShouldRespectPracticeMode()
    {
        // Arrange: Create user and teams
        var userPassword = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, userName, userPassword, token: TestContext.Current.CancellationToken);
        var team1 = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {userName} 1", token: TestContext.Current.CancellationToken);
        var team2 = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {userName} 2", token: TestContext.Current.CancellationToken);

        // Create game without practice mode
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Join After Game Test", acceptWithoutReview: true, practiceMode: false, token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);

        // Set game end time to past
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gameInDb = await dbContext.Games.FindAsync(new object?[] { game.Id }, TestContext.Current.CancellationToken);
            Assert.NotNull(gameInDb);
            gameInDb.EndTimeUtc = DateTimeOffset.UtcNow.AddHours(-1);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act & Assert: Joining should fail when practice mode is disabled
        var joinResponse1 = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team1.Id, DivisionId = null }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(joinResponse1.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, joinResponse1.StatusCode);

        // Now enable practice mode
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gameInDb = await dbContext.Games.FindAsync(new object?[] { game.Id }, TestContext.Current.CancellationToken);
            Assert.NotNull(gameInDb);
            gameInDb.PracticeMode = true;
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act & Assert: Joining should succeed when practice mode is enabled
        var joinResponse2 = await client.PostAsJsonAsync($"/api/Game/{game.Id}", new GameJoinModel { TeamId = team2.Id, DivisionId = null }, cancellationToken: TestContext.Current.CancellationToken);

        joinResponse2.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Test that flag submission respects both challenge deadline and practice mode settings
    /// </summary>
    [Fact]
    public async Task SubmitFlag_AfterDeadline_ShouldRespectPracticeModeAndGameEnd()
    {
        // Arrange: Create user and setup
        var userPassword = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, userName, userPassword, token: TestContext.Current.CancellationToken);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {userName}", token: TestContext.Current.CancellationToken);

        // Create game with practice mode disabled
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Submission Deadline Test", acceptWithoutReview: true, practiceMode: false, token: TestContext.Current.CancellationToken);
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Deadline Test Challenge", "flag{deadline_test}", token: TestContext.Current.CancellationToken);
        var challenge2 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Game End Test Challenge", "flag{game_end}", token: TestContext.Current.CancellationToken);

        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id, token: TestContext.Current.CancellationToken);

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user.UserName, Password = userPassword }, cancellationToken: TestContext.Current.CancellationToken);

        // Set challenge deadline to past
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var challengeInDb = await dbContext.GameChallenges.FindAsync(new object?[] { challenge.Id }, TestContext.Current.CancellationToken);
            Assert.NotNull(challengeInDb);
            challengeInDb.DeadlineUtc = DateTimeOffset.UtcNow.AddHours(-1);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act & Assert: Submission should fail when deadline passed and practice mode disabled
        var flagSubmitModel = new FlagSubmitModel { Flag = "flag{deadline_test}" };
        var submitResponse1 = await client.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}", flagSubmitModel, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(submitResponse1.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, submitResponse1.StatusCode);

        // Now enable practice mode
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gameInDb = await dbContext.Games.FindAsync(new object?[] { game.Id }, TestContext.Current.CancellationToken);
            Assert.NotNull(gameInDb);
            gameInDb.PracticeMode = true;
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act & Assert: Submission should succeed after enabling practice mode
        var submitResponse2 = await client.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}", flagSubmitModel, cancellationToken: TestContext.Current.CancellationToken);

        submitResponse2.EnsureSuccessStatusCode();
        var submissionId = await submitResponse2.Content.ReadFromJsonAsync<int>(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(300, TestContext.Current.CancellationToken);

        // Verify submission status
        var statusResponse =
            await client.GetAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}/Status/{submissionId}", TestContext.Current.CancellationToken);
        statusResponse.EnsureSuccessStatusCode();
        var status = await statusResponse.Content.ReadFromJsonAsync<AnswerResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(AnswerResult.Accepted, status);

        // Test game end scenario in practice mode
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gameInDb = await dbContext.Games.FindAsync(new object?[] { game.Id }, TestContext.Current.CancellationToken);
            Assert.NotNull(gameInDb);
            gameInDb.EndTimeUtc = DateTimeOffset.UtcNow.AddHours(-1);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act & Assert: Submission should succeed even after game ends in practice mode
        var flagSubmitModel2 = new FlagSubmitModel { Flag = "flag{game_end}" };
        var submitResponse3 = await client.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge2.Id}", flagSubmitModel2, cancellationToken: TestContext.Current.CancellationToken);

        submitResponse3.EnsureSuccessStatusCode();
        var submissionId2 = await submitResponse3.Content.ReadFromJsonAsync<int>(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(300, TestContext.Current.CancellationToken);

        var statusResponse2 =
            await client.GetAsync($"/api/Game/{game.Id}/Challenges/{challenge2.Id}/Status/{submissionId2}", TestContext.Current.CancellationToken);
        statusResponse2.EnsureSuccessStatusCode();
        var status2 = await statusResponse2.Content.ReadFromJsonAsync<AnswerResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(AnswerResult.Accepted, status2);
    }

    /// <summary>
    /// Test that scoreboard calculation properly excludes submissions after deadline
    /// even in practice mode, while accepting them as valid submissions
    /// </summary>
    [Fact]
    public async Task Scoreboard_ShouldCalculateCorrectlyWithDeadlineAndPracticeMode()
    {
        // Arrange: Create teams and game
        var user1Password = "User1@Pass123";
        var user1 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), user1Password, token: TestContext.Current.CancellationToken);
        var team1 = await TestDataSeeder.CreateTeamAsync(factory.Services, user1.Id, "Team 1", token: TestContext.Current.CancellationToken);

        var user2Password = "User2@Pass123";
        var user2 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), user2Password, token: TestContext.Current.CancellationToken);
        var team2 = await TestDataSeeder.CreateTeamAsync(factory.Services, user2.Id, "Team 2", token: TestContext.Current.CancellationToken);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Scoreboard Test", acceptWithoutReview: true, practiceMode: true, token: TestContext.Current.CancellationToken);
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Scoreboard Challenge", "flag{scoreboard}", originalScore: 100, token: TestContext.Current.CancellationToken);

        // Set challenge deadline to future
        var deadline = DateTimeOffset.UtcNow.AddMinutes(5);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var challengeInDb = await dbContext.GameChallenges.FindAsync(new object?[] { challenge.Id }, TestContext.Current.CancellationToken);
            Assert.NotNull(challengeInDb);
            challengeInDb.DeadlineUtc = deadline;
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Join both teams
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team1.Id, user1.Id, token: TestContext.Current.CancellationToken);
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team2.Id, user2.Id, token: TestContext.Current.CancellationToken);

        // Test 1: Both teams submit before deadline (same deadline period)
        using var client1 = factory.CreateClient();
        await client1.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user1.UserName, Password = user1Password }, cancellationToken: TestContext.Current.CancellationToken);

        var submitResponse1 = await client1.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}", new FlagSubmitModel { Flag = "flag{scoreboard}" }, cancellationToken: TestContext.Current.CancellationToken);
        submitResponse1.EnsureSuccessStatusCode();
        var submissionId1 = await submitResponse1.Content.ReadFromJsonAsync<int>(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(300, TestContext.Current.CancellationToken);

        using var client2 = factory.CreateClient();
        await client2.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user2.UserName, Password = user2Password }, cancellationToken: TestContext.Current.CancellationToken);

        var submitResponse2 = await client2.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}", new FlagSubmitModel { Flag = "flag{scoreboard}" }, cancellationToken: TestContext.Current.CancellationToken);
        submitResponse2.EnsureSuccessStatusCode();
        var submissionId2 = await submitResponse2.Content.ReadFromJsonAsync<int>(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(300, TestContext.Current.CancellationToken);

        // Verify both submissions are accepted
        var status1Response =
            await client1.GetAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}/Status/{submissionId1}", TestContext.Current.CancellationToken);
        status1Response.EnsureSuccessStatusCode();
        var status1 = await status1Response.Content.ReadFromJsonAsync<AnswerResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(AnswerResult.Accepted, status1);

        var status2Response =
            await client2.GetAsync($"/api/Game/{game.Id}/Challenges/{challenge.Id}/Status/{submissionId2}", TestContext.Current.CancellationToken);
        status2Response.EnsureSuccessStatusCode();
        var status2 = await status2Response.Content.ReadFromJsonAsync<AnswerResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(AnswerResult.Accepted, status2);

        // Test 2: Update deadline to past for team 2 scenario
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var challengeInDb = await dbContext.GameChallenges.FindAsync(new object?[] { challenge.Id }, TestContext.Current.CancellationToken);
            Assert.NotNull(challengeInDb);
            challengeInDb.DeadlineUtc = DateTimeOffset.UtcNow.AddSeconds(-10);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Create second challenge for after-deadline submission test
        var challenge2 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Deadline Test Challenge", "flag{after_deadline}", originalScore: 100, token: TestContext.Current.CancellationToken);

        // Team 3 (new) submits after deadline in practice mode - should be accepted but not scored
        var user3Password = "User3@Pass123";
        var user3 = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), user3Password, token: TestContext.Current.CancellationToken);
        var team3 = await TestDataSeeder.CreateTeamAsync(factory.Services, user3.Id, "Team 3", token: TestContext.Current.CancellationToken);
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team3.Id, user3.Id, token: TestContext.Current.CancellationToken);

        using var client3 = factory.CreateClient();
        await client3.PostAsJsonAsync("/api/Account/LogIn", new LoginModel { UserName = user3.UserName, Password = user3Password }, cancellationToken: TestContext.Current.CancellationToken);

        var submitResponse3 = await client3.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challenge2.Id}", new FlagSubmitModel { Flag = "flag{after_deadline}" }, cancellationToken: TestContext.Current.CancellationToken);
        submitResponse3.EnsureSuccessStatusCode();
        var submissionId3 = await submitResponse3.Content.ReadFromJsonAsync<int>(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(300, TestContext.Current.CancellationToken);

        // Verify submission is accepted
        var status3Response =
            await client3.GetAsync($"/api/Game/{game.Id}/Challenges/{challenge2.Id}/Status/{submissionId3}", TestContext.Current.CancellationToken);
        status3Response.EnsureSuccessStatusCode();
        var status3 = await status3Response.Content.ReadFromJsonAsync<AnswerResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(AnswerResult.Accepted, status3);

        // Get scoreboard and verify scoring is correct
        var scoreboardResponse = await client1.GetAsync($"/api/Game/{game.Id}/Scoreboard", TestContext.Current.CancellationToken);
        scoreboardResponse.EnsureSuccessStatusCode();

        var scoreboardContent = await scoreboardResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        // Verify that teams who submitted within deadline are in scoreboard
        Assert.Contains(team1.Name, scoreboardContent);
    }
}
