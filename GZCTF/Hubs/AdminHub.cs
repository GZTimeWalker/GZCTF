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
        var context = Context.GetHttpContext()!;
        var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (dbContext is not null && userId is not null)
        {
            var currentUser = await dbContext.Users.FirstOrDefaultAsync(i => i.Id == userId);
            logger.Log($"AdminHub 获取新连接", currentUser!, TaskStatus.Pending, LogLevel.Information);
        }

        if (!await HubHelper.HasAdmin(Context.GetHttpContext()!))
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }
}