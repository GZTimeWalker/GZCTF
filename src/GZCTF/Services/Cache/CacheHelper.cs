using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using GZCTF.Services.Cache.Handlers;
using MemoryPack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace GZCTF.Services.Cache;

public class CacheHelper(
    IDistributedCache distributedCache,
    IMemoryCache memoryCache,
    ChannelWriter<CacheRequest> channelWriter)
{
    /// <summary>
    /// Get or create cache, if cache not exists will block.
    /// Use local memory cache first to reduce the pressure on distributed cache.
    /// Use CacheMaker and CacheRequest to replace handling longer time operation
    /// </summary>
    public async Task<TResult> GetOrCreateAsync<TResult, TLogger>(
        ILogger<TLogger> logger,
        string key,
        Func<DistributedCacheEntryOptions, Task<TResult>> func,
        MemoryCacheEntryOptions? memoryCacheOptions = null,
        CancellationToken token = default)
    {
        var value = await memoryCache.GetOrCreateAsync<TResult>(key,
            _ => GetOrCreateFromDistributedCacheAsync(logger, key, func, token),
            memoryCacheOptions ?? CommonMemoryCacheOptions);

        return value ?? await GetOrCreateFromDistributedCacheAsync(logger, key, func, token);
    }

    /// <summary>
    /// Get cache value, return null if not exists.
    /// </summary>
    public async Task<TResult?> GetAsync<TResult>(string key, CancellationToken token = default)
    {
        if (memoryCache.TryGetValue(key, out TResult? value) && value is not null)
            return value;

        var bytes = await distributedCache.GetAsync(key, token);
        if (TryDeserialize(bytes, ref value))
            memoryCache.Set(key, value, CommonMemoryCacheOptions);

        return value;
    }

    public Task<string?> GetStringAsync(string key, CancellationToken token = default)
    {
        if (memoryCache.TryGetValue(key, out string? value) && value is not null)
            return Task.FromResult<string?>(value);

        return distributedCache.GetStringAsync(key, token);
    }

    public async Task SetStringAsync(string key, string value, DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        await distributedCache.SetStringAsync(key, value, options, token);
        memoryCache.Set(key, value, CommonMemoryCacheOptions);
    }

    public async Task RemoveAsync(string key, CancellationToken token)
    {
        await distributedCache.RemoveAsync(key, token);
        memoryCache.Remove(key);
    }

    public async Task FlushScoreboardCache(int gameId, CancellationToken token) =>
        await channelWriter.WriteAsync(ScoreboardCacheHandler.MakeCacheRequest(gameId), token);

    public async Task FlushRecentGamesCache(CancellationToken token) =>
        await channelWriter.WriteAsync(RecentGamesCacheHandler.MakeCacheRequest(), token);

    public async Task FlushGameListCache(CancellationToken token) =>
        await channelWriter.WriteAsync(GameListCacheHandler.MakeCacheRequest(), token);

    static readonly MemoryCacheEntryOptions CommonMemoryCacheOptions = new()
    {
        // The pulling frequency of the scoreboard is 10s from the client side,
        // so we use memory cache to reduce the pressure on distributed cache.
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5),
    };

    async Task<TResult> GetOrCreateFromDistributedCacheAsync<TResult, TLogger>(
        ILogger<TLogger> logger,
        string key,
        Func<DistributedCacheEntryOptions, Task<TResult>> func,
        CancellationToken token = default)
    {
        var cacheTime = DateTimeOffset.Now;
        var value = await distributedCache.GetAsync(key, token);
        TResult? result = default;

        // most of the time, the cache is already been set
        if (TryDeserialize(value, ref result))
            return result;

        // wait if the cache is updating
        value = await WaitLockAsync(key, token);

        if (TryDeserialize(value, ref result))
            return result;

        var lockKey = CacheKey.UpdateLock(key);
        await SetLockAsync(lockKey, token);

        int byteCount;
        try
        {
            // begin the update
            var cacheOptions = new DistributedCacheEntryOptions();
            result = await func(cacheOptions);
            var bytes = MemoryPackSerializer.Serialize(result);
            byteCount = bytes.Length;

            // finish the update
            await distributedCache.SetAsync(key, bytes, cacheOptions, token);
        }
        finally
        {
            // always release the lock
            await ReleaseLockAsync(lockKey, token);
        }

        logger.SystemLog(StaticLocalizer[
            nameof(Resources.Program.Cache_Updated),
            key, cacheTime.ToString("HH:mm:ss.fff"), byteCount
        ], TaskStatus.Success, LogLevel.Debug);

        return result;
    }

    static bool TryDeserialize<TResult>(byte[]? value, [NotNullWhen(true)] ref TResult? result)
    {
        if (value is null)
            return false;

        try
        {
            if (MemoryPackSerializer.Deserialize<TResult>(value) is { } deserialized)
                result = deserialized;

            return result is not null;
        }
        catch
        {
            return false;
        }
    }

    async Task<byte[]?> WaitLockAsync(string key, CancellationToken token = default)
    {
        var lockKey = CacheKey.UpdateLock(key);
        var lockValue = await distributedCache.GetAsync(lockKey, token);

        if (lockValue is null)
            return null;

        while (lockValue is not null)
        {
            await Task.Delay(100, token);
            lockValue = await distributedCache.GetAsync(lockKey, token);
        }

        // if we wait for the lock, we should try to get the value again
        return await distributedCache.GetAsync(key, token);
    }

    Task SetLockAsync(string lockKey, CancellationToken token = default)
        => distributedCache.SetAsync(lockKey, [],
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(1) }, token);

    Task ReleaseLockAsync(string lockKey, CancellationToken token = default) =>
        distributedCache.RemoveAsync(lockKey, token);
}

