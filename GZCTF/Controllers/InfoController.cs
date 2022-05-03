using CTFServer.Extensions;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using Microsoft.AspNetCore.Mvc;

namespace CTFServer.Controllers;

/// <summary>
/// 全局信息接口
/// </summary>
[Route("api")]
[ApiController]
public class InfoController : ControllerBase
{
    private readonly ILogger<InfoController> logger;
    private readonly INoticeRepository noticeRepository;
    private readonly IRecaptchaExtension recaptchaExtension;

    public InfoController(INoticeRepository _noticeRepository,
        IRecaptchaExtension _recaptchaExtension,
        ILogger<InfoController> _logger)
    {
        logger = _logger;
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
    /// 获取 Recaptcha SiteKey
    /// </summary>
    /// <remarks>
    /// 获取 Recaptcha SiteKey
    /// </remarks>
    /// <response code="200">成功获取 Recaptcha SiteKey</response>
    [HttpGet("SiteKey")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult GetRecaptchaSiteKey()
        => Ok(recaptchaExtension.SiteKey());    
}
