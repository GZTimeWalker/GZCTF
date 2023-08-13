using System.Threading.Channels;
using GZCTF.Repositories;
using GZCTF.Utils;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Services;

/// <summary>
/// 缓存更新请求
/// </summary>
public class CacheRequest
{
    public string Key { get; set; } = string.Empty;
    public string[] Params { get; set; } = Array.Empty<string>();
    public DistributedCacheEntryOptions? Options { get; set; }

    public CacheRequest(string key, DistributedCacheEntryOptions? options = null, params string[] _params)
    {
        Key = key;
        Params = _params;
        Options = options;
    }
}

/// <summary>
/// 缓存请求处理接口
/// </summary>
public interface ICacheRequestHandler
{
    public string? CacheKey(CacheRequest request);
    public Task<byte[]> Handler(AsyncServiceScope scope, CacheRequest request, CancellationToken token = default);
}

public class CacheMaker : IHostedService
{
    private readonly ILogger<CacheMaker> _logger;
    private readonly IDistributedCache _cache;
    private readonly ChannelReader<CacheRequest> _channelReader;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private CancellationTokenSource _tokenSource { get; set; } = new CancellationTokenSource();
    private readonly Dictionary<string, ICacheRequestHandler> _cacheHandlers = new();

    public CacheMaker(
        ILogger<CacheMaker> logger,
        IDistributedCache cache,
        ChannelReader<CacheRequest> channelReader,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _cache = cache;
        _channelReader = channelReader;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void AddCacheRequestHandler<T>(string key) where T : ICacheRequestHandler, new()
        => _cacheHandlers.Add(key, new T());

    private async Task Maker(CancellationToken token = default)
    {
        _logger.SystemLog($"缓存更新线程已启动", TaskStatus.Pending, LogLevel.Debug);

        try
        {
            await foreach (var item in _channelReader.ReadAllAsync(token))
            {
                if (!_cacheHandlers.ContainsKey(item.Key))
                {
                    _logger.SystemLog($"缓存更新线程未找到匹配的请求：{item.Key}", TaskStatus.NotFound, LogLevel.Warning);
                    continue;
                }

                var handler = _cacheHandlers[item.Key];
                var key = handler.CacheKey(item);

                if (key is null)
                {
                    _logger.SystemLog($"无效的缓存更新请求：{item.Key}", TaskStatus.NotFound, LogLevel.Warning);
                    continue;
                }

                var updateLock = CacheKey.UpdateLock(key);

                if (await _cache.GetAsync(updateLock, token) is not null)
                {
                    // only one GZCTF instance will never encounter this problem
                    _logger.SystemLog($"缓存更新线程已锁定：{key}", TaskStatus.Pending, LogLevel.Debug);
                    continue;
                }

                await using var scope = _serviceScopeFactory.CreateAsyncScope();

                try
                {
                    await _cache.SetAsync(updateLock, Array.Empty<byte>(), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                    }, token);

                    var bytes = await handler.Handler(scope, item, token);

                    if (bytes is not null && bytes.Length > 0)
                    {
                        await _cache.SetAsync(key, bytes, item.Options ?? new DistributedCacheEntryOptions(), token);
                        _logger.SystemLog($"缓存已更新：{key} @ {bytes.Length} bytes", TaskStatus.Success, LogLevel.Debug);
                    }
                    else
                    {
                        _logger.SystemLog($"缓存生成失败：{key}", TaskStatus.Failed, LogLevel.Warning);
                    }
                }
                catch (Exception e)
                {
                    _logger.SystemLog($"缓存更新线程更新失败：{key} @ {e.Message}", TaskStatus.Failed, LogLevel.Error);
                }
                finally
                {
                    await _cache.RemoveAsync(updateLock, token);
                }

                token.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.SystemLog($"任务取消，缓存更新线程将退出", TaskStatus.Exit, LogLevel.Debug);
        }
        finally
        {
            _logger.SystemLog($"缓存更新线程已退出", TaskStatus.Exit, LogLevel.Debug);
        }
    }

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

        _logger.SystemLog("缓存更新已停用", TaskStatus.Success, LogLevel.Debug);

        return Task.CompletedTask;
    }
}
