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
        loggerConfiguration.Sink(new SignalRSink(serviceProvider));
}

public class SignalRSink(IServiceProvider serviceProvider) : ILogEventSink
{
    IHubContext<AdminHub, IAdminClient>? _hubContext;

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Information)
            return;

        _hubContext ??= serviceProvider.GetRequiredService<IHubContext<AdminHub, IAdminClient>>();

        logEvent.Properties.TryGetValue("UserName", out LogEventPropertyValue? userName);
        logEvent.Properties.TryGetValue("IP", out LogEventPropertyValue? ip);
        logEvent.Properties.TryGetValue("Status", out LogEventPropertyValue? status);

        try
        {
            _hubContext.Clients.All.ReceivedLog(
                new LogMessageModel
                {
                    Time = logEvent.Timestamp,
                    Level = logEvent.Level.ToString(),
                    Msg = logEvent.RenderMessageWithExceptions(),
                    UserName = LogHelper.GetStringValue(userName, "Anonymous"),
                    IP = LogHelper.GetStringValue(ip),
                    Status = logEvent.Exception is null
                        ? LogHelper.GetStringValue(status)
                        : TaskStatus.Failed.ToString(),
                }).Wait();
        }
        catch
        {
            // ignored
        }
    }
}
