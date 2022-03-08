using Microsoft.AspNetCore.SignalR;
using CTFServer.Hubs.Interface;
using CTFServer.Services;
using CTFServer.Utils;

namespace CTFServer.Hubs;

public class LoggingHub : Hub<ILoggingClient>
{
    private readonly SignalRLoggingService loggingEmitter;

    // This is the way to init LoggingEmitter.
    public LoggingHub(SignalRLoggingService emitter)
    {
        loggingEmitter = emitter;
    }

    public override async Task OnConnectedAsync()
    {
        if (!await HubHelper.HasAdmin(Context.GetHttpContext()!))
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }
}
