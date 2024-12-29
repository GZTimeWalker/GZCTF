using FluentStorage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GZCTF.Services.HealthCheck;

public class StorageHealthCheck(IBlobStorage blobStorage) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var time = DateTime.UtcNow;
            var filename = $"{nameof(StorageHealthCheck)}-{Guid.CreateVersion7()}";
            await blobStorage.WriteTextAsync(filename, "ok", cancellationToken: cancellationToken);
            try
            {
                if (!await blobStorage.ExistsAsync(filename, cancellationToken: cancellationToken) ||
                    await blobStorage.ReadTextAsync(filename, cancellationToken: cancellationToken) != "ok")
                {
                    return HealthCheckResult.Unhealthy();
                }
            }
            finally
            {
                await blobStorage.DeleteAsync(filename, cancellationToken: cancellationToken);
            }
            return DateTime.UtcNow - time > TimeSpan.FromSeconds(1)
                ? HealthCheckResult.Degraded()
                : HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}
