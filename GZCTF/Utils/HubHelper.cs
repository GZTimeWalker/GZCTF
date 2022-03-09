using Microsoft.EntityFrameworkCore;
using CTFServer.Models;
using NLog;
using System.Security.Claims;

namespace CTFServer.Utils;

public class HubHelper
{
    /// <summary>
    /// 当前请求是否具有权限
    /// </summary>
    /// <param name="context">当前请求</param>
    /// <param name="privilege">权限</param>
    /// <returns></returns>
    public static async Task<bool> HasPrivilege(HttpContext context, Privilege privilege)
    {
        var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (dbContext is null || userId is null)
            return false;

        var currentUser = await dbContext.Users.FirstOrDefaultAsync(i => i.Id == userId);

        return currentUser is not null && currentUser.Privilege >= privilege;
    }

    /// <summary>
    /// 当前请求是否具有<see cref="Privilege.Admin"/>权限
    /// </summary>
    /// <param name="context">当前请求</param>
    /// <returns></returns>
    public static Task<bool> HasAdmin(HttpContext context)
        => HasPrivilege(context, Privilege.Admin);

    /// <summary>
    /// 当前请求是否具有大于等于<see cref="Privilege.Monitor"/>权限
    /// </summary>
    /// <param name="context">当前请求</param>
    /// <returns></returns>
    public static Task<bool> HasMonitor(HttpContext context)
        => HasPrivilege(context, Privilege.Monitor);

    /// <summary>
    /// 当前请求是否具有大于等于<see cref="Privilege.User"/>权限
    /// </summary>
    /// <param name="context">当前请求</param>
    /// <returns></returns>
    public static Task<bool> HasSignedIn(HttpContext context)
        => HasPrivilege(context, Privilege.User);
}
