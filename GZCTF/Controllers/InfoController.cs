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
/// 数据相关接口
/// </summary>
[ApiController]
[RequireSignedIn]
[Route("api/[controller]/[action]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
public class InfoController : ControllerBase
{
    private static readonly Logger logger = LogManager.GetLogger("InfoController");
    private readonly IRankRepository rankRepository;
    private readonly ISubmissionRepository submissionRepository;
    private readonly IMemoryCache cache;
    private readonly INoticeRepository announcementRepository;

    public InfoController(
        IMemoryCache memoryCache,
        ISubmissionRepository _submissionRepository,
        INoticeRepository _announcementRepository,
        IRankRepository _rankRepository)
    {
        cache = memoryCache;
        rankRepository = _rankRepository;
        submissionRepository = _submissionRepository;
        announcementRepository = _announcementRepository;
    }

    /// <summary>
    /// 积分榜接口
    /// </summary>
    /// <remarks>
    /// 使用此接口获取积分榜，需要已登录权限
    /// </remarks>
    /// <param name="token">操作取消token</param>
    /// <response code="200">成功获取积分榜</response>
    /// <response code="401">无权访问</response>
    [HttpGet]
    [ProducesResponseType(typeof(ScoreBoardMessageModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> ScoreBoard(CancellationToken token)
    {
        if (cache.TryGetValue(CacheKey.ScoreBoard, out ScoreBoardMessageModel result))
            return Ok(result);

        result = new();
        result.TopDetail = new();
        result.Rank = await rankRepository.GetRank(token);

        if (result.Rank is null)
            return Ok(result);

        foreach (var r in result.Rank.Take(10))
        {
            if(r.User is not null && r.User.EmailConfirmed)
                result.TopDetail.Add(new ScoreBoardTimeLine()
                {
                    UserName = r.UserName,
                    TimeLine = submissionRepository.GetTimeLine(r.User, token)
                });
        }

        result.UpdateTime = DateTimeOffset.Now;

        LogHelper.SystemLog(logger, "重构缓存：ScoreBoard");

        cache.Set(CacheKey.ScoreBoard, result, TimeSpan.FromHours(10));

        return Ok(result);
    }

    /// <summary>
    /// 公告接口
    /// </summary>
    /// <remarks>
    /// 使用此接口获取公告，需要已登录权限
    /// </remarks>
    /// <param name="token">操作取消token</param>
    /// <response code="200">成功获取公告</response>
    /// <response code="401">无权访问</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<Notice>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Announcements(CancellationToken token)
    {
        if (cache.TryGetValue(CacheKey.Announcements, out List<Notice> result))
            return Ok(result);

        result = await announcementRepository.GetNotices(0, 3, token);

        cache.Set(CacheKey.Announcements, result, TimeSpan.FromHours(10));

        return Ok(result);
    }
}
