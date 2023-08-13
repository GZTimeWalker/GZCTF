using System.Security.Claims;
using GZCTF.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Middlewares;

/// <summary>
/// 需要权限访问
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePrivilegeAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>
    /// 所需权限
    /// </summary>
    private readonly Role _requiredPrivilege;

    public RequirePrivilegeAttribute(Role privilege)
        => _requiredPrivilege = privilege;

    public static IActionResult GetResult(string msg, int code)
        => new JsonResult(new RequestResponse(msg, code)) { StatusCode = code };

    public static IActionResult RequireLoginResult => GetResult("请先登录", StatusCodes.Status401Unauthorized);
    public static IActionResult ForbiddenResult => GetResult("无权访问", StatusCodes.Status403Forbidden);

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePrivilegeAttribute>>();
        var dbcontext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

        UserInfo? user = null;

        if (context.HttpContext.User.Identity?.IsAuthenticated is true)
            user = await dbcontext.Users.SingleOrDefaultAsync(u => u.Id ==
                context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (user is null)
        {
            context.Result = RequireLoginResult;
            return;
        }

        if (DateTimeOffset.UtcNow - user.LastVisitedUTC > TimeSpan.FromSeconds(5))
        {
            user.UpdateByHttpContext(context.HttpContext);
            await dbcontext.SaveChangesAsync(); // avoid to update ConcurrencyStamp
        }

        if (user.Role < _requiredPrivilege)
        {
            if (_requiredPrivilege > Role.User)
                logger.Log($"未经授权的访问：{context.HttpContext.Request.Path}", user, TaskStatus.Denied);

            context.Result = ForbiddenResult;
        }
    }
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
