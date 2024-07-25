using System.Net.Mime;
using System.Security.Cryptography;
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

    const string CspTemplate = "default-src 'strict-dynamic' 'nonce-{0}' 'unsafe-inline' http: https:; " +
                               "style-src 'self' 'unsafe-inline'; img-src * 'self' data: blob:; " +
                               "font-src * 'self' data:; object-src 'none'; frame-src * https:; " +
                               "connect-src 'self'; base-uri 'none';";

    public static void AddEntityConfiguration(this IConfigurationBuilder builder,
        Action<DbContextOptionsBuilder> optionsAction) =>
        builder.Add(new EntityConfigurationSource(optionsAction));

    public static void UseCustomFavicon(this WebApplication app) =>
        app.MapGet("/favicon.webp", FaviconHandler);

    public static async Task UseIndexAsync(this WebApplication app)
    {
        if (!File.Exists(Path.Join(app.Environment.WebRootPath, "index.html")))
        {
            app.MapFallback(context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = MediaTypeNames.Text.Html;
                return Task.CompletedTask;
            });
            return;
        }

        await using Stream stream = app.Environment.WebRootFileProvider
            .GetFileInfo("index.html").CreateReadStream();

        using var streamReader = new StreamReader(stream);
        var template = await streamReader.ReadToEndAsync();

        app.MapFallback(IndexHandler(template));
    }

    static string GetETag(string hash) => $"\"favicon-{hash[..8]}\"";

    static async Task<IResult> FaviconHandler(
        HttpContext context,
        IDistributedCache cache,
        IOptionsSnapshot<GlobalConfig> globalConfig,
        CancellationToken token = default)
    {
        var hash = await cache.GetStringAsync(CacheKey.Favicon, token);
        if (hash is null)
        {
            hash = globalConfig.Value.FaviconHash;
            if (hash is null || !Codec.FileHashRegex().IsMatch(hash))
                goto FallbackToDefaultIcon;
        }

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
            MediaTypeNames.Image.Webp,
            "favicon.webp",
            entityTag: EntityTagHeaderValue.Parse(eTag));

    FallbackToDefaultIcon:

        eTag = GetETag(Program.DefaultFaviconHash[..8]);
        await cache.SetStringAsync(CacheKey.Favicon, Program.DefaultFaviconHash, FaviconOptions, token);

    SendDefaultIcon:

        return Results.File(
            Program.DefaultFavicon,
            MediaTypeNames.Image.Webp,
            "favicon.webp",
            entityTag: EntityTagHeaderValue.Parse(eTag));
    }

    static HomePageHandlerDelegate IndexHandler(string template) => async (
        HttpContext context,
        IDistributedCache cache,
        IOptionsSnapshot<GlobalConfig> globalConfig,
        CancellationToken token = default) =>
    {
        var content = await cache.GetStringAsync(CacheKey.Index, token);

        if (content is not null)
            goto SendContent;

        GlobalConfig config = globalConfig.Value;
        var title = HtmlEncoder.Default.Encode(config.Platform);
        var description = HtmlEncoder.Default.Encode(config.Description ?? GlobalConfig.DefaultDescription);
        content = template.Replace("%title%", title).Replace("%description%", description);

        await cache.SetStringAsync(CacheKey.Index, content, token);

    SendContent:

        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12));

        context.Response.Headers.ContentSecurityPolicy = string.Format(CspTemplate, nonce);

        return Results.Text(content.Replace("%nonce%", nonce), MediaTypeNames.Text.Html);
    };

    delegate Task<IResult> HomePageHandlerDelegate(
        HttpContext context,
        IDistributedCache cache,
        IOptionsSnapshot<GlobalConfig> globalConfig,
        CancellationToken token = default);
}
