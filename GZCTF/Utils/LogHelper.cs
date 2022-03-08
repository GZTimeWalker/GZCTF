using CTFServer.Models;
using NLog;
using LogLevel = NLog.LogLevel;

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
    public static void SystemLog(Logger _logger, string msg, TaskStatus status = TaskStatus.Success, LogLevel? level = null)
        => Log(_logger, msg, "System", "-", status, level ?? LogLevel.Debug);

    /// <summary>
    /// 登记一条 Log 记录
    /// </summary>
    /// <param name="_logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="user">用户对象</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void Log(Logger _logger, string msg, UserInfo user, TaskStatus status, LogLevel? level = null)
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
    public static void Log(Logger _logger, string msg, string username, HttpContext context, TaskStatus status, LogLevel? level = null)
    {
        var remoteAddress = context.Connection.RemoteIpAddress;

        if (remoteAddress is null)
            return;

        string ip = remoteAddress.IsIPv4MappedToIPv6 ? remoteAddress.MapToIPv4().ToString() : remoteAddress.MapToIPv6().ToString().Replace("::ffff:", "");

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
    public static void Log(Logger _logger, string msg, string ip, TaskStatus status, LogLevel? level = null)
        => Log(_logger, msg, "Anonymous", ip, status, level);

    /// <summary>
    /// 登记一条 Log 记录
    /// </summary>
    /// <param name="_logger">传入的 Nlog.Logger</param>
    /// <param name="msg">Log 消息</param>
    /// <param name="username">用户名</param>
    /// <param name="ip">当前IP</param>
    /// <param name="status">操作执行结果</param>
    /// <param name="level">Log 级别</param>
    public static void Log(Logger _logger, string msg, string username, string ip, TaskStatus status, LogLevel? level = null)
    {
        LogEventInfo logEventInfo = new(level ?? LogLevel.Info, _logger.Name, msg);
        logEventInfo.Properties["uname"] = username;
        logEventInfo.Properties["ip"] = ip;
        logEventInfo.Properties["status"] = status.ToString();
        _logger.Log(logEventInfo);
    }
}
