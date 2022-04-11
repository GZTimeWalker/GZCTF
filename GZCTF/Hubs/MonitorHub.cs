using Microsoft.AspNetCore.SignalR;
using CTFServer.Hubs.Client;
using CTFServer.Utils;
using CTFServer.Hubs.Clients;

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
