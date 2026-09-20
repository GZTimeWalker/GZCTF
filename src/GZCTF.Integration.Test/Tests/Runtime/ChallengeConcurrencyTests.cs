using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.ChallengeRuntime.Domain;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Runtime;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ChallengeConcurrencyTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task A_user_has_at_most_one_active_instance_per_challenge()
    {
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "S09!LearnerPassword");
        var challengeId = await SeedChallengeAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.UserChallengeInstances.Add(new UserChallengeInstance
        {
            UserId = user.Id,
            ChallengeId = challengeId,
            Status = ChallengeInstanceStatus.Running,
            IsActive = true
        });
        await db.SaveChangesAsync();

        db.UserChallengeInstances.Add(new UserChallengeInstance
        {
            UserId = user.Id,
            ChallengeId = challengeId,
            Status = ChallengeInstanceStatus.Pending,
            IsActive = true
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Global_completion_remains_unique_for_a_user_and_challenge()
    {
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "S09!LearnerPassword");
        var challengeId = await SeedChallengeAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ChallengeProgress.Add(new ChallengeProgress
        {
            UserId = user.Id,
            ChallengeId = challengeId
        });
        await db.SaveChangesAsync();

        db.ChallengeProgress.Add(new ChallengeProgress
        {
            UserId = user.Id,
            ChallengeId = challengeId
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private async Task<Guid> SeedChallengeAsync()
    {
        var challengeId = Guid.CreateVersion7();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Challenges.Add(new Challenge
        {
            Id = challengeId,
            Type = ChallengeType.StaticAttachment,
            PublicationState = ChallengePublicationState.Published,
            SourceType = "s09-test",
            SourceId = challengeId.ToString("N"),
            Localizations =
            [
                new ChallengeLocalization
                {
                    Locale = "en",
                    Title = "Runtime constraint challenge",
                    Summary = "Runtime constraint challenge",
                    Body = "Body"
                }
            ]
        });
        await db.SaveChangesAsync();
        return challengeId;
    }
}
