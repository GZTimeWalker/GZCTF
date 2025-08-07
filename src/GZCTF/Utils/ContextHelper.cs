using System.Security.Claims;
using GZCTF.Services.Token;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Utils;

public static class ContextHelper
{
    /// <summary>
    /// Checks if the current request has the specified privilege.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="privilege">The privilege to check against.</param>
    /// <returns></returns>
    static async Task<bool> HasPrivilege(HttpContext context, Role privilege)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null || !Guid.TryParse(userId, out var id))
            return false;

        var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
        return await dbContext.Users.AnyAsync(i => i.Id == id && i.Role >= privilege);
    }

    /// <summary>
    /// Checks if the current request has a valid token.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns></returns>
    public static async Task<bool> HasValidToken(HttpContext context)
    {
        var value = context.Request.Headers.Authorization.FirstOrDefault()?.Split(' ');

        if (value is not ["Bearer", { Length: > 0 } token])
            return false;

        var tokenService = context.RequestServices.GetRequiredService<ITokenService>();
        return await tokenService.ValidateToken(token);
    }

    /// <summary>
    /// Checks if the current request has <see cref="Role.Admin" /> privileges.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns></returns>
    public static Task<bool> HasAdmin(HttpContext context) => HasPrivilege(context, Role.Admin);

    /// <summary>
    /// Checks if the current request has <see cref="Role.Monitor" /> privileges.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns></returns>
    public static Task<bool> HasMonitor(HttpContext context) => HasPrivilege(context, Role.Monitor);

    /// <summary>
    /// Checks if the current request has <see cref="Role.User" /> privileges.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns></returns>
    public static Task<bool> HasUser(HttpContext context) => HasPrivilege(context, Role.User);
}
