using System.Collections.Concurrent;
using System.Net;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace GZCTF.Extensions;

public static class DatabaseSinkExtension
{
    public static LoggerConfiguration Database(this LoggerSinkConfiguration loggerConfiguration,
        IServiceProvider serviceProvider) =>
        loggerConfiguration.Sink(new DatabaseSink(serviceProvider), LogEventLevel.Information);
}

public class DatabaseSink : ILogEventSink, IDisposable
{
    readonly ConcurrentQueue<LogModel> _logBuffer = [];
    readonly AsyncManualResetEvent _resetEvent = new();
    readonly IServiceProvider _serviceProvider;
    readonly CancellationTokenSource _tokenSource = new();

    DateTimeOffset _lastFlushTime = DateTimeOffset.FromUnixTimeSeconds(0);

    public DatabaseSink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Task.Factory.StartNew(
            () => WriteToDatabase(_tokenSource.Token),
            _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void Dispose()
    {
        _tokenSource.Cancel();
        GC.SuppressFinalize(this);
    }

    public void Emit(LogEvent logEvent)
    {
        _logBuffer.Enqueue(ToLogModel(logEvent));
        _resetEvent.Set();
    }

    static LogModel ToLogModel(LogEvent logEvent)
    {
        logEvent.Properties.TryGetValue("UserName", out var userName);
        logEvent.Properties.TryGetValue("SourceContext", out var sourceContext);
        logEvent.Properties.TryGetValue("IP", out var ip);
        logEvent.Properties.TryGetValue("Status", out var status);

        return new LogModel
        {
            TimeUtc = logEvent.Timestamp.ToUniversalTime(),
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessageWithExceptions(),
            UserName = LogHelper.GetLogPropertyValue(userName, "Anonymous"),
            Logger = LogHelper.GetLogPropertyValue<string>(sourceContext, "Unknown") ?? string.Empty,
            RemoteIP = LogHelper.GetLogPropertyValue<IPAddress>(ip, null),
            Status = logEvent.Exception is null
                ? LogHelper.GetLogPropertyValue(status, TaskStatus.Failed)
                : TaskStatus.Failed,
            Exception = logEvent.Exception?.ToString()
        };
    }

    async Task WriteToDatabase(CancellationToken token = default)
    {
        List<LogModel> lockedLogBuffer = [];

        try
        {
            while (!token.IsCancellationRequested)
            {
                await _resetEvent.WaitAsync(token);
                _resetEvent.Reset();

                while (_logBuffer.TryDequeue(out var logModel))
                    lockedLogBuffer.Add(logModel);

                if (lockedLogBuffer.Count <= 50 && DateTimeOffset.Now - _lastFlushTime <= TimeSpan.FromSeconds(10))
                    continue;

                await using var scope = _serviceProvider.CreateAsyncScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Logs.AddRangeAsync(lockedLogBuffer, token);
                var affectedRows = 0;

                try
                {
                    affectedRows = await dbContext.SaveChangesAsync(token);
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSink>>();
                    logger.LogErrorMessage(ex);
                }
                finally
                {
                    if (affectedRows > 0)
                    {
                        // If some logs were saved, we need to remove them from the buffer
                        lockedLogBuffer.RemoveAll(logModel => logModel.Id != 0);
                    }

                    _lastFlushTime = DateTimeOffset.Now;
                }
            }
        }
        catch (TaskCanceledException) { }
    }
}
