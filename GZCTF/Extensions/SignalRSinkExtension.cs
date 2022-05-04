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
            hubContext.Clients.All.ReceivedLog(
                new LogMessageModel
                {
                    Time = logEvent.Timestamp,
                    Level = logEvent.Level.ToString(),
                    UserName = logEvent.Properties.GetValueOrDefault("UserName")?.ToString(),
                    IP = logEvent.Properties.GetValueOrDefault("IP")?.ToString(),
                    Msg = logEvent.RenderMessage(),
                    Status = logEvent.Properties.GetValueOrDefault("Status")?.ToString()
                }).Wait();
        }
    }
}