using GZCTF.Features.Dashboard.Api;
using GZCTF.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.Dashboard.Application;

public sealed record DashboardSolveDelta(string UserName, Guid? CohortId, DateOnly Date, int UniqueSolvedCount);

public sealed class DashboardDeltaPublisher(AppDbContext db, IHubContext<DashboardHub> hub)
{
    public async Task PublishFirstSolveAsync(Guid userId, CancellationToken token = default)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, token);
        if (user is null) return;
        var count = await db.ChallengeProgress.CountAsync(item => item.UserId == userId, token);
        var delta = new DashboardSolveDelta(user.UserName ?? string.Empty, user.CohortId, DateOnly.FromDateTime(DateTime.UtcNow), count);
        var dashboards = await db.Dashboards.AsNoTracking().Where(item => item.IsEnabled).Select(item => item.Id).ToArrayAsync(token);
        foreach (var dashboardId in dashboards)
            await hub.Clients.Group(DashboardHub.DashboardHubGroup(dashboardId)).SendAsync("solveDelta", delta, token);
    }
}
