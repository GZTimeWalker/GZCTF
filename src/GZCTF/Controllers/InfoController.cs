using GZCTF.Extensions;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Parameters;

namespace GZCTF.Controllers;

/// <summary>
/// 全局信息接口
/// </summary>
[Route("api")]
[ApiController]
public class InfoController(
    ICaptchaExtension captcha,
    IPostRepository postRepository,
    IOptionsSnapshot<GlobalConfig> globalConfig,
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
    /// 进行签名校验
    /// </summary>
    /// <remarks>
    /// 进行签名校验
    /// </remarks>
    /// <response code="200">签名有效</response>
    /// <response code="400">输入格式错误</response>
    /// <response code="401">签名无效</response>
    [HttpGet("Verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult VerifySignature(SignatureVerifyModel model)
    {
        // Token: <id>:<signature>
        // Data: $"GZCTF_TEAM_{team.Id}"

        var pk = Codec.Base64.DecodeToBytes(model.PublicKey);

        if (pk.Length != 32)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Signature_Invalid)]));

        var pos = model.TeamToken.IndexOf(':');

        if (pos == -1)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Signature_Invalid)]));

        var id = model.TeamToken[..pos];
        var sign = model.TeamToken[(pos + 1)..];

        Ed25519PublicKeyParameters publicKey = new(pk, 0);

        if (DigitalSignature.VerifySignature($"GZCTF_TEAM_{id}", sign, publicKey, SignAlgorithm.Ed25519))
            return Ok();

        return Unauthorized(new RequestResponse(localizer[nameof(Resources.Program.Signature_Invalid)]));
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
    public IActionResult GetClientCaptchaInfo() =>
        Ok(accountPolicy.Value.UseCaptcha ? captcha.ClientInfo() : new ClientCaptchaInfoModel());
}