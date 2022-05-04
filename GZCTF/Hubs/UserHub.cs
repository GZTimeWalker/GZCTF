using CTFServer.Hubs.Clients;
using CTFServer.Utils;
using Microsoft.AspNetCore.SignalR;

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