using GZCTF.Features.Imports.Infrastructure;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.Imports.Application;

public sealed class StartupLegacyMigrationService(
    LegacyDatabaseSource source,
    CanonicalImportService importer,
    AppDbContext db,
    ILogger<StartupLegacyMigrationService> logger)
{
    public async Task<CanonicalImportResult> RunOnceAsync(CancellationToken token = default)
    {
        var existing = await db.MigrationBatches.SingleOrDefaultAsync(item =>
            item.SourceType == "legacy-database" && item.PackageFingerprintSha256 == "legacy-database-v1", token);
        if (existing?.State == Domain.MigrationBatchState.Completed)
            return new CanonicalImportResult(existing.Id, existing.State, existing.ChallengeCount,
                existing.PathCount, existing.WarningCount);

        try
        {
            var graph = await source.ReadAsync(token);
            return await importer.ImportAsync(graph, token);
        }
        catch (Exception exception)
        {
            var failed = existing ?? new Domain.MigrationBatch
            {
                SourceType = "legacy-database",
                PackageFingerprintSha256 = "legacy-database-v1",
                State = Domain.MigrationBatchState.Failed
            };
            failed.State = Domain.MigrationBatchState.Failed;
            failed.ErrorsJson = System.Text.Json.JsonSerializer.Serialize(new[] { exception.GetType().Name });
            if (existing is null)
                db.MigrationBatches.Add(failed);
            await db.SaveChangesAsync(token);
            logger.LogError(exception, "Legacy database migration failed for batch {BatchId}", failed.Id);
            throw;
        }
    }
}
