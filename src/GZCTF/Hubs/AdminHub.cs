using GZCTF.Hubs.Client;
using GZCTF.Utils;
using Microsoft.AspNetCore.SignalR;

namespace GZCTF.Hubs;

public class AdminHub : Hub<IAdminClient>
{
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
