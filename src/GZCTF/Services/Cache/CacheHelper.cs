using System.Threading.Channels;
using GZCTF.Repositories;

namespace GZCTF.Services.Cache;

public class CacheHelper(ChannelWriter<CacheRequest> channelWriter)
{
    public async Task FlushScoreboardCache(int gameId, CancellationToken token) =>
        await channelWriter.WriteAsync(ScoreboardCacheHandler.MakeCacheRequest(gameId), token);
}

/// <summary>
/// 缓存标识
/// </summary>
public static class CacheKey
{
    /// <summary>
    /// 积分榜缓存根标识
    /// </summary>
    public const string ScoreBoardBase = "_ScoreBoard";

    /// <summary>
    /// 比赛基础信息缓存
    /// </summary>
    public const string BasicGameInfo = "_BasicGameInfo";

    /// <summary>
    /// 文章
    /// </summary>
    public const string Posts = "_Posts";

    /// <summary>
    /// 练习是否可用（题目总数不为空）
    /// </summary>
    public const string ExerciseAvailable = "_ExerciseAvailable";

    /// <summary>
    /// 客户端配置
    /// </summary>
    public const string ClientConfig = "_ClientConfig";

    /// <summary>
    /// 验证码配置
    /// </summary>
    public const string CaptchaConfig = "_CaptchaConfig";

    /// <summary>
    /// 缓存更新锁
    /// </summary>
    public static string UpdateLock(string key) => $"_CacheUpdateLock_{key}";

    /// <summary>
    /// 积分榜缓存
    /// </summary>
    public static string ScoreBoard(int id) => $"_ScoreBoard_{id}";

    /// <summary>
    /// 积分榜缓存
    /// </summary>
    public static string ScoreBoard(string id) => $"_ScoreBoard_{id}";

    /// <summary>
    /// 比赛通知缓存
    /// </summary>
    public static string GameNotice(int id) => $"_GameNotice_{id}";

    /// <summary>
    /// 容器连接数缓存
    /// </summary>
    public static string ConnectionCount(Guid id) => $"_Container_Conn_{id}";
}
