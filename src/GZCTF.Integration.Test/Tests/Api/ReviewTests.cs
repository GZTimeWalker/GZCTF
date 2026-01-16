using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Game;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class ReviewTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task ReviewChallenge_ShouldWork_AfterSolving()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Setup Game
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Review Game " + TestDataSeeder.RandomName());
        
        // 2. Setup Challenge
        var chal = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Review Chal", "flag{review}");

        // 3. Setup Team & Participation
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, "Reviewers");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/Account/Login", new { UserName = user.UserName, Password = "Test@123" });

        // 4. Try Review BEFORE Solving -> Should Fail
        var reviewModel = new ChallengeReviewModel
        {
            Rating = ReviewRating.Like,
            Comment = "Good challenge!"
        };
        var failResponse = await client.PostAsJsonAsync($"/api/game/{game.Id}/Challenges/{chal.Id}/Review", reviewModel);
        Assert.Equal(HttpStatusCode.BadRequest, failResponse.StatusCode); // "You must solve the challenge first."

        // 5. Solve Challenge (Add Submission directly to DB for speed)
        var submission = new Submission
        {
            GameId = game.Id,
            ChallengeId = chal.Id,
            TeamId = team.Id,
            ParticipationId = participation.Id,
            UserId = user.Id,
            Answer = "flag{review}",
            Status = AnswerResult.Accepted,
            SubmitTimeUtc = DateTimeOffset.UtcNow
        };
        await context.Submissions.AddAsync(submission);
        await context.SaveChangesAsync();

        // 6. Submit Review AFTER Solving -> Should Success
        var successResponse = await client.PostAsJsonAsync($"/api/game/{game.Id}/Challenges/{chal.Id}/Review", reviewModel);
        successResponse.EnsureSuccessStatusCode();

        // 7. Verify Review as Admin
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), "Test@123", role: Role.Admin);
        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/Login", new { UserName = admin.UserName, Password = "Test@123" });

        var reviewsResponse = await adminClient.GetAsync($"/api/edit/Games/{game.Id}/Reviews?count=100");
        reviewsResponse.EnsureSuccessStatusCode();

        // Note: The response is wrapped in ArrayResponse<ChallengeReviewDetailModel> or similar implementation
        // For simplicity, we can read as JsonDocument to check content
        var content = await reviewsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Good challenge!", content);
    }
}
