using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Utils;

public static class HubHelper
{
    /// <summary>
    /// 当前请求是否具有权限
    /// </summary>
    /// <param name="context">当前请求</param>
    /// <param name="privilege">权限</param>
    /// <returns></returns>
    static async Task<bool> HasPrivilege(HttpContext context, Role privilege)
    {
        var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null || !Guid.TryParse(userId, out Guid id))
            return false;

        UserInfo? currentUser = await dbContext.Users.FirstOrDefaultAsync(i => i.Id == id);

        return currentUser is not null && currentUser.Role >= privilege;
    }

    /// <summary>
    /// 当前请求是否具有<see cref="Role.Admin" />权限
    /// </summary>
    /// <param name="context">当前请求</param>
    /// <returns></returns>
    public static Task<bool> HasAdmin(HttpContext context) => HasPrivilege(context, Role.Admin);

    /// <summary>
    /// 当前请求是否具有大于等于<see cref="Role.Monitor" />权限
    /// </summary>
    /// <param name="context">当前请求</param>
    /// <returns></returns>
    public static Task<bool> HasMonitor(HttpContext context) => HasPrivilege(context, Role.Monitor);

    /// <summary>
    /// 当前请求是否具有大于等于<see cref="Role.User" />权限
    /// </summary>
    /// <param name="context">当前请求</param>
    /// <returns></returns>
    public static Task<bool> HasUser(HttpContext context) => HasPrivilege(context, Role.User);
}
