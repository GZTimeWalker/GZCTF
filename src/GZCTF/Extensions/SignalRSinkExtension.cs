using System.Net;
using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Models.Request.Admin;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace GZCTF.Extensions;

public static class SignalRSinkExtension
{
    public static LoggerConfiguration SignalR(this LoggerSinkConfiguration loggerConfiguration,
        IServiceProvider serviceProvider) =>
        loggerConfiguration.Sink(new SignalRSink(serviceProvider), LogEventLevel.Information);
}

public class SignalRSink(IServiceProvider serviceProvider) : ILogEventSink
{
    IHubContext<AdminHub, IAdminClient>? _hubContext;

    public void Emit(LogEvent logEvent)
    {
        _hubContext ??= serviceProvider.GetRequiredService<IHubContext<AdminHub, IAdminClient>>();

        logEvent.Properties.TryGetValue("UserName", out var userName);
        logEvent.Properties.TryGetValue("IP", out var ip);
        logEvent.Properties.TryGetValue("Status", out var status);

        try
        {
            _hubContext.Clients.All.ReceivedLog(
                new LogMessageModel
                {
                    Time = logEvent.Timestamp,
                    Level = logEvent.Level.ToString(),
                    Msg = logEvent.RenderMessageWithExceptions(),
                    UserName = LogHelper.GetLogPropertyValue(userName, "Anonymous"),
                    IP = LogHelper.GetLogPropertyValue<IPAddress>(ip, null),
                    Status = logEvent.Exception is null
                        ? LogHelper.GetLogPropertyValue(status, TaskStatus.Failed)
                        : TaskStatus.Failed,
                }).Wait();
        }
        catch
        {
            // ignored
        }
    }
}
