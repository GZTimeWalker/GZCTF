using MemoryPack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GZCTF.Services.HealthCheck;

public class CacheHealthCheck(IDistributedCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{nameof(CacheHealthCheck)}-{Guid.CreateVersion7()}";
            await cache.SetAsync(key, MemoryPackSerializer.Serialize(DateTime.UtcNow), cancellationToken);
            var time = MemoryPackSerializer.Deserialize<DateTime>(await cache.GetAsync(key, cancellationToken));
            await cache.RemoveAsync(key, cancellationToken);
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
