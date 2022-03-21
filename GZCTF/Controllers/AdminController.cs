using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using CTFServer.Middlewares;
using CTFServer.Models;
using CTFServer.Models.Request.Account;
using CTFServer.Models.Request.Admin;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using NLog;
using System.Net.Mime;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Controllers;

/// <summary>
/// 管理员数据交互接口
/// </summary>
[RequireAdmin]
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class AdminController : ControllerBase
{
    private static readonly Logger logger = LogManager.GetLogger("AdminController");
    private readonly UserManager<UserInfo> userManager;
    private readonly ILogRepository logRepository;

    public AdminController(UserManager<UserInfo> _userManager, ILogRepository _logRepository)
    {
        userManager = _userManager;
        logRepository = _logRepository;
    }

    /// <summary>
    /// 获取全部用户
    /// </summary>
    /// <remarks>
    /// 使用此接口获取全部用户，需要Admin权限
    /// </remarks>
    /// <response code="200">用户列表</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpGet("Users")]
    [ProducesResponseType(typeof(List<ClientUserInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Users([FromQuery] int count = 100, [FromQuery] int skip = 0)
        => Ok(await (
            from user in userManager.Users.Skip(skip).Take(count)
            select ClientUserInfoModel.FromUserInfo(user)
           ).ToListAsync());

    /// <summary>
    /// 获取全部日志
    /// </summary>
    /// <remarks>
    /// 使用此接口获取全部日志，需要Admin权限
    /// </remarks>
    /// <response code="200">日志列表</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpGet("Logs/{level:alpha}")]
    [ProducesResponseType(typeof(List<ClientUserInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logs([FromRoute] string level = "All", [FromQuery] int count = 50, [FromQuery] int skip = 0, CancellationToken token = default)
        => Ok(await logRepository.GetLogs(skip, count, level, token));
}