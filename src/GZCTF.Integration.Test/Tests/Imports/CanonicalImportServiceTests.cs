using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.Imports.Application;
using GZCTF.Features.Imports.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Imports;

[Collection(nameof(IntegrationTestCollection))]
public sealed class CanonicalImportServiceTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Import_is_transactional_and_idempotent_while_preserving_duplicate_titles()
    {
        var source = BuildSource($"s17-{Guid.NewGuid():N}");
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<CanonicalImportService>();
        var first = await service.ImportAsync(source);
        var second = await service.ImportAsync(source);

        Assert.Equal(MigrationBatchState.Completed, first.State);
        Assert.Equal(first.BatchId, second.BatchId);
        Assert.Equal(2, first.PathCount);
        Assert.Equal(2, first.ChallengeCount);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.LegacyPathMaps.CountAsync(item => item.MigrationBatchId == first.BatchId));
        Assert.Equal(2, await db.LegacyChallengeMaps.CountAsync(item => item.MigrationBatchId == first.BatchId));
        Assert.Equal(2, await db.Challenges.CountAsync(item => item.SourceType == source.SourceType));
        var sourceIds = source.Paths.SelectMany(path => path.Modules)
            .SelectMany(module => module.Challenges).Select(challenge => challenge.SourceId).ToArray();
        var progress = await db.ChallengeProgress.Include(item => item.Challenge)
            .Where(item => sourceIds.Contains(item.Challenge.SourceId)).ToListAsync();
        Assert.Empty(progress);
    }

    private static CanonicalChallengeImportBatch BuildSource(string fingerprint)
    {
        static CanonicalChallengeImport Challenge(string sourceId) => new(
            "legacy", sourceId, ChallengeType.StaticAttachment,
            [new ImportLocalizedText("en", "Same title", "Summary", "Body")],
            [], "flag{legacy}", null, [], null, "{\"score\":100}", 0, 0);

        return new CanonicalChallengeImportBatch(
            "legacy", fingerprint,
            [
                new CanonicalPathImport("legacy", "game-a", "game-a", "Same game title", "Summary",
                    [new CanonicalModuleImport("module-a", "Module", "Summary", 0, [Challenge("challenge-a")])]),
                new CanonicalPathImport("legacy", "game-b", "game-b", "Same game title", "Summary",
                    [new CanonicalModuleImport("module-b", "Module", "Summary", 0, [Challenge("challenge-b")])])
            ],
            []);
    }
}
