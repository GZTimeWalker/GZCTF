using CTFServer.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CTFServer.Middlewares;

/// <summary>
/// 需要权限访问
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePrivilegeAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>
    /// 所需权限
    /// </summary>
    private readonly Role RequiredPrivilege;

    public RequirePrivilegeAttribute(Role privilege)
        => RequiredPrivilege = privilege;

    public static IActionResult GetResult(string msg, int code)
        => new JsonResult(new RequestResponse(msg, code)) { StatusCode = code };

    public static IActionResult RequireLoginResult => GetResult("请先登录", StatusCodes.Status401Unauthorized);
    public static IActionResult ForbiddenResult => GetResult("无权访问", StatusCodes.Status403Forbidden);

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePrivilegeAttribute>>();
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<UserInfo>>();
        UserInfo? user = null;

        if (context.HttpContext.User.Identity?.IsAuthenticated is true)
            user = await userManager.GetUserAsync(context.HttpContext.User);

        if (user is null)
        {
            context.Result = RequireLoginResult;
            return;
        }

        user.UpdateByHttpContext(context.HttpContext);
        await userManager.UpdateAsync(user);

        if (user.Role < RequiredPrivilege)
        {
            if (RequiredPrivilege > Role.User)
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