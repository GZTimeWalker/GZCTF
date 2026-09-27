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

    private async Task<Guid> SeedChallengeAsync(string flag)
    {
        var challengeId = Guid.CreateVersion7();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Challenges.Add(new Challenge
        {
            Id = challengeId,
            Type = ChallengeType.StaticAttachment,
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
            Flags = [new ChallengeFlag { Kind = ChallengeFlagKind.Static, Value = flag }]
        });
        await db.SaveChangesAsync();
        return challengeId;
    }
}
