using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace GZCTF.Middlewares;

/// <summary>
/// Authorization filter for privilege
/// </summary>
/// <param name="privilege"> The privilege required </param>
/// <param name="allowToken"> Whether to allow token authentication </param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePrivilegeAttribute(Role privilege, bool allowToken = false) : Attribute, IAsyncAuthorizationFilter
{
    internal record BasicUserInfo(Guid Id, Role Role, string? UserName, DateTimeOffset? LastVisitedUtc);

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var logger =
            context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePrivilegeAttribute>>();

        if (allowToken && await ContextHelper.HasValidToken(context.HttpContext))
            return;

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var localizer =
            context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<Program>>();
        var diagnosticContext =
            context.HttpContext.RequestServices.GetRequiredService<IDiagnosticContext>();

        var id = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        BasicUserInfo? user = null;

        if (id is not null && context.HttpContext.User.Identity?.IsAuthenticated is true &&
            Guid.TryParse(id, out var guid))
            user = await dbContext.Users.AsNoTracking().Where(u => u.Id == guid).Select(u =>
                new BasicUserInfo(u.Id, u.Role, u.UserName, u.LastVisitedUtc)).FirstOrDefaultAsync();

        if (user is null)
        {
            context.Result = RequestResponse.Result(localizer[nameof(Resources.Program.Auth_LoginRequired)],
                StatusCodes.Status401Unauthorized);
            return;
        }

        diagnosticContext.Set("UserId", user.Id);
        diagnosticContext.Set("UserName", user.UserName ?? "Anonymous");

        var remoteIpAddress = context.HttpContext.Connection.RemoteIpAddress;
        if (remoteIpAddress != null)
        {
            diagnosticContext.Set("IP", remoteIpAddress);
            if (DateTimeOffset.UtcNow - user.LastVisitedUtc > TimeSpan.FromSeconds(5))
            {
                await dbContext.Users.Where(u => u.Id == user.Id)
                    .ExecuteUpdateAsync(s =>
                    {
                        s.SetProperty(u => u.LastVisitedUtc, _ => DateTimeOffset.UtcNow);
                        s.SetProperty(u => u.IP, _ => remoteIpAddress);
                    });
                await dbContext.SaveChangesAsync();
            }
        }

        if (user.Role >= privilege)
            return;

        if (privilege > Role.User)
            logger.Log(StaticLocalizer[
                    nameof(Resources.Program.Auth_PathAccessForbidden), context.HttpContext.Request.Path],
                user.UserName ?? "Anonymous", remoteIpAddress, TaskStatus.Denied);

        context.Result = RequestResponse.Result(localizer[nameof(Resources.Program.Auth_AccessForbidden)],
            StatusCodes.Status403Forbidden);
    }
}

/// <summary>
/// User required
/// </summary>
public class RequireUserAttribute() : RequirePrivilegeAttribute(Role.User);

/// <summary>
/// Monitor role required
/// </summary>
public class RequireMonitorAttribute() : RequirePrivilegeAttribute(Role.Monitor);

/// <summary>
/// Admin privilege required
/// </summary>
public class RequireAdminAttribute() : RequirePrivilegeAttribute(Role.Admin);

/// <summary>
/// Admin privilege required, but allow token authentication
/// </summary>
public class RequireAdminOrTokenAttribute() : RequirePrivilegeAttribute(Role.Admin, true);
