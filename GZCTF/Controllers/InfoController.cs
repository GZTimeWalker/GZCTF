using CTFServer.Extensions;
using CTFServer.Models.Internal;
using CTFServer.Repositories.Interface;
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
    private readonly IOptions<AccountPolicy> accountPolicy;
    private readonly IOptions<GlobalConfig> globalConfig;
    private readonly INoticeRepository noticeRepository;
    private readonly IRecaptchaExtension recaptchaExtension;

    public InfoController(INoticeRepository _noticeRepository,
        IRecaptchaExtension _recaptchaExtension,
        IOptions<GlobalConfig> _globalConfig,
        IOptions<AccountPolicy> _accountPolicy)
    {
        globalConfig = _globalConfig;
        accountPolicy = _accountPolicy;
        noticeRepository = _noticeRepository;
        recaptchaExtension = _recaptchaExtension;
    }

    /// <summary>
    /// 获取最新公告
    /// </summary>
    /// <remarks>
    /// 获取最新公告
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">成功获取公告</response>
    [HttpGet("Notices")]
    [ProducesResponseType(typeof(Notice[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotices(CancellationToken token)
        => Ok(await noticeRepository.GetLatestNotices(token));

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