/// <summary>
/// Cache keys
/// </summary>
public static class CacheKey
{
    /// <summary>
    /// Favicon
    /// </summary>
    public const string Favicon = "_Favicon";

    /// <summary>
    /// Index
    /// </summary>
    public const string Index = "_Index";

    /// <summary>
    /// Scoreboard
    /// </summary>
    public const string ScoreBoardBase = "_ScoreBoard";

    /// <summary>
    /// Recent games
    /// </summary>
    public const string RecentGames = "_RecentGames";

    /// <summary>
    /// The game list cache, latest 100 games
    /// </summary>
    public const string GameList = "_GameList";

    /// <summary>
    /// Posts
    /// </summary>
    public const string Posts = "_Posts";

    /// <summary>
    /// The cron job lock
    /// </summary>
    public const string CronJobLock = "_CronJobLock";

    /// <summary>
    /// Is exercise available
    /// </summary>
    public const string ExerciseAvailable = "_ExerciseAvailable";

    /// <summary>
    /// The client configuration
    /// </summary>
    public const string ClientConfig = "_ClientConfig";

    /// <summary>
    /// The captcha configuration
    /// </summary>
    public const string CaptchaConfig = "_CaptchaConfig";

    /// <summary>
    /// The cache update lock
    /// </summary>
    public static string UpdateLock(string key) => $"_UpdateLock{key}";

    /// <summary>
    /// The last update time
    /// </summary>
    public static string LastUpdateTime(string key) => $"_LastUpdateTime_{key}";

    /// <summary>
    /// Scoreboard cache
    /// </summary>
    public static string ScoreBoard(int id) => $"_ScoreBoard_{id}";

    /// <summary>
    /// Scoreboard cache
    /// </summary>
    public static string ScoreBoard(string id) => $"_ScoreBoard_{id}";

    /// <summary>
    /// Game cache
    /// </summary>
    public static string GameCache(int id) => $"_GameCache_{id}";

    /// <summary>
    /// Game notice cache
    /// </summary>
    public static string GameNotice(int id) => $"_GameNotice_{id}";

    /// <summary>
    /// Container connection counter
    /// </summary>
    public static string ConnectionCount(Guid id) => $"_Container_Conn_{id}";

    /// <summary>
    /// HashPow cache
    /// </summary>
    public static string HashPow(string key) => $"_HP_{key}";
}
