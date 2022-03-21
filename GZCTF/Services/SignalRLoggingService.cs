using Microsoft.AspNetCore.SignalR;
using CTFServer.Extensions;
using CTFServer.Hubs;
using CTFServer.Hubs.Client;
using CTFServer.Models.Request.Admin;
using NLog;

namespace CTFServer.Services;

public class SignalRLoggingService : IDisposable
{
    private bool disposed = false;
    private IHubContext<LoggingHub, ILoggingClient> Hub { get; set; }
    public SignalRTarget? Target { get; set; }

    public SignalRLoggingService(IHubContext<LoggingHub, ILoggingClient> _Hub)
    {
        Hub = _Hub;
        Target = SignalRTarget.Instance;
        if (Target is not null)
            Target.LogEventHandler += OnLog;
    }

    ~SignalRLoggingService()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            if (Target is not null)
                Target.LogEventHandler -= OnLog;
            GC.SuppressFinalize(this);
        }
    }

    public async void OnLog(LogEventInfo logInfo)
    {
        if(logInfo.Level >= NLog.LogLevel.Error)
        {
            string status = logInfo.Properties.ContainsKey("status") ? 
                (string)logInfo.Properties["status"] : "Fail";

            await Hub.Clients.All.ReceivedLog(
                new LogMessageModel
                {
                    Time = logInfo.TimeStamp,
                    UserName = "System",
                    Level = logInfo.Level.ToString(),
                    IP = "-",
                    Msg = logInfo.Message,
                    Status = status
                });
        }
        else if(logInfo.Level >= NLog.LogLevel.Info)
        {
            await Hub.Clients.All.ReceivedLog(
                new LogMessageModel
                {
                    Time = logInfo.TimeStamp,
                    Level = logInfo.Level.ToString(),
                    UserName = (string)logInfo.Properties["uname"],
                    IP = (string)logInfo.Properties["ip"],
                    Msg = logInfo.Message,
                    Status = (string)logInfo.Properties["status"]
                });
        }
    }
}
