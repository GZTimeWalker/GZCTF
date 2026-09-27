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
        var sourceIds = source.Paths.SelectMany(path => path.Modules)
            .SelectMany(module => module.Challenges).Select(challenge => challenge.SourceId).ToArray();
        Assert.Equal(2, await db.Challenges.CountAsync(item => sourceIds.Contains(item.SourceId)));
        var progress = await db.ChallengeProgress.Include(item => item.Challenge)
            .Where(item => sourceIds.Contains(item.Challenge.SourceId)).ToListAsync();
        Assert.Empty(progress);
    }

    [Fact]
    public async Task Behavior_field_mismatch_blocks_parity_completion()
    {
        var source = BuildSource($"s20-{Guid.NewGuid():N}");
        await using var scope = factory.Services.CreateAsyncScope();
        var importer = scope.ServiceProvider.GetRequiredService<CanonicalImportService>();
        var result = await importer.ImportAsync(source);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mapping = await db.LegacyChallengeMaps.FirstAsync(item => item.MigrationBatchId == result.BatchId);
        var flag = await db.ChallengeFlags.SingleAsync(item => item.ChallengeId == mapping.ChallengeId);
        flag.Value = "flag{tampered}";
        await db.SaveChangesAsync();

        var parity = scope.ServiceProvider.GetRequiredService<ImportParityService>();
        var batch = await db.MigrationBatches.SingleAsync(item => item.Id == result.BatchId);
        await Assert.ThrowsAsync<ImportParityException>(() => parity.CompareAndEnforceAsync(source, batch));
    }

    [Fact]
    public async Task Imported_legacy_CTF_category_is_kept_on_the_independent_challenge()
    {
        var source = BuildSource($"ctf-category-{Guid.NewGuid():N}", "{\"category\":\"Web\"}");
        await using var scope = factory.Services.CreateAsyncScope();
        var importer = scope.ServiceProvider.GetRequiredService<CanonicalImportService>();
        await importer.ImportAsync(source);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sourceIds = source.Paths.SelectMany(path => path.Modules)
            .SelectMany(module => module.Challenges).Select(challenge => challenge.SourceId).ToArray();
        var imported = await db.Challenges.Where(item => sourceIds.Contains(item.SourceId)).ToListAsync();
        Assert.Equal(2, imported.Count);
        Assert.All(imported, challenge => Assert.Equal(ChallengeCategory.Web, challenge.CtfCategory));
    }

    private static CanonicalChallengeImportBatch BuildSource(
        string fingerprint, string metadata = "{\"score\":100}")
    {
        CanonicalChallengeImport Challenge(string sourceId) => new(
            "legacy", sourceId, ChallengeType.StaticAttachment,
            [new ImportLocalizedText("en", "Same title", "Summary", "Body")],
            [], "flag{legacy}", null, [], null, metadata, 0, 0);

        return new CanonicalChallengeImportBatch(
            "legacy", fingerprint,
            [
                new CanonicalPathImport("legacy", $"game-a-{fingerprint}", $"game-a-{fingerprint}",
                    "Same game title", "Summary",
                    [new CanonicalModuleImport($"module-a-{fingerprint}", "Module", "Summary", 0,
                        [Challenge($"challenge-a-{fingerprint}")])]),
                new CanonicalPathImport("legacy", $"game-b-{fingerprint}", $"game-b-{fingerprint}",
                    "Same game title", "Summary",
                    [new CanonicalModuleImport($"module-b-{fingerprint}", "Module", "Summary", 0,
                        [Challenge($"challenge-b-{fingerprint}")])])
            ],
            []);
    }
}
