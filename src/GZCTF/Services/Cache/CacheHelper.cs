﻿using System.Threading.Channels;
using GZCTF.Services.Cache.Handlers;

namespace GZCTF.Services.Cache;

public class CacheHelper(ChannelWriter<CacheRequest> channelWriter)
{
    public async Task FlushScoreboardCache(int gameId, CancellationToken token) =>
        await channelWriter.WriteAsync(ScoreboardCacheHandler.MakeCacheRequest(gameId), token);

    public async Task FlushRecentGamesCache(CancellationToken token) =>
        await channelWriter.WriteAsync(RecentGamesCacheHandler.MakeCacheRequest(), token);

    public async Task FlushGameListCache(CancellationToken token) =>
        await channelWriter.WriteAsync(GameListCacheHandler.MakeCacheRequest(), token);
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
