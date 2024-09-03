using MemoryPack;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Extensions;

public static class CacheExtensions
{
    /// <summary>
    /// Get or create cache, if cache not exists will block
    /// Use CacheMaker and CacheRequest to replace handling longer time operation
    /// </summary>
    public static async Task<TResult> GetOrCreateAsync<TResult, TLogger>(this IDistributedCache cache,
        ILogger<TLogger> logger,
        string key,
        Func<DistributedCacheEntryOptions, Task<TResult>> func,
        CancellationToken token = default)
    {
        var cacheTime = DateTimeOffset.Now;
        var value = await cache.GetAsync(key, token);
        TResult? result = default;

        if (value is not null)
        {
            try
            {
                result = MemoryPackSerializer.Deserialize<TResult>(value);
            }
            catch
            {
                // ignored
            }

            if (result is not null)
                return result;
        }

        var cacheOptions = new DistributedCacheEntryOptions();
        result = await func(cacheOptions);
        var bytes = MemoryPackSerializer.Serialize(result);

        await cache.SetAsync(key, bytes, cacheOptions, token);

        logger.SystemLog(Program.StaticLocalizer[
            nameof(Resources.Program.Cache_Updated),
            key, cacheTime.ToString("HH:mm:ss.fff"), bytes.Length
        ], TaskStatus.Success, LogLevel.Debug);

        return result;
    }
}
