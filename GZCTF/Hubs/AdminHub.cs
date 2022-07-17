using CTFServer.Hubs.Client;
using CTFServer.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace CTFServer.Hubs;

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