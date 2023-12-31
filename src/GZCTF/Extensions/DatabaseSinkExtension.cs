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
    readonly ConcurrentQueue<LogModel> _logBuffer = new();

    public DatabaseSink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Task.Run(() => WriteToDatabase(_tokenSource.Token), _tokenSource.Token);
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
            TimeUTC = logEvent.Timestamp.ToUniversalTime(),
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            UserName = logEvent.Properties["UserName"].ToString()[1..^1],
            Logger = logEvent.Properties["SourceContext"].ToString()[1..^1],
            RemoteIP = logEvent.Properties["IP"].ToString()[1..^1],
            Status = logEvent.Properties["Status"].ToString(),
            Exception = logEvent.Exception?.ToString()
        };

        _logBuffer.Enqueue(logModel);
    }

    async Task WriteToDatabase(CancellationToken token = default)
    {
        List<LogModel> lockedLogBuffer = new();

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