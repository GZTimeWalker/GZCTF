using CTFServer.Models;
using NLog;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace CTFServer.Utils;

public static class LogHelper
{
    /// <summary>
    /// 记录一条系统日志（无用户信息，默认Debug）
    /// </summary>
    /// <param name="_logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void SystemLog<T>(this ILogger<T> _logger, string msg, TaskStatus status = TaskStatus.Success, LogLevel? level = null)
        => Log(_logger, msg, "System", "-", status, level ?? LogLevel.Information);

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
        => _logger.Log(level ?? LogLevel.Information, msg, new { uname, ip, status });
}
