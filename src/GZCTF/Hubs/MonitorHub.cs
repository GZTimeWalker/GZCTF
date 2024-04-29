using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;

namespace GZCTF.Hubs;

public class MonitorHub : Hub<IMonitorClient>
{
    public override async Task OnConnectedAsync()
    {
        HttpContext? context = Context.GetHttpContext();

        if (context is null
            || !await HubHelper.HasMonitor(context)
            || !context.Request.Query.TryGetValue("game", out StringValues gameId)
            || !int.TryParse(gameId, out var gId))
        {
            Context.Abort();
            return;
        }

        var gameRepository = context.RequestServices.GetRequiredService<IGameRepository>();
        Game? game = await gameRepository.GetGameById(gId);

        if (game is null)
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();

        await Groups.AddToGroupAsync(Context.ConnectionId, $"Game_{gId}");
    }
}
