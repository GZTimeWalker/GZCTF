using CTFServer.Extensions;
using NpgsqlTypes;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.File.Archive;
using Serilog.Sinks.PostgreSQL;
using Serilog.Templates;
using Serilog.Templates.Themes;
using System.IO.Compression;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace CTFServer.Utils;

public static class LogHelper
{
    /// <summary>
    /// 记录一条系统日志（无用户信息，默认Info）
    /// </summary>
    /// <param name="_logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void SystemLog<T>(this ILogger<T> _logger, string msg, TaskStatus status = TaskStatus.Success, LogLevel? level = null)
        => Log(_logger, msg, "System", string.Empty, status, level ?? LogLevel.Information);

    /// <summary>
    /// 登记一条 Log 记录
    /// </summary>
    /// <param name="_logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="user">用户对象</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void Log<T>(this ILogger<T> _logger, string msg, UserInfo user, TaskStatus status, LogLevel? level = null)
        => Log(_logger, msg, user.UserName, user.IP, status, level);

    /// <summary>
    /// 登记一条 Log 记录
    /// </summary>
    /// <param name="_logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="username">用户名</param>
    /// <param name="context">Http上下文</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void Log<T>(this ILogger<T> _logger, string msg, string username, HttpContext context, TaskStatus status, LogLevel? level = null)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();

        if (ip is null)
            return;

        Log(_logger, msg, username, ip, status, level);
    }

    /// <summary>
    /// 登记一条 Log 记录
    /// </summary>
    /// <param name="_logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="ip">连接IP</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void Log<T>(this ILogger<T> _logger, string msg, string ip, TaskStatus status, LogLevel? level = null)
        => Log(_logger, msg, "Anonymous", ip, status, level);

    /// <summary>
    /// 登记一条 Log 记录
    /// </summary>
    /// <param name="_logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="uname">用户名</param>
    /// <param name="ip">当前IP</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void Log<T>(this ILogger<T> _logger, string msg, string uname, string ip, TaskStatus status, LogLevel? level = null)
    {
        using (_logger.BeginScope("{UserName}{Status}{IP}", uname, status, ip))
        {
            _logger.Log(level ?? LogLevel.Information, msg);
        }
    }

    public static IDictionary<string, ColumnWriterBase> ColumnWriters = new Dictionary<string, ColumnWriterBase>
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

    public static Logger GetLogger(IConfiguration configuration, IServiceProvider serviceProvider)
        => new LoggerConfiguration()
    .Enrich.FromLogContext()
    .MinimumLevel.Debug()
    .Filter.ByExcluding(logEvent =>
         logEvent.Exception != null &&
         logEvent.Exception.GetType() == typeof(OperationCanceledException))
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("AspNetCoreRateLimit", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
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