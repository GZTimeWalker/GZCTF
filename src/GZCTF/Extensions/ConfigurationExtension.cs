using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using FluentStorage;
using FluentStorage.Blobs;
using GZCTF.Models.Internal;
using GZCTF.Providers;
using GZCTF.Services.Cache;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace GZCTF.Extensions;

public static class ConfigurationExtension
{
    static readonly DistributedCacheEntryOptions FaviconOptions = new() { SlidingExpiration = TimeSpan.FromDays(7) };

    const string CspTemplate = "default-src 'strict-dynamic' 'nonce-{0}' 'unsafe-inline' http: https:; " +
                               "style-src 'self' 'unsafe-inline'; img-src * 'self' data: blob:; " +
                               "font-src * 'self' data:; object-src 'none'; frame-src * https:; " +
                               "connect-src 'self'; base-uri 'none';";

    static readonly HashSet<string> SupportedCultures = Program.SupportedCultures
        .Select(c => c.ToLower()).ToHashSet();

    static readonly HashSet<string> ShortSupportedCultures = SupportedCultures
        .Select(c => c.Split('-')[0]).ToHashSet();

    const StringSplitOptions DefaultSplitOptions =
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

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

#pragma warning disable RDG002
        // Unable to resolve endpoint handler: https://github.com/dotnet/core/issues/8288
        app.MapFallback(IndexHandler(template));
#pragma warning restore RDG002
    }

    static string GetETag(string hash) => $"\"favicon-{hash[..8]}\"";

    static async Task<IResult> FaviconHandler(
        HttpContext context,
        IBlobStorage storage,
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

        var path = StoragePath.Combine(PathHelper.Uploads, hash[..2], hash[2..4], hash);
        if (!await storage.ExistsAsync(path, token))
            goto FallbackToDefaultIcon;

        await cache.SetStringAsync(CacheKey.Favicon, hash, FaviconOptions, token);
        Stream stream = await storage.OpenReadAsync(path, token);

        return Results.File(
            stream,
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

    static string ExtractLanguage(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage) || acceptLanguage.Length > 100)
            return "en-us";

        foreach (var language in acceptLanguage.Split(',', DefaultSplitOptions))
        {
            var culture = language.Split(';', DefaultSplitOptions).First().ToLower();
            if (SupportedCultures.Contains(culture))
                return culture;

            if (culture is "*")
                return "en-us";

            var shortCulture = culture.Split('-', DefaultSplitOptions).First();
            if (ShortSupportedCultures.Contains(shortCulture))
                return shortCulture;
        }

        return "en-us";
    }

    static IndexHandlerDelegate IndexHandler(string template) => async (
        HttpContext context,
        IDistributedCache cache,
        IOptionsSnapshot<GlobalConfig> globalConfig,
        CancellationToken token = default) =>
    {
        var content = await cache.GetStringAsync(CacheKey.Index, token);

        if (content is null)
        {
            GlobalConfig config = globalConfig.Value;
            var title = HtmlEncoder.Default.Encode(config.Platform);
            var description = HtmlEncoder.Default.Encode(config.Description ?? GlobalConfig.DefaultDescription);
            content = template.Replace("%title%", title).Replace("%description%", description);

            await cache.SetStringAsync(CacheKey.Index, content, token);
        }

        var builder = new StringBuilder(content);

        var acceptLanguage = context.Request.Headers.AcceptLanguage;
        builder.Replace("%lang%", ExtractLanguage(acceptLanguage));

        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12));
        context.Response.Headers.ContentSecurityPolicy = string.Format(CspTemplate, nonce);
        builder.Replace("%nonce%", nonce);

        return Results.Text(builder.ToString(), MediaTypeNames.Text.Html);
    };

    delegate Task<IResult> IndexHandlerDelegate(
        HttpContext context,
        IDistributedCache cache,
        IOptionsSnapshot<GlobalConfig> globalConfig,
        CancellationToken token = default);
}
