using Microsoft.AspNetCore.SignalR;
using CTFServer.Hubs.Client;
using CTFServer.Utils;
using CTFServer.Hubs.Clients;

namespace CTFServer.Hubs;

public class UserHub : Hub<IUserClient>
{
    public override async Task OnConnectedAsync()
    {
        if (!await HubHelper.HasUser(Context.GetHttpContext()!))
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }
}
