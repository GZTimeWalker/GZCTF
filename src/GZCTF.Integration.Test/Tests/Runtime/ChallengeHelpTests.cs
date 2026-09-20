using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.ChallengeRuntime.Application;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Runtime;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ChallengeHelpTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Hints_are_ordered_writeup_is_recorded_once_and_help_does_not_solve()
    {
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "S13!LearnerPassword");
        var challengeId = await SeedChallengeAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ChallengeHelpService>();
        var firstHint = await service.RevealNextHintAsync(user.Id, challengeId, "zh-CN");
        var secondHint = await service.RevealNextHintAsync(user.Id, challengeId, "zh-CN");
        var noMoreHints = await service.RevealNextHintAsync(user.Id, challengeId, "zh-CN");
        var firstWriteup = await service.RevealWriteupAsync(user.Id, challengeId, "zh-CN");
        var repeatedWriteup = await service.RevealWriteupAsync(user.Id, challengeId, "zh-CN");

        Assert.Equal(0, firstHint?.SortOrder);
        Assert.Equal(1, secondHint?.SortOrder);
        Assert.Null(noMoreHints);
        Assert.Equal(firstWriteup?.FirstViewedAtUtc, repeatedWriteup?.FirstViewedAtUtc);
        Assert.Equal(3, await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .ChallengeHelpUsages.CountAsync(item => item.UserId == user.Id && item.ChallengeId == challengeId));
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .ChallengeProgress.CountAsync(item => item.UserId == user.Id && item.ChallengeId == challengeId));
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
            SourceType = "s13-test",
            SourceId = challengeId.ToString("N"),
            Localizations =
            [new ChallengeLocalization { Locale = "en", Title = "Help", Summary = "Help", Body = "Body" }],
            Hints =
            [
                new ChallengeHint { Locale = "en", SortOrder = 0, Content = "First hint" },
                new ChallengeHint { Locale = "en", SortOrder = 1, Content = "Second hint" }
            ],
            Writeups = [new ChallengeWriteup { Locale = "en", Content = "Official writeup" }]
        });
        await db.SaveChangesAsync();
        return challengeId;
    }
}
