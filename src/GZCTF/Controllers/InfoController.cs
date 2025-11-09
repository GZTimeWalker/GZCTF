using System.Security.Cryptography;
using GZCTF.Extensions;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Controllers;

/// <summary>
/// Global Information APIs
/// </summary>
[Route("api")]
[ApiController]
public class InfoController(
    CacheHelper cacheHelper,
    IDistributedCache cache,
    ICaptchaExtension captcha,
    IPostRepository postRepository,
    IServiceProvider serviceProvider,
    ILogger<InfoController> logger,
    IOptionsSnapshot<CaptchaConfig> captchaConfig,
    IOptionsSnapshot<AccountPolicy> accountPolicy,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    static readonly DistributedCacheEntryOptions PowChallengeCacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(5)
    };

    /// <summary>
    /// Get the latest posts
    /// </summary>
    /// <remarks>
    /// Get the latest posts
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved posts</response>
    [HttpGet("Posts/Latest")]
    [ProducesResponseType(typeof(PostInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestPosts(CancellationToken token)
    {
        var posts = await postRepository.GetPosts(token);
        (Post[] data, DateTimeOffset lastModified) = posts;
        var eTag = $"\"latest-{lastModified.ToUnixTimeSeconds():X}\"";
        if (ContextHelper.IsNotModified(Request, Response, eTag, lastModified))
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(data.Take(20).Select(PostInfoModel.FromPost));
    }

    /// <summary>
    /// Get all posts
    /// </summary>
    /// <remarks>
    /// Get all posts
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved posts</response>
    [HttpGet("Posts")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Query))]
    [ProducesResponseType(typeof(PostInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPosts(CancellationToken token)
    {
        var posts = await postRepository.GetPosts(token);
        (Post[] data, DateTimeOffset lastModified) = posts;
        var eTag = $"\"all-{lastModified.ToUnixTimeSeconds():X}\"";
        if (ContextHelper.IsNotModified(Request, Response, eTag, lastModified))
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(data.Select(PostInfoModel.FromPost));
    }

    /// <summary>
    /// Get post details
    /// </summary>
    /// <remarks>
    /// Get post details
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved post details</response>
    /// <response code="404">Post not found</response>
    [HttpGet("Posts/{id}")]
    [ProducesResponseType(typeof(PostDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPost(string id, CancellationToken token)
    {
        var post = await postRepository.GetPostByIdFromCache(id, token);

        if (post is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Post_NotFound)],
                StatusCodes.Status404NotFound));

        var lastModified = post.UpdateTimeUtc;
        var eTag = $"\"{post.Id}-{lastModified.ToUnixTimeSeconds():X}\"";
        if (ContextHelper.IsNotModified(Request, Response, eTag, lastModified))
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(PostDetailModel.FromPost(post));
    }

    /// <summary>
    /// Get client configuration
    /// </summary>
    /// <remarks>
    /// Get client configuration
    /// </remarks>
    /// <response code="200">Successfully retrieved configuration</response>
    [HttpGet("Config")]
    [ProducesResponseType(typeof(ClientConfig), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClientConfig(CancellationToken token = default)
    {
        var data = await cacheHelper.GetOrCreateAsync(logger, CacheKey.ClientConfig,
            entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(7);
                return Task.FromResult(ClientConfig.FromServiceProvider(serviceProvider));
            }, token: token);

        var lastModified = data.UpdateTimeUtc;
        var eTag = $"\"cfg-{lastModified.ToUnixTimeSeconds():X}\"";
        if (ContextHelper.IsNotModified(Request, Response, eTag, lastModified))
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(data);
    }

    /// <summary>
    /// Get Captcha configuration
    /// </summary>
    /// <remarks>
    /// Get Captcha configuration
    /// </remarks>
    /// <response code="200">Successfully retrieved Captcha configuration</response>
    [HttpGet("Captcha")]
    [ProducesResponseType(typeof(ClientCaptchaInfoModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClientCaptchaInfo(CancellationToken token = default)
    {
        var data = await cacheHelper.GetOrCreateAsync(logger, CacheKey.CaptchaConfig,
            entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(7);
                return Task.FromResult(accountPolicy.Value.UseCaptcha
                    ? captcha.ClientInfo()
                    : new ClientCaptchaInfoModel());
            }, token: token);

        var lastModified = data.UpdateTimeUtc;
        var eTag = $"\"cap-{lastModified.ToUnixTimeSeconds():X}\"";
        if (ContextHelper.IsNotModified(Request, Response, eTag, lastModified))
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(data);
    }

    /// <summary>
    /// Create Pow Captcha
    /// </summary>
    /// <remarks>
    /// Create Pow Captcha, valid for 5 minutes
    /// </remarks>
    /// <response code="200">Successfully retrieved Pow Challenge</response>
    /// <response code="404">Pow is disabled</response>
    [HttpGet("Captcha/PowChallenge")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.PowChallenge))]
    [ProducesResponseType(typeof(HashPowChallenge), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PowChallenge(CancellationToken token = default)
    {
        if (captchaConfig.Value.Provider != CaptchaProvider.HashPow)
            return NotFound();

        var challenge = RandomNumberGenerator.GetBytes(8);
        var id = RandomNumberGenerator.GetHexString(12, true);
        await cache.SetAsync(CacheKey.HashPow(id), challenge, PowChallengeCacheOptions, token);

        return Ok(new HashPowChallenge
        {
            Id = id,
            Challenge = Convert.ToHexStringLower(challenge),
            Difficulty = captchaConfig.Value.HashPow.Difficulty
        });
    }
}
