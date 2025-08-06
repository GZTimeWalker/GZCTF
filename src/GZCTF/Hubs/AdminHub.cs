using GZCTF.Hubs.Clients;
using Microsoft.AspNetCore.SignalR;

namespace GZCTF.Hubs;

public class AdminHub : Hub<IAdminClient>
{
    public override async Task OnConnectedAsync()
    {
        var context = Context.GetHttpContext();

        if (context is null || (!await ContextHelper.HasAdmin(context) && !await ContextHelper.HasValidToken(context)))
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }
}
