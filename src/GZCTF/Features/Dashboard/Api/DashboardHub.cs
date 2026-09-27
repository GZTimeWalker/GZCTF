using GZCTF.Features.Dashboard.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GZCTF.Features.Dashboard.Api;

[AllowAnonymous]
public sealed class DashboardHub(DashboardTokenService tokens) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext();
        var dashboardId = http?.Request.Query["id"].FirstOrDefault();
        var rawToken = http?.Request.Query["token"].FirstOrDefault();
        if (!Guid.TryParse(dashboardId, out var id) ||
            !await tokens.IsValidAsync(id, rawToken, http?.Connection.RemoteIpAddress?.ToString(), Context.ConnectionAborted))
        {
            Context.Abort();
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, DashboardHubGroup(id), Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public static string DashboardHubGroup(Guid dashboardId) => $"dashboard:{dashboardId:N}";
}

