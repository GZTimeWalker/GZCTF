using CTFServer.Hubs.Clients;
using CTFServer.Utils;
using Microsoft.AspNetCore.SignalR;

namespace CTFServer.Hubs;

public class MonitorHub : Hub<IMonitorClient>
{
    public override async Task OnConnectedAsync()
    {
        if (!await HubHelper.HasMonitor(Context.GetHttpContext()!))
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }
}