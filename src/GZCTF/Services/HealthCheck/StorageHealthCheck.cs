using FluentStorage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GZCTF.Services.HealthCheck;

public class StorageHealthCheck(IBlobStorage blobStorage) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var time = DateTime.UtcNow;
            const string filename = "GZCTF_HealthCheck";
            string random = Guid.NewGuid().ToString();
            await blobStorage.WriteTextAsync(filename, random, cancellationToken: cancellationToken);

            if (!await blobStorage.ExistsAsync(filename, cancellationToken: cancellationToken) ||
                await blobStorage.ReadTextAsync(filename, cancellationToken: cancellationToken) != random)
                return HealthCheckResult.Unhealthy();

            return DateTime.UtcNow - time > TimeSpan.FromSeconds(10)
                ? HealthCheckResult.Degraded()
                : HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}
