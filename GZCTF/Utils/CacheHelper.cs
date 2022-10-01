using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace CTFServer.Utils;

public static class CacheHelper
{
    public static async Task<T> GetOrCreateAsync<T, L>(this IDistributedCache cache, ILogger<L> logger, string key, Func<DistributedCacheEntryOptions, Task<T>> func) where T : class
    {
        var value = await cache.GetAsync(key);
        T? result = null;

        if (value is not null)
        {
            try
            {
                result = JsonSerializer.Deserialize<T>(value);
            }
            catch
            { }
            if (result is not null)
                return result;
        }

        logger.SystemLog($"重建缓存：{key}", TaskStatus.Pending, LogLevel.Debug);

        var cacheOptions = new DistributedCacheEntryOptions();
        result = await func(cacheOptions);
        await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(result), cacheOptions);
        return result;
    }
}
