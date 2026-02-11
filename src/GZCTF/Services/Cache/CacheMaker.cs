using System.Threading.Channels;
using GZCTF.Services.Cache.Handlers;
using MemoryPack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace GZCTF.Services.Cache;

/// <summary>
/// Cache update request
/// </summary>
public class CacheRequest(
    string key,
    DistributedCacheEntryOptions? options = null,
    params string[] @params)
{
    public DateTimeOffset Time { get; } = DateTimeOffset.Now;
    public string Key { get; } = key;
    public string[] Params { get; } = @params;
    public DistributedCacheEntryOptions? Options { get; } = options;
}

/// <summary>
/// Cache request handler
/// </summary>
public interface ICacheRequestHandler
{
    public string? CacheKey(CacheRequest request);
    public Task<byte[]> Handle(AsyncServiceScope scope, CacheRequest request, CancellationToken token = default);
}

public class CacheMaker(
    ILogger<CacheMaker> logger,
    IDistributedCache cache,
    IMemoryCache memoryCache,
    ChannelReader<CacheRequest> channelReader,
    IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    private readonly Dictionary<string, ICacheRequestHandler> _cacheHandlers = new();
    private CancellationTokenSource TokenSource { get; set; } = new();

    public async Task StartAsync(CancellationToken token)
    {
        TokenSource = new CancellationTokenSource();

        #region Add Handlers

        AddCacheRequestHandler<ScoreboardCacheHandler>(CacheKey.ScoreBoardBase);
        AddCacheRequestHandler<RecentGamesCacheHandler>(CacheKey.RecentGames);
        AddCacheRequestHandler<GameListCacheHandler>(CacheKey.GameList);

        #endregion

        await Task.Factory.StartNew(() => Maker(TokenSource.Token), token, TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public Task StopAsync(CancellationToken token)
    {
        TokenSource.Cancel();

        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.Cache_Stopped)], TaskStatus.Success,
            LogLevel.Debug);

        return Task.CompletedTask;
    }

    public void AddCacheRequestHandler<T>(string key) where T : ICacheRequestHandler, new() =>
        _cacheHandlers.Add(key, new T());

    private async Task Maker(CancellationToken token = default)
    {
        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.Cache_WorkerStarted)], TaskStatus.Pending,
            LogLevel.Debug);

        try
        {
            CacheRequest? buffered = null;

            while (!token.IsCancellationRequested)
            {
                CacheRequest item;
                if (buffered is not null)
                {
                    item = buffered;
                    buffered = null;
                }
                else
                {
                    if (!await channelReader.WaitToReadAsync(token))
                        break;
                    if (!channelReader.TryRead(out var readItem) || readItem is null)
                        continue;
                    item = readItem;
                }

                if (!_cacheHandlers.TryGetValue(item.Key, out var handler))
                {
                    logger.SystemLog(
                        StaticLocalizer[nameof(Resources.Program.Cache_NoMatchingRequest), item.Key],
                        TaskStatus.NotFound,
                        LogLevel.Warning);
                    continue;
                }

                var key = handler.CacheKey(item);

                if (key is null)
                {
                    logger.SystemLog(
                        StaticLocalizer[nameof(Resources.Program.Cache_InvalidUpdateRequest), item.Key],
                        TaskStatus.NotFound,
                        LogLevel.Warning);
                    continue;
                }

                // Coalesce queued duplicate refresh requests for the same cache key.
                // This reduces burst pressure without dropping the latest state.
                while (channelReader.TryRead(out var next))
                {
                    if (next.Key == item.Key &&
                        next.Params.Length == item.Params.Length &&
                        next.Params.SequenceEqual(item.Params))
                    {
                        item = next;
                        continue;
                    }

                    buffered = next;
                    break;
                }

                var updateLock = CacheKey.UpdateLock(key);

                if (await cache.GetAsync(updateLock, token) is not null)
                {
                    // only one GZCTF instance will never encounter this
                    logger.SystemLog(StaticLocalizer[nameof(Resources.Program.Cache_InvalidUpdateRequest), key],
                        TaskStatus.Pending,
                        LogLevel.Debug);
                    continue;
                }

                var lastUpdateKey = CacheKey.LastUpdateTime(key);
                var lastUpdateBytes = MemoryPackSerializer.Serialize(DateTimeOffset.UtcNow);
                await cache.SetAsync(lastUpdateKey, lastUpdateBytes, new(), token);

                await using var scope = serviceScopeFactory.CreateAsyncScope();

                try
                {
                    await cache.SetAsync(updateLock, [],
                        new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(1) },
                        token);

                    var bytes = await handler.Handle(scope, item, token);

                    if (bytes.Length > 0)
                    {
                        await cache.SetAsync(key, bytes, item.Options ?? new DistributedCacheEntryOptions(), token);
                        logger.SystemLog(
                            StaticLocalizer[
                                nameof(Resources.Program.Cache_Updated),
                                key, item.Time.ToString("HH:mm:ss.fff"), bytes.Length
                            ], TaskStatus.Success, LogLevel.Debug);

                        // notify local memory cache
                        memoryCache.Remove(key);
                    }
                    else
                    {
                        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.Cache_GenerationFailed), key],
                            TaskStatus.Failed,
                            LogLevel.Warning);
                    }
                }
                catch (Exception e)
                {
                    logger.SystemLog(
                        StaticLocalizer[nameof(Resources.Program.Cache_UpdateWorkerFailed), key, e.Message],
                        TaskStatus.Failed,
                        LogLevel.Error);
                }
                finally
                {
                    await cache.RemoveAsync(updateLock, token);
                }

                token.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException)
        {
            logger.SystemLog(StaticLocalizer[nameof(Resources.Program.Cache_WorkerCancelled)], TaskStatus.Exit,
                LogLevel.Debug);
        }
        finally
        {
            logger.SystemLog(StaticLocalizer[nameof(Resources.Program.Cache_WorkerStopped)], TaskStatus.Exit,
                LogLevel.Debug);
        }
    }
}
