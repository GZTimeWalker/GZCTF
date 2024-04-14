using System.Collections.Concurrent;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace GZCTF.Extensions;

public static class DatabaseSinkExtension
{
    public static LoggerConfiguration Database(this LoggerSinkConfiguration loggerConfiguration,
        IServiceProvider serviceProvider) =>
        loggerConfiguration.Sink(new DatabaseSink(serviceProvider));
}

public class DatabaseSink : ILogEventSink, IDisposable
{
    readonly IServiceProvider _serviceProvider;

    DateTimeOffset _lastFlushTime = DateTimeOffset.FromUnixTimeSeconds(0);
    readonly CancellationTokenSource _tokenSource = new();
    readonly ConcurrentQueue<LogModel> _logBuffer = [];
    readonly AsyncManualResetEvent _resetEvent = new();

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
        if (logEvent.Level < LogEventLevel.Information)
            return;

        _logBuffer.Enqueue(ToLogModel(logEvent));
        _resetEvent.Set();
    }

    static LogModel ToLogModel(LogEvent logEvent)
    {
        logEvent.Properties.TryGetValue("UserName", out LogEventPropertyValue? userName);
        logEvent.Properties.TryGetValue("SourceContext", out LogEventPropertyValue? sourceContext);
        logEvent.Properties.TryGetValue("IP", out LogEventPropertyValue? ip);
        logEvent.Properties.TryGetValue("Status", out LogEventPropertyValue? status);

        return new LogModel
        {
            TimeUtc = logEvent.Timestamp.ToUniversalTime(),
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            UserName = LogHelper.GetStringValue(userName, "Anonymous"),
            Logger = LogHelper.GetStringValue(sourceContext, "Unknown"),
            RemoteIP = LogHelper.GetStringValue(ip),
            Status = LogHelper.GetStringValue(status),
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

                while (_logBuffer.TryDequeue(out LogModel? logModel))
                    lockedLogBuffer.Add(logModel);

                if (lockedLogBuffer.Count > 50 || DateTimeOffset.Now - _lastFlushTime > TimeSpan.FromSeconds(10))
                {
                    await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();

                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await dbContext.Logs.AddRangeAsync(lockedLogBuffer, token);

                    try
                    {
                        await dbContext.SaveChangesAsync(token);
                    }
                    finally
                    {
                        lockedLogBuffer.Clear();
                        _lastFlushTime = DateTimeOffset.Now;
                    }
                }
            }
        }
        catch (TaskCanceledException) { }
    }
}