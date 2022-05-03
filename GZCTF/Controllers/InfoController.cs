using CTFServer.Repositories.Interface;
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
    public InfoController(INoticeRepository _noticeRepository,
        ILogger<InfoController> _logger)
    {
        logger = _logger;
        noticeRepository = _noticeRepository;
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
}
