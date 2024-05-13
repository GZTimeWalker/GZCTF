using System.Text.Encodings.Web;
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
    static readonly DistributedCacheEntryOptions FaviconOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
    };

    public static IConfigurationBuilder AddEntityConfiguration(this IConfigurationBuilder builder,
        Action<DbContextOptionsBuilder> optionsAction) =>
        builder.Add(new EntityConfigurationSource(optionsAction));

    public static void UseCustomFavicon(this WebApplication app) =>
        app.MapGet("/favicon.webp", FaviconHandler);

    public static async Task UseHomePageAsync(this WebApplication app, IWebHostEnvironment env)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapFallbackToFile("index.html");
        }
        else
        {
            await using var stream = app.Environment.WebRootFileProvider
                .GetFileInfo("index.html").CreateReadStream();
            using var streamReader = new StreamReader(stream);
            var template = await streamReader.ReadToEndAsync();
            app.MapFallback(HomePageHandler(template));
        }
    }

    static string GetETag(string hash) => $"\"favicon-{hash[..8]}\"";

    static async Task<IResult> FaviconHandler(HttpContext context, IDistributedCache cache,
        IOptionsSnapshot<GlobalConfig> globalConfig, CancellationToken token = default)
    {
        var hash = await cache.GetStringAsync(CacheKey.Favicon, token);
        if (hash is not null)
            goto WriteCustomIcon;

        hash = globalConfig.Value.FaviconHash;
        if (hash is null || !Codec.FileHashRegex().IsMatch(hash))
            goto FallbackToDefaultIcon;

        WriteCustomIcon:
        var eTag = GetETag(hash[..8]);
        if (context.Request.Headers.IfNoneMatch == eTag)
            return Results.StatusCode(StatusCodes.Status304NotModified);

        if (hash == Program.DefaultFaviconHash)
            goto SendDefaultIcon;

        var path = Path.GetFullPath(Path.Combine(FilePath.Uploads, hash[..2], hash[2..4], hash));
        if (!File.Exists(path))
            goto FallbackToDefaultIcon;

        await cache.SetStringAsync(CacheKey.Favicon, hash, FaviconOptions, token);

        return Results.File(
            path,
            "image/webp",
            "favicon.webp",
            entityTag: EntityTagHeaderValue.Parse(eTag));

    FallbackToDefaultIcon:
        eTag = GetETag(Program.DefaultFaviconHash[..8]);
        await cache.SetStringAsync(CacheKey.Favicon, Program.DefaultFaviconHash, FaviconOptions, token);

    SendDefaultIcon:
        return Results.File(
            Program.DefaultFavicon,
            "image/webp",
            "favicon.webp",
            entityTag: EntityTagHeaderValue.Parse(eTag));
    }

    static HomePageHandlerDelegate HomePageHandler(string template)
        => async (
            IDistributedCache cache,
            IOptionsSnapshot<GlobalConfig> globalConfig,
            CancellationToken token = default) =>
    {
        var content = await cache.GetStringAsync(CacheKey.HomePage, token);
        if (content is null)
        {
            // TODO: use real title and description
            // TODO: clear cache when the global config changes
            // TODO: UI and database for config
            content = template.Replace("%title%", HtmlEncoder.Default.Encode(globalConfig.Value.Title))
                .Replace("%description%", HtmlEncoder.Default.Encode(globalConfig.Value.Title));
            await cache.SetStringAsync(CacheKey.HomePage, content, token);
        }

        return Results.Text(content, "text/html");
    };

    delegate Task<IResult> HomePageHandlerDelegate(IDistributedCache cache, IOptionsSnapshot<GlobalConfig> globalConfig, CancellationToken token = default);
}
