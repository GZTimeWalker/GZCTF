using GZCTF.Features.Imports.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GZCTF.Services.HealthCheck;

public sealed class MigrationHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var batch = await db.MigrationBatches.AsNoTracking().SingleOrDefaultAsync(item =>
            item.SourceType == "legacy-database" && item.PackageFingerprintSha256 == "legacy-database-v1",
            cancellationToken);
        return batch?.State is MigrationBatchState.Running or MigrationBatchState.Failed
            ? HealthCheckResult.Unhealthy($"legacy_migration_{batch.Id}")
            : HealthCheckResult.Healthy();
    }
}
