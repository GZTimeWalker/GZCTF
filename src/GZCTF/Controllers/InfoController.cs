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
/// 全局信息接口
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
    IOptionsSnapshot<AccountPolicy> accountPolicy,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    /// <summary>
    /// 获取最新文章
    /// </summary>
    /// <remarks>
    /// 获取最新文章
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">成功获取文章</response>
    [HttpGet("Posts/Latest")]
    [ProducesResponseType(typeof(PostInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestPosts(CancellationToken token) =>
        Ok((await postRepository.GetPosts(token)).Take(20).Select(PostInfoModel.FromPost));

    /// <summary>
    /// 获取全部文章
    /// </summary>
    /// <remarks>
    /// 获取全部文章
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">成功获取文章</response>
    [HttpGet("Posts")]
    [ProducesResponseType(typeof(PostInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPosts(CancellationToken token) =>
        Ok((await postRepository.GetPosts(token)).Select(PostInfoModel.FromPost));

    /// <summary>
    /// 获取文章详情
    /// </summary>
    /// <remarks>
    /// 获取文章详情
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取文章详情</response>
    /// <response code="404">文章未找到</response>
    [HttpGet("Posts/{id}")]
    [ProducesResponseType(typeof(PostDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPost(string id, CancellationToken token)
    {
        Post? post = await postRepository.GetPostByIdFromCache(id, token);

        if (post is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Post_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(PostDetailModel.FromPost(post));
    }

    /// <summary>
    /// 获取客户端设置
    /// </summary>
    /// <remarks>
    /// 获取客户端设置
    /// </remarks>
    /// <response code="200">成功获取配置信息</response>
    [HttpGet("Config")]
    [ProducesResponseType(typeof(ClientConfig), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClientConfig(CancellationToken token = default)
    {
        ClientConfig data = await cache.GetOrCreateAsync(logger, CacheKey.ClientConfig,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7);
                return Task.FromResult(ClientConfig.FromConfigs(globalConfig.Value, containerPolicy.Value));
            }, token);

        return Ok(data);
    }

    /// <summary>
    /// 获取 Captcha 配置
    /// </summary>
    /// <remarks>
    /// 获取 Captcha 配置
    /// </remarks>
    /// <response code="200">成功获取 Captcha 配置</response>
    [HttpGet("Captcha")]
    [ProducesResponseType(typeof(ClientCaptchaInfoModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClientCaptchaInfo(CancellationToken token = default)
    {
        ClientCaptchaInfoModel data = await cache.GetOrCreateAsync(logger, CacheKey.CaptchaConfig,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7);
                return Task.FromResult(accountPolicy.Value.UseCaptcha
                    ? captcha.ClientInfo()
                    : new ClientCaptchaInfoModel());
            }, token);

        return Ok(data);
    }

    /// <summary>
    /// 创建 Pow 验证码
    /// </summary>
    /// <remarks>
    /// 创建 Pow 验证码，有效期 5 分钟
    /// </remarks>
    /// <response code="200">成功获取 Pow Challenge</response>
    /// <response code="404">Pow 已禁用</response>
    [HttpGet("Captcha/PowChallenge")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.PowChallenge))]
    [ProducesResponseType(typeof(HashPowChallenge), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PowChallenge(CancellationToken token = default)
    {
        if (captchaConfig.Value.Provider != CaptchaProvider.HashPow)
            return NotFound();

        byte[] challenge = RandomNumberGenerator.GetBytes(8);
        string id = RandomNumberGenerator.GetHexString(12, true);
        await cache.SetAsync(CacheKey.HashPow(id), challenge, PowChallengeCacheOptions, token);

        return Ok(new HashPowChallenge
        {
            Id = id,
            Challenge = Convert.ToHexStringLower(challenge),
            Difficulty = captchaConfig.Value.HashPow.Difficulty
        });
    }

    static readonly DistributedCacheEntryOptions PowChallengeCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };
}
