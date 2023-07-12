using GZCTF.Hubs.Client;
using GZCTF.Utils;
using Microsoft.AspNetCore.SignalR;

namespace GZCTF.Hubs;

public class AdminHub : Hub<IAdminClient>
{
    private readonly ILogger<AdminHub> logger;

    public AdminHub(ILogger<AdminHub> _logger)
    {
        logger = _logger;
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
