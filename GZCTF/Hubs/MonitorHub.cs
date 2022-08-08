using CTFServer.Hubs.Clients;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;

namespace CTFServer.Hubs;

public class MonitorHub : Hub<IMonitorClient>
{
    public override async Task OnConnectedAsync()
    {
        var context = Context.GetHttpContext();

        if (context is null
            || !await HubHelper.HasMonitor(context)
            || !context.Request.Query.TryGetValue("game", out StringValues gameId)
            || !int.TryParse(gameId, out int gameNumId))
        {
            Context.Abort();
            return;
        }

        var gameRepository = context.RequestServices.GetRequiredService<IGameRepository>();
        var game = await gameRepository.GetGameById(gameNumId);

        if (game is null)
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();

        await Groups.AddToGroupAsync(Context.ConnectionId, $"Game_{gameNumId}");
    }
}