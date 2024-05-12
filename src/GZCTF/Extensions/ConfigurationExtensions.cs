using GZCTF.Models.Internal;
using GZCTF.Providers;
using GZCTF.Services.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace GZCTF.Extensions;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddEntityConfiguration(this IConfigurationBuilder builder,
        Action<DbContextOptionsBuilder> optionsAction) =>
        builder.Add(new EntityConfigurationSource(optionsAction));

    public static void UseCustomFavicon(this WebApplication app) =>
        app.MapGet("/favicon.webp", FaviconHandler);

    static async Task<IResult> FaviconHandler(HttpContext context, IDistributedCache cache,
        IOptionsSnapshot<GlobalConfig> globalConfig, CancellationToken token = default)
    {
        var hash = await cache.GetStringAsync(CacheKey.Favicon, token);
        if (hash is not null)
        {
            goto WriteCustomIcon;
        }

        hash = globalConfig.Value.FaviconHash;
        if (hash is null || !Codec.FileHashRegex().IsMatch(hash))
        {
            goto FallbackToDefaultIcon;
        }
        else
        {
            goto WriteCustomIcon;
        }

    WriteCustomIcon:
        var eTag = $"\"{hash}\"";
        if (context.Request.Headers.IfNoneMatch == eTag)
        {
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        if (hash == Program.DefaultFaviconHash)
        {
            return Results.File(
                Program.DefaultFavicon,
                "image/webp",
                "favicon.webp",
                lastModified: DateTimeOffset.UtcNow,
                entityTag: EntityTagHeaderValue.Parse(eTag));
        }

        var path = Path.GetFullPath(Path.Combine(FilePath.Uploads, hash[..2], hash[2..4], hash));
        if (File.Exists(path))
        {
            await cache.SetStringAsync(CacheKey.Favicon, hash,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) }, token);

            return Results.File(
                path,
                "image/webp",
                "favicon.webp",
                lastModified: File.GetLastWriteTimeUtc(path),
                entityTag: EntityTagHeaderValue.Parse(eTag));
        }

    FallbackToDefaultIcon:
        eTag = $"\"{Program.DefaultFaviconHash}\"";
        await cache.SetStringAsync(CacheKey.Favicon, Program.DefaultFaviconHash,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) }, token);

        return Results.File(
            Program.DefaultFavicon,
            "image/webp",
            "favicon.webp",
            lastModified: DateTimeOffset.UtcNow,
            entityTag: EntityTagHeaderValue.Parse(eTag));
    }
}
