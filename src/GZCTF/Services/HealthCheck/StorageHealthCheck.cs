using FluentStorage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GZCTF.Services.HealthCheck;

public class StorageHealthCheck(IBlobStorage blobStorage) : IHealthCheck
{
    static readonly string Filename = $"{nameof(StorageHealthCheck)}_{Guid.CreateVersion7():N}";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var time = DateTime.UtcNow;
            var random = Guid.NewGuid().ToString();
            await blobStorage.WriteTextAsync(Filename, random, cancellationToken: cancellationToken);

            try
            {
                if (!await blobStorage.ExistsAsync(Filename, cancellationToken) ||
                    await blobStorage.ReadTextAsync(Filename, cancellationToken: cancellationToken) != random)
                    return HealthCheckResult.Unhealthy();
            }
            finally
            {
                await blobStorage.DeleteAsync(Filename, cancellationToken);
            }

            return DateTime.UtcNow - time > TimeSpan.FromSeconds(5)
                ? HealthCheckResult.Degraded()
                : HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}
