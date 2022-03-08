using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using CTFServer.Middlewares;
using CTFServer.Models;
using CTFServer.Models.Request;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using NLog;
using System.Net.Mime;

namespace CTFServer.Controllers;

/// <summary>
/// 管理员数据交互接口
/// </summary>
[RequireAdmin]
[ApiController]
[Route("api/[controller]/[action]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class AdminController : ControllerBase
{
    private static readonly Logger logger = LogManager.GetLogger("AdminController");
    private readonly ILogRepository logRepository;
    private readonly IRankRepository rankRepository;
    private readonly IMemoryCache cache;
    private readonly IAnnouncementRepository announcementRepository;

    public AdminController(ILogRepository _logRepository,
        IMemoryCache memoryCache,
        IAnnouncementRepository _announcementRepository,
        IRankRepository _rankRepository)
    {
        cache = memoryCache;
        announcementRepository = _announcementRepository;
        logRepository = _logRepository;
        rankRepository = _rankRepository;
    }

    /// <summary>
    /// 系统日志获取接口
    /// </summary>
    /// <remarks>
    /// 使用此接口获取系统日志，需要Admin权限
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token">操作取消token</param>
    /// <response code="200">成功获取日志</response>
    /// <response code="400">校验失败</response>
    /// <response code="401">无权访问</response>
    [HttpGet]
    [ProducesResponseType(typeof(IList<LogMessageModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Logs([FromQuery] LogRequestModel model, CancellationToken token)
    {
        return Ok(await logRepository.GetLogs(model.Skip, model.Count, model.Level, token));
    }

    /// <summary>
    /// 检查用户提交并核对排名分数
    /// </summary>
    /// <remarks>
    /// 使用此接口检查用户提交并核对排名分数，需要Admin权限
    /// </remarks>
    /// <param name="token">操作取消token</param>
    /// <response code="200">成功完成操作</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckRankScore(CancellationToken token)
    {
        await rankRepository.CheckRankScore(token);
        cache.Remove(CacheKey.ScoreBoard);
        return Ok();
    }

    /// <summary>
    /// 添加或更新公告
    /// </summary>
    /// <remarks>
    /// 使用此接口添加或更新公告，需要Admin权限
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token">操作取消token</param>
    /// <param name="Id">公告Id</param>
    /// <response code="404">公告未找到</response>
    /// <response code="400">公告无效</response>
    /// <response code="200">成功完成操作</response>
    [HttpPost("{Id:int?}")]
    [ProducesResponseType(typeof(Announcement), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Notice(int? Id, [FromBody] UpdateAnnouncementModel model, CancellationToken token)
    {
        Announcement? announcement;

        if (Id is null)
        {
            if (model.Title is null || model.Content is null)
                return BadRequest(new RequestResponse("公告无效"));

            announcement = new()
            {
                Title = model.Title,
                Content = model.Content,
                IsPinned = model.IsPinned ?? false,
                PublishTimeUTC = DateTimeOffset.UtcNow
            };
        }
        else
        {
            announcement = await announcementRepository.GetAnnouncementById((int)Id, token);

            if (announcement is null)
                return NotFound(new RequestResponse("公告未找到", 404));

            announcement.Title = model.Title ?? announcement.Title;
            announcement.Content = model.Content ?? announcement.Content;
            announcement.IsPinned = model.IsPinned ?? announcement.IsPinned;
            announcement.PublishTimeUTC = DateTimeOffset.UtcNow;
        }

        announcement = await announcementRepository.AddOrUpdateAnnouncement(announcement, token);

        LogHelper.SystemLog(logger, $"成功更新公告#{announcement.Id}");

        cache.Remove(CacheKey.Announcements);

        return Ok(announcement);
    }
}