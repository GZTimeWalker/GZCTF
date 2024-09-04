using GZCTF.Services.Cache;
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

        // most of the time, the cache is already been set
        if (TryDeserialize(value, ref result))
            return result!;

        // wait if the cache is updating
        value = await WaitLockAsync(cache, key, token);

        if (TryDeserialize(value, ref result))
            return result!;

        var lockKey = CacheKey.UpdateLock(key);
        await SetLockAsync(cache, lockKey, token);

        // begin the update
        var cacheOptions = new DistributedCacheEntryOptions();
        result = await func(cacheOptions);
        var bytes = MemoryPackSerializer.Serialize(result);

        await cache.SetAsync(key, bytes, cacheOptions, token);
        // finish the update

        await ReleaseLockAsync(cache, lockKey, token);

        logger.SystemLog(Program.StaticLocalizer[
            nameof(Resources.Program.Cache_Updated),
            key, cacheTime.ToString("HH:mm:ss.fff"), bytes.Length
        ], TaskStatus.Success, LogLevel.Debug);

        return result;
    }

    static bool TryDeserialize<TResult>(byte[]? value, ref TResult? result)
    {
        if (value is null)
            return false;

        try
        {
            result = MemoryPackSerializer.Deserialize<TResult>(value);
            return result is not null;
        }
        catch
        {
            return false;
        }
    }

    static async Task<byte[]?> WaitLockAsync(IDistributedCache cache, string key, CancellationToken token = default)
    {
        var lockKey = CacheKey.UpdateLock(key);
        var lockValue = await cache.GetAsync(lockKey, token);

        if (lockValue is null)
            return null;

        while (lockValue is not null)
        {
            await Task.Delay(100, token);
            lockValue = await cache.GetAsync(lockKey, token);
        }

        // if we wait for the lock, we should try to get the value again
        return await cache.GetAsync(key, token);
    }

    static Task SetLockAsync(IDistributedCache cache, string lockKey, CancellationToken token = default)
        => cache.SetAsync(lockKey, [],
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) }, token);

    static Task ReleaseLockAsync(IDistributedCache cache, string lockKey, CancellationToken token = default) =>
        cache.RemoveAsync(lockKey, token);
}
