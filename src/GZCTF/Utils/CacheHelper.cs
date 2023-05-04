using MemoryPack;
using Microsoft.Extensions.Caching.Distributed;

namespace CTFServer.Utils;

public static class CacheHelper
{
    /// <summary>
    /// 获取缓存或重新构建，如果缓存不存在会阻塞
    /// 使用 CacheMaker 和 CacheRequest 代替处理耗时更久的缓存
    /// </summary>
    public static async Task<T> GetOrCreateAsync<T, L>(this IDistributedCache cache,
        ILogger<L> logger,
        string key,
        Func<DistributedCacheEntryOptions, Task<T>> func,
        CancellationToken token = default)
        where T : class
    {
        var value = await cache.GetAsync(key, token);
        T? result = default;

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

        await cache.SetAsync(key, bytes, cacheOptions, token);
        logger.SystemLog($"重建缓存：{key} @ {bytes.Length} bytes", TaskStatus.Success, LogLevel.Debug);

        return result;
    }
}

/// <summary>
/// 缓存标识
/// </summary>
public static class CacheKey
{
    /// <summary>
    /// 积分榜缓存
    /// </summary>
    public static string ScoreBoard(int id) => $"_ScoreBoard_{id}";

    /// <summary>
    /// 比赛通知缓存
    /// </summary>
    public static string ScoreBoard(string id) => $"_ScoreBoard_{id}";

    /// <summary>
    /// 积分榜缓存根标识
    /// </summary>
    public const string ScoreBoardBase = "_ScoreBoard";

    /// <summary>
    /// 比赛通知缓存
    /// </summary>
    public static string GameNotice(int id) => $"_GameNotice_{id}";

    /// <summary>
    /// 比赛通知缓存
    /// </summary>
    public static string GameNotice(string id) => $"_ScoreBoard_{id}";

    /// <summary>
    /// 比赛基础信息缓存
    /// </summary>
    public const string BasicGameInfo = "_BasicGameInfo";

    /// <summary>
    /// 文章
    /// </summary>
    public const string Posts = "_Posts";
}
