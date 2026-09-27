using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.ChallengeRuntime.Application;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Runtime;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ChallengeSubmissionTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Correct_submission_completes_once_and_increments_daily_stat_once()
    {
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "S12!LearnerPassword");
        var challengeId = await SeedChallengeAsync("flag{s12-static}");

        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ChallengeSubmissionService>();
        var incorrect = await service.SubmitAsync(user.Id, challengeId, "flag{wrong}");
        var first = await service.SubmitAsync(user.Id, challengeId, "flag{s12-static}");
        var duplicate = await service.SubmitAsync(user.Id, challengeId, "flag{s12-static}");

        Assert.False(incorrect.Accepted);
        Assert.True(first.Accepted);
        Assert.True(first.FirstSolve);
        Assert.Equal(ChallengeSolveMode.Independent, first.SolveMode);
        Assert.True(duplicate.Accepted);
        Assert.False(duplicate.FirstSolve);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.ChallengeProgress.CountAsync(item =>
            item.UserId == user.Id && item.ChallengeId == challengeId));
        Assert.Equal(1, await db.LearnerDailySolveStats.CountAsync(item =>
            item.UserId == user.Id && item.SolveCount == 1));
        Assert.Equal(3, await db.ChallengeSubmissions.CountAsync(item =>
            item.UserId == user.Id && item.ChallengeId == challengeId));
    }

    [Fact]
    public async Task Static_challenge_accepts_any_configured_flag()
    {
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "S12!LearnerPassword");
        var challengeId = await SeedChallengeAsync("flag{first}", "flag{second}");

        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ChallengeSubmissionService>();
        var result = await service.SubmitAsync(user.Id, challengeId, "flag{second}");

        Assert.True(result.Accepted);
        Assert.True(result.FirstSolve);
    }

    [Fact]
    public async Task Limited_correct_submission_keeps_its_record_and_progress()
    {
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "S12!LearnerPassword");
        var challengeId = await SeedChallengeAsync("flag{correct}", submissionLimit: 1);

        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ChallengeSubmissionService>();
        var first = await service.SubmitAsync(user.Id, challengeId, "flag{correct}");
        var second = await service.SubmitAsync(user.Id, challengeId, "flag{wrong}");

        Assert.True(first.Accepted);
        Assert.True(first.FirstSolve);
        Assert.Equal("challenge.submission_limit_exhausted", second.RejectionCode);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.ChallengeSubmissions.CountAsync(item =>
            item.UserId == user.Id && item.ChallengeId == challengeId));
        Assert.Equal(1, await db.ChallengeProgress.CountAsync(item =>
            item.UserId == user.Id && item.ChallengeId == challengeId));
    }

    private async Task<Guid> SeedChallengeAsync(
        string flag, string? anotherFlag = null, int submissionLimit = 0)
    {
        var challengeId = Guid.CreateVersion7();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Challenges.Add(new Challenge
        {
            Id = challengeId,
            Type = ChallengeType.StaticAttachment,
            SubmissionLimit = submissionLimit,
            PublicationState = ChallengePublicationState.Published,
            SourceType = "s12-test",
            SourceId = challengeId.ToString("N"),
            Localizations =
            [
                new ChallengeLocalization
                {
                    Locale = "en",
                    Title = "Submission challenge",
                    Summary = "Submission challenge",
                    Body = "Body"
                }
            ],
            Flags = anotherFlag is null
                ? [new ChallengeFlag { Kind = ChallengeFlagKind.Static, Value = flag }]
                : [new ChallengeFlag { Kind = ChallengeFlagKind.Static, Value = flag },
                    new ChallengeFlag { Kind = ChallengeFlagKind.Static, Value = anotherFlag }]
        });
        await db.SaveChangesAsync();
        return challengeId;
    }
}
