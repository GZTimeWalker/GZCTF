using GZCTF.Extensions;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GZCTF.Controllers;

/// <summary>
/// 全局信息接口
/// </summary>
[Route("api")]
[ApiController]
public class InfoController(ICaptchaExtension captcha,
    IPostRepository postRepository,
    IOptionsSnapshot<GlobalConfig> globalConfig,
    IOptionsSnapshot<AccountPolicy> accountPolicy) : ControllerBase
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
    public async Task<IActionResult> GetLatestPosts(CancellationToken token)
        => Ok((await postRepository.GetPosts(token)).Take(20).Select(PostInfoModel.FromPost));

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
    /// <response code="404">文章未找到</response>
    [HttpGet("Posts/{id}")]
    [ProducesResponseType(typeof(PostDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPost(string id, CancellationToken token)
    {
        var post = await postRepository.GetPostByIdFromCache(id, token);

        if (post is null)
            return NotFound(new RequestResponse("文章不存在", StatusCodes.Status404NotFound));

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
    /// 获取 Captcha 配置
    /// </summary>
    /// <remarks>
    /// 获取 Captcha 配置
    /// </remarks>
    /// <response code="200">成功获取 Captcha 配置</response>
    [HttpGet("Captcha")]
    [ProducesResponseType(typeof(ClientCaptchaInfoModel), StatusCodes.Status200OK)]
    public IActionResult GetClientCaptchaInfo()
        => Ok(accountPolicy.Value.UseCaptcha ? captcha.ClientInfo() : new ClientCaptchaInfoModel());
}
