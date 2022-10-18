using Microsoft.Extensions.Caching.Distributed;
using MemoryPack;

namespace CTFServer.Utils;

public static class CacheHelper
{
    public static async Task<T> GetOrCreateAsync<T, L>(this IDistributedCache cache, ILogger<L> logger, string key, Func<DistributedCacheEntryOptions, Task<T>> func)
        where T : class
    {
        var value = await cache.GetAsync(key);
        T? result = default(T);

        if (value is not null)
        {
            try
            {
                result = MemoryPackSerializer.Deserialize<T>(value);
            }
            catch
            { }
            if (result is not null)
                return result;
        }

        var cacheOptions = new DistributedCacheEntryOptions();
        result = await func(cacheOptions);
        var bytes = MemoryPackSerializer.Serialize(result);
        await cache.SetAsync(key, bytes, cacheOptions);

        logger.SystemLog($"重建缓存：{key} @ {bytes.Length} bytes", TaskStatus.Success, LogLevel.Debug);
        return result;
    }
}
