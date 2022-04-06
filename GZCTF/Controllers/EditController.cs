using CTFServer.Middlewares;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using CTFServer.Models.Request.Edit;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace CTFServer.Controllers;

/// <summary>
/// 数据修改交互接口
/// </summary>
[RequireAdmin]
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class EditController : Controller
{
    private readonly INoticeRepository noticeRepository;

    public EditController(INoticeRepository _noticeRepository)
    {
        noticeRepository = _noticeRepository;
    }

    /// <summary>
    /// 添加公告
    /// </summary>
    /// <remarks>
    /// 添加公告，需要管理员权限
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">成功获取文件</response>
    /// <response code="404">文件未找到</response>
    [HttpGet("Notice")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> AddNotice(NoticeModel model, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
