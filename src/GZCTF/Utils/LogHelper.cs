using System.IO.Compression;
using System.Net;
using GZCTF.Extensions;
using NpgsqlTypes;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.File.Archive;
using Serilog.Sinks.PostgreSQL;
using Serilog.Templates;
using Serilog.Templates.Themes;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace GZCTF.Utils;

public static class LogHelper
{
    /// <summary>
    /// 记录一条系统日志（无用户信息，默认Info）
    /// </summary>
    /// <param name="logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void SystemLog<T>(this ILogger<T> logger, string msg, TaskStatus status = TaskStatus.Success, LogLevel? level = null)
        => Log(logger, msg, "System", string.Empty, status, level ?? LogLevel.Information);

    /// <summary>
    /// 登记一条 Log 记录
    /// </summary>
    /// <param name="logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="user">用户对象</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void Log<T>(this ILogger<T> logger, string msg, UserInfo? user, TaskStatus status, LogLevel? level = null)
        => Log(logger, msg, user?.UserName ?? "Anonymous", user?.IP ?? "0.0.0.0", status, level);

    /// <summary>
    /// 登记一条 Log 记录
    /// </summary>
    /// <param name="logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="context">Http上下文</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void Log<T>(this ILogger<T> logger, string msg, HttpContext? context, TaskStatus status, LogLevel? level = null)
    {
        var ip = context?.Connection?.RemoteIpAddress?.ToString() ?? IPAddress.Loopback.ToString();
        var username = context?.User?.Identity?.Name ?? "Anonymous";

        Log(logger, msg, username, ip, status, level);
    }

    /// <summary>
    /// 登记一条 Log 记录
    /// </summary>
    /// <param name="logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="ip">连接IP</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void Log<T>(this ILogger<T> logger, string msg, string ip, TaskStatus status, LogLevel? level = null)
        => Log(logger, msg, "Anonymous", ip, status, level);

    /// <summary>
    /// 登记一条 Log 记录
    /// </summary>
    /// <param name="logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="uname">用户名</param>
    /// <param name="ip">当前IP</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void Log<T>(this ILogger<T> logger, string msg, string uname, string ip, TaskStatus status, LogLevel? level = null)
    {
        using (logger.BeginScope("{UserName}{Status}{IP}", uname, status, ip))
        {
            logger.Log(level ?? LogLevel.Information, msg);
        }
    }

    public static void UseRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "[{StatusCode}] {Elapsed,8:####0.00}ms HTTP {RequestMethod,-6} {RequestPath} @ {RemoteIP}";
            options.GetLevel = (context, time, ex) =>
                time > 10000 && context.Response.StatusCode != 101 ? LogEventLevel.Warning :
                (context.Response.StatusCode > 499 || ex is not null) ? LogEventLevel.Error : LogEventLevel.Debug;
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
            };
        });
    }

    public static IDictionary<string, ColumnWriterBase> ColumnWriters => new Dictionary<string, ColumnWriterBase>()
    {
        {"Message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
        {"Level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
        {"TimeUTC", new TimeColumnWriter() },
        {"Exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
        {"Logger", new SinglePropertyColumnWriter("SourceContext", PropertyWriteMethod.Raw, NpgsqlDbType.Varchar) },
        {"UserName", new SinglePropertyColumnWriter("UserName", PropertyWriteMethod.Raw, NpgsqlDbType.Varchar) },
        {"Status", new SinglePropertyColumnWriter("Status", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar) },
        {"RemoteIP", new SinglePropertyColumnWriter("IP", PropertyWriteMethod.Raw, NpgsqlDbType.Varchar) }
    };

    private const string LogTemplate = "[{@t:yy-MM-dd HH:mm:ss.fff} {@l:u3}] {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}: {@m} {#if Length(Status) > 0}#{Status} <{UserName}>{#if Length(IP) > 0}@{IP}{#end}{#end}\n{@x}";
    private const string InitLogTemplate = "[{@t:yy-MM-dd HH:mm:ss.fff} {@l:u3}] {@m}\n{@x}";

    public static Serilog.ILogger GetInitLogger()
        => new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("AspNetCoreRateLimit", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            .WriteTo.Async(t => t.Console(
                formatter: new ExpressionTemplate(InitLogTemplate, theme: TemplateTheme.Literate),
                restrictedToMinimumLevel: LogEventLevel.Debug
            ))
            .CreateBootstrapLogger();

    public static Serilog.ILogger GetLogger(IConfiguration configuration, IServiceProvider serviceProvider)
        => new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .Filter.ByExcluding(logEvent =>
                 logEvent.Exception != null &&
                 logEvent.Exception.GetType() == typeof(OperationCanceledException))
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("AspNetCoreRateLimit", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            .WriteTo.Async(t => t.Console(
                formatter: new ExpressionTemplate(LogTemplate, theme: TemplateTheme.Literate),
                restrictedToMinimumLevel: LogEventLevel.Debug
            ))
            .WriteTo.Async(t => t.File(
                path: "log/log_.log",
                formatter: new ExpressionTemplate(LogTemplate),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                restrictedToMinimumLevel: LogEventLevel.Debug,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 5,
                hooks: new ArchiveHooks(CompressionLevel.Optimal, "log/archive/{UtcDate:yyyy-MM}")
            ))
            .WriteTo.Async(t => t.PostgreSQL(
                connectionString: configuration.GetConnectionString("Database"),
                tableName: "Logs",
                respectCase: true,
                columnOptions: ColumnWriters,
                restrictedToMinimumLevel: LogEventLevel.Information,
                period: TimeSpan.FromSeconds(30)
            ))
            .WriteTo.SignalR(serviceProvider)
            .CreateLogger();
}

public class TimeColumnWriter : ColumnWriterBase
{
    public TimeColumnWriter() : base(NpgsqlDbType.TimestampTz)
    {
    }

    public override object GetValue(LogEvent logEvent, IFormatProvider? formatProvider = null)
        => logEvent.Timestamp.ToUniversalTime();
}
