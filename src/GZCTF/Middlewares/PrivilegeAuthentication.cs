using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace GZCTF.Middlewares;

/// <summary>
/// Authorization filter for privilege
/// </summary>
/// <param name="privilege">
/// The privilege required
/// </param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePrivilegeAttribute(Role privilege) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePrivilegeAttribute>>();
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<Program>>();
        var diagnosticContext = context.HttpContext.RequestServices.GetRequiredService<IDiagnosticContext>();

        var id = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        UserInfo? user = null;

        if (id is not null && context.HttpContext.User.Identity?.IsAuthenticated is true &&
            Guid.TryParse(id, out Guid guid))
            user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == guid);

        if (user is null)
        {
            context.Result = GetResult(localizer[nameof(Resources.Program.Auth_LoginRequired)],
                StatusCodes.Status401Unauthorized);
            return;
        }

        diagnosticContext.Set("UserId", user.Id);
        diagnosticContext.Set("UserName", user.UserName);
        diagnosticContext.Set("IP", context.HttpContext.Connection.RemoteIpAddress);

        if (DateTimeOffset.UtcNow - user.LastVisitedUtc > TimeSpan.FromSeconds(5))
        {
            user.UpdateByHttpContext(context.HttpContext);
            await dbContext.SaveChangesAsync(); // avoid to update ConcurrencyStamp
        }

        if (user.Role < privilege)
        {
            if (privilege > Role.User)
                logger.Log(
                    Program.StaticLocalizer[nameof(Resources.Program.Auth_PathAccessForbidden),
                        context.HttpContext.Request.Path], user,
                    TaskStatus.Denied);

            context.Result = GetResult(localizer[nameof(Resources.Program.Auth_AccessForbidden)],
                StatusCodes.Status403Forbidden);
        }
    }

    public static IActionResult GetResult(string msg, int code) =>
        new JsonResult(new RequestResponse(msg, code)) { StatusCode = code };
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
