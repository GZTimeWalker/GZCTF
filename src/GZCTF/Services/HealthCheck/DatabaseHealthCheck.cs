using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GZCTF.Services.HealthCheck;

public class DatabaseHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var time = DateTime.UtcNow;
            var appliedMigrations =
                await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
            return DateTime.UtcNow - time > TimeSpan.FromSeconds(1)
                ? HealthCheckResult.Degraded()
                : !appliedMigrations.SequenceEqual(dbContext.Database.GetMigrations())
                    ? HealthCheckResult.Unhealthy()
                    : HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}
