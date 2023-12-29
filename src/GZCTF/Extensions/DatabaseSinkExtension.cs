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

public class DatabaseSink : ILogEventSink
{
    readonly IServiceProvider _serviceProvider;

    static DateTimeOffset LastFlushTime = DateTimeOffset.FromUnixTimeSeconds(0);
    static readonly List<LogModel> LockedLogBuffer = new();
    static readonly List<LogModel> LogBuffer = new();

    public DatabaseSink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Information) return;

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

        lock (LogBuffer)
        {
            LogBuffer.Add(logModel);

            var needFlush = DateTimeOffset.Now - LastFlushTime > TimeSpan.FromSeconds(10);
            if (!needFlush && LogBuffer.Count < 100) return;

            LockedLogBuffer.AddRange(LogBuffer);
            LogBuffer.Clear();

            Task.Run(Flush);
        }
    }

    async Task Flush()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Logs.AddRangeAsync(LockedLogBuffer);
        await dbContext.SaveChangesAsync();

        LockedLogBuffer.Clear();
        LastFlushTime = DateTimeOffset.Now;
    }
}
