using System.Security.Claims;
using GZCTF.Services.Token;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace GZCTF.Utils;

public static class ContextHelper
{
    static readonly CacheControlHeaderValue NoCacheHeader = new() { NoCache = true, MustRevalidate = true };

    static readonly CacheControlHeaderValue PrivateNoCacheHeader =
        new() { NoCache = true, Private = true, MustRevalidate = true };

    public static EntityTagHeaderValue SetCacheHeaders(HttpResponse response, StringSegment eTag,
        DateTimeOffset lastModified, bool isPrivate = false, bool isWeak = true)
    {
        var headers = response.GetTypedHeaders();
        headers.ETag = new EntityTagHeaderValue(eTag, isWeak);
        headers.LastModified = lastModified;
        headers.CacheControl = isPrivate ? PrivateNoCacheHeader : NoCacheHeader;
        return headers.ETag;
    }

    public static bool IsNotModified(HttpRequest request, HttpResponse response, StringSegment eTag,
        DateTimeOffset lastModified, bool isPrivate = false, bool isWeak = true)
    {
        var eTagValue = SetCacheHeaders(response, eTag, lastModified, isPrivate, isWeak);
        var headers = request.GetTypedHeaders();

        var ifNoneMatch = headers.IfNoneMatch;
        if (ifNoneMatch.Count > 0)
        {
            return ifNoneMatch.Any(tag =>
                StringSegment.Equals(tag.Tag, "*", StringComparison.Ordinal) ||
                tag.Compare(eTagValue, !isWeak));
        }

        if (headers.IfModifiedSince is { } ifModifiedSince)
        {
            return lastModified <= ifModifiedSince;
        }

        // No conditional headers, so the resource is considered modified.
        return false;
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
