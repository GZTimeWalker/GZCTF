using System.Threading.Channels;
using GZCTF.Repositories;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Services.Cache;

/// <summary>
/// 缓存更新请求
/// </summary>
public class CacheRequest(string key, DistributedCacheEntryOptions? options = null, params string[] _params)
{
    public string Key { get; set; } = key;
    public string[] Params { get; set; } = _params;
    public DistributedCacheEntryOptions? Options { get; set; } = options;
}

/// <summary>
/// 缓存请求处理接口
/// </summary>
public interface ICacheRequestHandler
{
    public string? CacheKey(CacheRequest request);
    public Task<byte[]> Handler(AsyncServiceScope scope, CacheRequest request, CancellationToken token = default);
}

public class CacheMaker(
    ILogger<CacheMaker> logger,
    IDistributedCache cache,
    ChannelReader<CacheRequest> channelReader,
    IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    readonly Dictionary<string, ICacheRequestHandler> _cacheHandlers = new();
    CancellationTokenSource _tokenSource { get; set; } = new();

    public Task StartAsync(CancellationToken token)
    {
        _tokenSource = new CancellationTokenSource();

        #region Add Handlers

        AddCacheRequestHandler<ScoreboardCacheHandler>(CacheKey.ScoreBoardBase);

        #endregion

        _ = Maker(_tokenSource.Token);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken token)
    {
        _tokenSource.Cancel();

        logger.SystemLog("缓存更新已停用", TaskStatus.Success, LogLevel.Debug);

        return Task.CompletedTask;
    }

    public void AddCacheRequestHandler<T>(string key) where T : ICacheRequestHandler, new() => _cacheHandlers.Add(key, new T());

    async Task Maker(CancellationToken token = default)
    {
        logger.SystemLog("缓存更新线程已启动", TaskStatus.Pending, LogLevel.Debug);

        try
        {
            await foreach (CacheRequest item in channelReader.ReadAllAsync(token))
            {
                if (!_cacheHandlers.ContainsKey(item.Key))
                {
                    logger.SystemLog($"缓存更新线程未找到匹配的请求：{item.Key}", TaskStatus.NotFound, LogLevel.Warning);
                    continue;
                }

                ICacheRequestHandler handler = _cacheHandlers[item.Key];
                var key = handler.CacheKey(item);

                if (key is null)
                {
                    logger.SystemLog($"无效的缓存更新请求：{item.Key}", TaskStatus.NotFound, LogLevel.Warning);
                    continue;
                }

                var updateLock = CacheKey.UpdateLock(key);

                if (await cache.GetAsync(updateLock, token) is not null)
                {
                    // only one GZCTF instance will never encounter this problem
                    logger.SystemLog($"缓存更新线程已锁定：{key}", TaskStatus.Pending, LogLevel.Debug);
                    continue;
                }

                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();

                try
                {
                    await cache.SetAsync(updateLock, Array.Empty<byte>(),
                        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) },
                        token);

                    var bytes = await handler.Handler(scope, item, token);

                    if (bytes is not null && bytes.Length > 0)
                    {
                        await cache.SetAsync(key, bytes, item.Options ?? new DistributedCacheEntryOptions(), token);
                        logger.SystemLog($"缓存已更新：{key} @ {bytes.Length} bytes", TaskStatus.Success, LogLevel.Debug);
                    }
                    else
                    {
                        logger.SystemLog($"缓存生成失败：{key}", TaskStatus.Failed, LogLevel.Warning);
                    }
                }
                catch (Exception e)
                {
                    logger.SystemLog($"缓存更新线程更新失败：{key} @ {e.Message}", TaskStatus.Failed, LogLevel.Error);
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
            logger.SystemLog("任务取消，缓存更新线程将退出", TaskStatus.Exit, LogLevel.Debug);
        }
        finally
        {
            logger.SystemLog("缓存更新线程已退出", TaskStatus.Exit, LogLevel.Debug);
        }
    }
}