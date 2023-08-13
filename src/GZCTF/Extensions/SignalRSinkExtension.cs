using GZCTF.Hubs;
using GZCTF.Hubs.Client;
using GZCTF.Models.Request.Admin;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace GZCTF.Extensions;

public static class SignalRSinkExtension
{
    public static LoggerConfiguration SignalR(this LoggerSinkConfiguration loggerConfiguration, IServiceProvider serviceProvider)
        => loggerConfiguration.Sink(new SignalRSink(serviceProvider));
}

public class SignalRSink : ILogEventSink
{
    private readonly IServiceProvider _serviceProvider;
    private IHubContext<AdminHub, IAdminClient>? _hubContext;

    public SignalRSink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        _hubContext ??= _serviceProvider.GetRequiredService<IHubContext<AdminHub, IAdminClient>>();

        if (logEvent.Level >= LogEventLevel.Information)
        {
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
            { }
        }
    }
}
