﻿using System.Threading.Channels;
using GZCTF.Repositories;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Services.Cache;

/// <summary>
/// 缓存更新请求
/// </summary>
public class CacheRequest(string key, DistributedCacheEntryOptions? options = null, params string[] @params)
{
    public string Key { get; set; } = key;
    public string[] Params { get; set; } = @params;
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
    CancellationTokenSource TokenSource { get; set; } = new();

    public async Task StartAsync(CancellationToken token)
    {
        TokenSource = new CancellationTokenSource();

        #region Add Handlers

        AddCacheRequestHandler<ScoreboardCacheHandler>(CacheKey.ScoreBoardBase);

        #endregion

        await Task.Factory.StartNew(() => Maker(TokenSource.Token), token, TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public Task StopAsync(CancellationToken token)
    {
        TokenSource.Cancel();

        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.Cache_Stopped)], TaskStatus.Success,
            LogLevel.Debug);

        return Task.CompletedTask;
    }

    public void AddCacheRequestHandler<T>(string key) where T : ICacheRequestHandler, new() =>
        _cacheHandlers.Add(key, new T());

    async Task Maker(CancellationToken token = default)
    {
        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.Cache_WorkerStarted)], TaskStatus.Pending,
            LogLevel.Debug);

        try
        {
            await foreach (CacheRequest item in channelReader.ReadAllAsync(token))
            {
                if (!_cacheHandlers.TryGetValue(item.Key, out ICacheRequestHandler? handler))
                {
                    logger.SystemLog(
                        Program.StaticLocalizer[nameof(Resources.Program.Cache_NoMatchingRequest), item.Key],
                        TaskStatus.NotFound,
                        LogLevel.Warning);
                    continue;
                }

                var key = handler.CacheKey(item);

                if (key is null)
                {
                    logger.SystemLog(
                        Program.StaticLocalizer[nameof(Resources.Program.Cache_InvalidUpdateRequest), item.Key],
                        TaskStatus.NotFound,
                        LogLevel.Warning);
                    continue;
                }

                var updateLock = CacheKey.UpdateLock(key);

                if (await cache.GetAsync(updateLock, token) is not null)
                {
                    // only one GZCTF instance will never encounter this problem
                    logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.Cache_InvalidUpdateRequest), key],
                        TaskStatus.Pending,
                        LogLevel.Debug);
                    continue;
                }

                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();

                try
                {
                    await cache.SetAsync(updateLock, [],
                        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) },
                        token);

                    var bytes = await handler.Handler(scope, item, token);

                    if (bytes.Length > 0)
                    {
                        await cache.SetAsync(key, bytes, item.Options ?? new DistributedCacheEntryOptions(), token);
                        logger.SystemLog(
                            Program.StaticLocalizer[nameof(Resources.Program.Cache_Updated), key, bytes.Length],
                            TaskStatus.Success,
                            LogLevel.Debug);
                    }
                    else
                    {
                        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.Cache_GenerationFailed), key],
                            TaskStatus.Failed,
                            LogLevel.Warning);
                    }
                }
                catch (Exception e)
                {
                    logger.SystemLog(
                        Program.StaticLocalizer[nameof(Resources.Program.Cache_UpdateWorkerFailed), key, e.Message],
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
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.Cache_WorkerCancelled)], TaskStatus.Exit,
                LogLevel.Debug);
        }
        finally
        {
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.Cache_WorkerStopped)], TaskStatus.Exit,
                LogLevel.Debug);
        }
    }
}
