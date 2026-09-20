using Microsoft.Extensions.Caching.Memory;

namespace GZCTF.Features.Dashboard.Application;

public sealed class DashboardCache(IMemoryCache memoryCache)
{
    public bool TryGet<T>(string key, out T? value) => memoryCache.TryGetValue(key, out value);

    public void Set<T>(string key, T value) => memoryCache.Set(key, value, TimeSpan.FromSeconds(15));

    public void Clear()
    {
        // Entries expire quickly; precise invalidation is added with token lifecycle wiring.
    }
}
