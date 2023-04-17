using CTFServer.Extensions;
using CTFServer.Models.Internal;
using CTFServer.Models.Request.Info;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CTFServer.Controllers;

/// <summary>
/// 全局信息接口
/// </summary>
[Route("api")]
[ApiController]
public class InfoController : ControllerBase
{
    private readonly IOptionsSnapshot<AccountPolicy> accountPolicy;
    private readonly IOptionsSnapshot<GlobalConfig> globalConfig;
    private readonly IPostRepository postRepository;
    private readonly IRecaptchaExtension recaptchaExtension;

    public InfoController(IPostRepository _postRepository,
        IRecaptchaExtension _recaptchaExtension,
        IOptionsSnapshot<GlobalConfig> _globalConfig,
        IOptionsSnapshot<AccountPolicy> _accountPolicy)
    {
        globalConfig = _globalConfig;
        accountPolicy = _accountPolicy;
        postRepository = _postRepository;
        recaptchaExtension = _recaptchaExtension;
    }

    /// <summary>
    /// 获取最新文章
    /// </summary>
    /// <remarks>
    /// 获取最新文章
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">成功获取公告</response>
    [HttpGet("Posts/Latest")]
    [ProducesResponseType(typeof(PostInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestPosts(CancellationToken token)
        => Ok((await postRepository.GetPosts(token)).Take(20).Select(PostInfoModel.FromPost));

    /// <summary>
    /// 获取全部文章
    /// </summary>
    /// <remarks>
    /// 获取全部文章
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">成功获取公告</response>
    [HttpGet("Posts")]
    [ProducesResponseType(typeof(PostInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPosts(CancellationToken token)
        => Ok((await postRepository.GetPosts(token)).Select(PostInfoModel.FromPost));

    /// <summary>
    /// 获取文章详情
    /// </summary>
    /// <remarks>
    /// 获取文章详情
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取文章详情</response>
    [HttpGet("Posts/{id}")]
    [ProducesResponseType(typeof(PostDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPost(string id, CancellationToken token)
    {
        var post = await postRepository.GetPostByIdFromCache(id, token);

        if (post is null)
            return NotFound(new RequestResponse("文章未找到", 404));

        return Ok(PostDetailModel.FromPost(post));
    }

    /// <summary>
    /// 获取全局设置
    /// </summary>
    /// <remarks>
    /// 获取全局设置
    /// </remarks>
    /// <response code="200">成功获取配置信息</response>
    [HttpGet("Config")]
    [ProducesResponseType(typeof(GlobalConfig), StatusCodes.Status200OK)]
    public IActionResult GetGlobalConfig() => Ok(globalConfig.Value);

    /// <summary>
    /// 获取 Recaptcha SiteKey
    /// </summary>
    /// <remarks>
    /// 获取 Recaptcha SiteKey
    /// </remarks>
    /// <response code="200">成功获取 Recaptcha SiteKey</response>
    [HttpGet("SiteKey")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult GetRecaptchaSiteKey()
        => Ok(accountPolicy.Value.UseGoogleRecaptcha ? recaptchaExtension.SiteKey() : "NOTOKEN");
}
