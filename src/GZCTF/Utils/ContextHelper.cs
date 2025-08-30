using System.Security.Claims;
using GZCTF.Services.Token;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace GZCTF.Utils;

public static class ContextHelper
{
    static readonly CacheControlHeaderValue NoCacheHeader = new() { NoCache = true, MustRevalidate = true };

    static readonly CacheControlHeaderValue PrivateNoCacheHeader =
        new() { NoCache = true, Private = true, MustRevalidate = true };

    public static bool IsModified(HttpRequest request, HttpResponse response, string eTag, DateTimeOffset lastModified,
        bool isPrivate = false)
    {
        response.Headers.ETag = eTag;
        response.Headers.LastModified = lastModified.ToString("R");
        response.GetTypedHeaders().CacheControl = isPrivate ? PrivateNoCacheHeader : NoCacheHeader;

        if (request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var inm))
        {
            var header = inm.ToString();
            if (!string.IsNullOrEmpty(header))
            {
                var tags = header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (tags.Any(t => string.Equals(t, eTag, StringComparison.Ordinal) || t == "*"))
                    return false;
            }
        }

        if (request.Headers.TryGetValue(HeaderNames.IfModifiedSince, out var ims) &&
            DateTimeOffset.TryParse(ims.ToString(), out var since))
        {
            if (lastModified <= since.ToUniversalTime())
                return false;
        }

        return true;
    }

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
