using System.Collections.Concurrent;
using Namotion.Reflection;
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

        LogModel logModel = new()
        {
            TimeUtc = logEvent.Timestamp.ToUniversalTime(),
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            UserName = logEvent.TryGetPropertyValue<string>("UserName")?[1..^1],
            Logger = logEvent.TryGetPropertyValue<string>("SourceContext")?[1..^1] ?? "",
            RemoteIP = logEvent.TryGetPropertyValue<string>("IP")?[1..^1],
            Status = logEvent.TryGetPropertyValue<TaskStatus>("Status").ToString(),
            Exception = logEvent.Exception?.ToString()
        };

        _logBuffer.Enqueue(logModel);
    }

    async Task WriteToDatabase(CancellationToken token = default)
    {
        List<LogModel> lockedLogBuffer = [];

        try
        {
            while (!token.IsCancellationRequested)
            {
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

                await Task.Delay(TimeSpan.FromSeconds(1), token);
            }
        }
        catch (TaskCanceledException) { }
    }
}