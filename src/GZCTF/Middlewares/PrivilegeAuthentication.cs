using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Middlewares;

/// <summary>
/// 需要权限访问
/// </summary>
/// <param name="privilege">
/// 所需权限
/// </param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePrivilegeAttribute(Role privilege) : Attribute, IAsyncAuthorizationFilter
{
    public static IActionResult RequireLoginResult => GetResult("请先登录", StatusCodes.Status401Unauthorized);
    public static IActionResult ForbiddenResult => GetResult("无权访问", StatusCodes.Status403Forbidden);

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePrivilegeAttribute>>();
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var id = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        UserInfo? user = null;

        if (id is not null && context.HttpContext.User.Identity?.IsAuthenticated is true && Guid.TryParse(id, out var guid))
            user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == guid);

        if (user is null)
        {
            context.Result = RequireLoginResult;
            return;
        }

        if (DateTimeOffset.UtcNow - user.LastVisitedUtc > TimeSpan.FromSeconds(5))
        {
            user.UpdateByHttpContext(context.HttpContext);
            await dbContext.SaveChangesAsync(); // avoid to update ConcurrencyStamp
        }

        if (user.Role < privilege)
        {
            if (privilege > Role.User)
                logger.Log($"未经授权的访问：{context.HttpContext.Request.Path}", user, TaskStatus.Denied);

            context.Result = ForbiddenResult;
        }
    }

    public static IActionResult GetResult(string msg, int code) => new JsonResult(new RequestResponse(msg, code)) { StatusCode = code };
}

/// <summary>
/// 需要已登录用户权限
/// </summary>
public class RequireUserAttribute : RequirePrivilegeAttribute
{
    public RequireUserAttribute() : base(Role.User)
    {
    }
}

/// <summary>
/// 需要监控者权限
/// </summary>
public class RequireMonitorAttribute : RequirePrivilegeAttribute
{
    public RequireMonitorAttribute() : base(Role.Monitor)
    {
    }
}

/// <summary>
/// 需要Admin权限
/// </summary>
public class RequireAdminAttribute : RequirePrivilegeAttribute
{
    public RequireAdminAttribute() : base(Role.Admin)
    {
    }
}