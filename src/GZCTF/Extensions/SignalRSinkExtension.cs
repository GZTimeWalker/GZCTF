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

        try
        {
            _hubContext.Clients.All.ReceivedLog(
                new LogMessageModel
                {
                    Time = logEvent.Timestamp,
                    Level = logEvent.Level.ToString(),
                    UserName = logEvent.Properties["UserName"].ToString()[1..^1],
                    IP = logEvent.Properties["IP"].ToString()[1..^1],
                    Msg = logEvent.RenderMessage(),
                    Status = logEvent.Properties["Status"].ToString()
                }).Wait();
        }
        catch
        {
            // ignored
        }
    }
}