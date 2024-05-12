using GZCTF.Models.Internal;
using GZCTF.Providers;
using GZCTF.Services.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace GZCTF.Extensions;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddEntityConfiguration(this IConfigurationBuilder builder,
        Action<DbContextOptionsBuilder> optionsAction) =>
        builder.Add(new EntityConfigurationSource(optionsAction));

    public static void UseCustomFavicon(this WebApplication app) =>
        app.MapGet("/favicon.webp", FaviconHandler);

    static async void FaviconHandler(HttpContext context, IDistributedCache cache,
        IOptionsSnapshot<GlobalConfig> globalConfig, CancellationToken token = default)
    {
        context.Response.ContentType = "image/webp";
        context.Response.Headers.CacheControl = $"public, max-age={60 * 60 * 24 * 7}";

        var value = await cache.GetAsync(CacheKey.Favicon, token);
        if (value is not null)
        {
            await context.Response.BodyWriter.WriteAsync(value, token);
            return;
        }

        var hash = globalConfig.Value.FaviconHash;
        byte[] bytes;
        if (hash is null || !Codec.FileHashRegex().IsMatch(hash))
        {
            bytes = await File.ReadAllBytesAsync("wwwroot/favicon.webp", token);
        }
        else
        {
            var path = Path.GetFullPath(Path.Combine(FilePath.Uploads, hash[..2], hash[2..4], hash));
            if (File.Exists(path))
                bytes = await File.ReadAllBytesAsync(path, token);
            else
                bytes = await File.ReadAllBytesAsync("wwwroot/favicon.webp", token);
        }

        await cache.SetAsync(CacheKey.Favicon, bytes,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) }, token);
        await context.Response.BodyWriter.WriteAsync(bytes, token);
    }
}
