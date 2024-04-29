using MemoryPack;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Extensions;

public static class CacheExtensions
{
    /// <summary>
    /// 获取缓存或重新构建，如果缓存不存在会阻塞
    /// 使用 CacheMaker 和 CacheRequest 代替处理耗时更久的缓存
    /// </summary>
    public static async Task<TResult> GetOrCreateAsync<TResult, TLogger>(this IDistributedCache cache,
        ILogger<TLogger> logger,
        string key,
        Func<DistributedCacheEntryOptions, Task<TResult>> func,
        CancellationToken token = default)
    {
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
        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.Cache_Rebuilt), key, bytes.Length],
            TaskStatus.Success, LogLevel.Debug);

        return result;
    }
}
