using CTFServer.Hubs;
using CTFServer.Hubs.Client;
using CTFServer.Models.Request.Admin;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace CTFServer.Extensions;

public static class SignalRSinkExtension
{
    public static LoggerConfiguration SignalR(this LoggerSinkConfiguration loggerConfiguration, IServiceProvider serviceProvider)
        => loggerConfiguration.Sink(new SignalRSink(serviceProvider));
}

public class SignalRSink : ILogEventSink
{
    private readonly IServiceProvider serviceProvider;
    private IHubContext<AdminHub, IAdminClient>? hubContext;

    public SignalRSink(IServiceProvider _serviceProvider)
    {
        serviceProvider = _serviceProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        if (hubContext is null)
            hubContext = serviceProvider.GetRequiredService<IHubContext<AdminHub, IAdminClient>>();

        if (logEvent.Level >= LogEventLevel.Information)
        {
            try
            {
                hubContext.Clients.All.ReceivedLog(
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
            { }
        }
    }
}
