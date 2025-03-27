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
    IDistributedCache cache,
    ICaptchaExtension captcha,
    IPostRepository postRepository,
    ILogger<InfoController> logger,
    IOptionsSnapshot<CaptchaConfig> captchaConfig,
    IOptionsSnapshot<GlobalConfig> globalConfig,
    IOptionsSnapshot<ContainerPolicy> containerPolicy,
    IOptionsSnapshot<ContainerProvider> containerProvider,
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
    [ResponseCache(Duration = 300)]
    [ProducesResponseType(typeof(PostInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestPosts(CancellationToken token) =>
        Ok((await postRepository.GetPosts(token)).Take(20).Select(PostInfoModel.FromPost));

    /// <summary>
    /// Get all posts
    /// </summary>
    /// <remarks>
    /// Get all posts
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved posts</response>
    [HttpGet("Posts")]
    [ResponseCache(Duration = 300)]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Query))]
    [ProducesResponseType(typeof(PostInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPosts(CancellationToken token) =>
        Ok((await postRepository.GetPosts(token)).Select(PostInfoModel.FromPost));

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
    [ResponseCache(Duration = 300)]
    [ProducesResponseType(typeof(PostDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPost(string id, CancellationToken token)
    {
        var post = await postRepository.GetPostByIdFromCache(id, token);

        if (post is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Post_NotFound)],
                StatusCodes.Status404NotFound));

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
    [ResponseCache(Duration = 300)]
    [ProducesResponseType(typeof(ClientConfig), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClientConfig(CancellationToken token = default)
    {
        var data = await cache.GetOrCreateAsync(logger, CacheKey.ClientConfig,
            entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(7);
                return Task.FromResult(ClientConfig.FromConfigs(globalConfig.Value, containerPolicy.Value,
                    containerProvider.Value));
            }, token);

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
    [ResponseCache(Duration = 300)]
    [ProducesResponseType(typeof(ClientCaptchaInfoModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClientCaptchaInfo(CancellationToken token = default)
    {
        var data = await cache.GetOrCreateAsync(logger, CacheKey.CaptchaConfig,
            entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(7);
                return Task.FromResult(accountPolicy.Value.UseCaptcha
                    ? captcha.ClientInfo()
                    : new ClientCaptchaInfoModel());
            }, token);

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
