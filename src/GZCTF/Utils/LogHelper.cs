using System.IO.Compression;
using System.Net;
using System.Text;
using GZCTF.Extensions;
using GZCTF.Models.Internal;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.File.Archive;
using Serilog.Sinks.Grafana.Loki;
using Serilog.Templates;
using Serilog.Templates.Themes;
using ILogger = Serilog.ILogger;

namespace GZCTF.Utils;

public static class LogHelper
{
    const string LogTemplate = "[{@t:yy-MM-dd HH:mm:ss.fff} {@l:u3}] " +
                               "{Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}: " +
                               "{@m} {#if Length(Status) > 0}#{Status} <{UserName}>" +
                               "{#if Length(IP) > 0} @ {IP}{#end}{#end}\n{@x}";

    const string InitLogTemplate = "[{@t:yy-MM-dd HH:mm:ss.fff} {@l:u3}] {@m}\n{@x}";

    /// <summary>
    /// Record a system log (no user information, default Info level)
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="msg">message</param>
    /// <param name="status">task status</param>
    /// <param name="level">log level</param>
    public static void SystemLog<T>(this ILogger<T> logger, string msg, TaskStatus status = TaskStatus.Success,
        LogLevel? level = null) =>
        Log(logger, msg, "System", string.Empty, status, level ?? LogLevel.Information);

    /// <summary>
    /// Log an error, no formatting
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="msg"></param>
    /// <param name="exception"></param>
    /// <typeparam name="T"></typeparam>
    public static void LogErrorMessage<T>(this ILogger<T> logger, Exception exception, string msg) =>
        logger.LogError(exception, "{msg:l}", msg);

    /// <summary>
    /// Log an error, no formatting
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="exception"></param>
    /// <typeparam name="T"></typeparam>
    public static void LogErrorMessage<T>(this ILogger<T> logger, Exception exception) =>
        logger.LogError(exception, "{msg:l}", exception.Message);

    /// <summary>
    /// Record a log
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="msg">message</param>
    /// <param name="status">task status</param>
    /// <param name="level">log level</param>
    /// <param name="user">the user</param>
    public static void Log<T>(this ILogger<T> logger, string msg, UserInfo? user, TaskStatus status,
        LogLevel? level = null) =>
        Log(logger, msg, user?.UserName ?? "Anonymous", user?.IP ?? "0.0.0.0", status, level);

    /// <summary>
    /// Record a log
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="msg">message</param>
    /// <param name="status">task status</param>
    /// <param name="level">log level</param>
    /// <param name="context">http context</param>
    public static void Log<T>(this ILogger<T> logger, string msg, HttpContext? context, TaskStatus status,
        LogLevel? level = null)
    {
        var ip = context?.Connection.RemoteIpAddress?.ToString() ?? IPAddress.Loopback.ToString();
        var username = context?.User.Identity?.Name ?? "Anonymous";

        Log(logger, msg, username, ip, status, level);
    }

    /// <summary>
    /// Record a log
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="msg">message</param>
    /// <param name="status">task status</param>
    /// <param name="level">log level</param>
    /// <param name="ip">ip</param>
    public static void Log<T>(this ILogger<T> logger, string msg, string ip, TaskStatus status, LogLevel? level = null)
        => Log(logger, msg, "Anonymous", ip, status, level);

    /// <summary>
    /// Record a log
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="msg">message</param>
    /// <param name="status">task status</param>
    /// <param name="level">log level</param>
    /// <param name="uname">user name</param>
    /// <param name="ip">ip</param>
    public static void Log<T>(this ILogger<T> logger, string msg, string uname, string ip, TaskStatus status,
        LogLevel? level = null)
    {
        using (LogContext.PushProperty("UserName", uname))
        using (LogContext.PushProperty("IP", ip))
        using (LogContext.PushProperty("Status", status.ToString()))
        {
            logger.Log(level ?? LogLevel.Information, "{msg:l}", msg);
        }
    }

    public static void UseRequestLogging(this WebApplication app) =>
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "[{StatusCode}] {Elapsed,8:####0.00}ms HTTP {RequestMethod,-6} {RequestPath} @ {IP}";
            options.GetLevel = (context, time, ex) =>
                context.Response.StatusCode == 204 ? LogEventLevel.Verbose :
                time > 10000 && context.Response.StatusCode != 101 ? LogEventLevel.Warning :
                context.Response.StatusCode > 499 || ex is not null ? LogEventLevel.Error : LogEventLevel.Debug;

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("IP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            };
        });

    public static ILogger GetInitLogger() =>
        new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("AspNetCoreRateLimit", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            .WriteTo.Async(t => t.Console(
                new ExpressionTemplate(InitLogTemplate, theme: TemplateTheme.Literate),
                LogEventLevel.Debug
            ))
            .CreateBootstrapLogger();

    public static ILogger GetLogger(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Filter.ByExcluding(
                Matching.WithProperty<string>("RequestPath", v =>
                    v.TrimEnd('/').Equals("/healthz", StringComparison.OrdinalIgnoreCase) ||
                    v.TrimEnd('/').Equals("/metrics", StringComparison.OrdinalIgnoreCase) ||
                    v.StartsWith("/assets", StringComparison.OrdinalIgnoreCase)))
            .Filter.ByExcluding(logEvent =>
                logEvent.Exception is OperationCanceledException)
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("AspNetCoreRateLimit", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            .WriteTo.Async(t => t.Console(
                new ExpressionTemplate(LogTemplate, theme: TemplateTheme.Literate),
                LogEventLevel.Debug))
            .WriteTo.Async(t => t.File(
                path: Path.Combine(PathHelper.Base, PathHelper.Logs, "log_.log"),
                formatter: new ExpressionTemplate(LogTemplate),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                restrictedToMinimumLevel: LogEventLevel.Debug,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 5,
                hooks: new ArchiveHooks(CompressionLevel.Optimal,
                    Path.Combine(PathHelper.Base, PathHelper.Logs, "archive", "{UtcDate:yyyy-MM}"))
            ))
            .WriteTo.Database(serviceProvider)
            .WriteTo.SignalR(serviceProvider);

        if (configuration.GetSection("Logging").GetSection("Loki") is not { } lokiSection || !lokiSection.Exists())
            return loggerConfig.CreateLogger();

        if (lokiSection.Get<GrafanaLokiOptions>() is { Enable: true, EndpointUri: not null } lokiOptions)
            loggerConfig = loggerConfig.WriteTo.GrafanaLoki(
                lokiOptions.EndpointUri,
                lokiOptions.Labels ?? [new() { Key = "app", Value = "gzctf" }],
                lokiOptions.PropertiesAsLabels,
                lokiOptions.Credentials,
                lokiOptions.Tenant,
                (LogEventLevel)(lokiOptions.MinimumLevel ?? LogLevel.Trace));

        return loggerConfig.CreateLogger();
    }

    public static string GetStringValue(LogEventPropertyValue? value, string defaultValue = "")
    {
        if (value is ScalarValue { Value: string rawValue })
            return rawValue;
        return value?.ToString() ?? defaultValue;
    }

    public static string RenderMessageWithExceptions(this LogEvent logEvent)
    {
        if (logEvent.Exception is null)
            return logEvent.RenderMessage();

        var exception = logEvent.Exception;
        var sb = new StringBuilder(logEvent.RenderMessage());

        sb.AppendLine();
        while (true)
        {
            sb.Append($"{exception.GetType()}: {exception.Message}");
            exception = exception.InnerException;

            if (exception is null)
                break;

            sb.AppendLine();
            sb.Append(" ---> ");
        }

        return sb.ToString();
    }
}
