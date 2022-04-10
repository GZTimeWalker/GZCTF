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
    /// <param name="token"></param>
    /// <response code="200">成功获取文件</response>
    [HttpPost("Notices")]
    [ProducesResponseType(typeof(Notice), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddNotice([FromBody] NoticeModel model, CancellationToken token)
    {
        var res = await noticeRepository.AddNotice(new() {
             Content = model.Content,
             Title = model.Title,
             IsPinned = model.IsPinned,
             PublishTimeUTC = DateTimeOffset.UtcNow
        }, token);
        return Ok(res);
    }

    /// <summary>
    /// 获取所有公告
    /// </summary>
    /// <remarks>
    /// 获取所有公告，需要管理员权限
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">成功获取文件</response>
    [HttpGet("Notices")]
    [ProducesResponseType(typeof(Notice[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotices(CancellationToken token) 
        => Ok(await noticeRepository.GetNotices(token));

    /// <summary>
    /// 修改公告
    /// </summary>
    /// <remarks>
    /// 修改公告，需要管理员权限
    /// </remarks>
    /// <param name="id">公告Id</param>
    /// <param name="token"></param>
    /// <param name="model"></param>
    /// <response code="200">成功修改公告</response>
    /// <response code="404">未找到公告</response>
    [HttpPut("Notices/{id}")]
    [ProducesResponseType(typeof(Notice), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNotice(int id, [FromBody] NoticeModel model, CancellationToken token) 
    {
        var notice = await noticeRepository.GetNoticeById(id, token);

        if (notice is null)
            return NotFound(new RequestResponse("公告未找到", 404));

        notice.UpdateInfo(model);
        await noticeRepository.UpdateAsync(notice, token);

        return Ok(notice);
    }

    /// <summary>
    /// 删除公告
    /// </summary>
    /// <remarks>
    /// 删除公告，需要管理员权限
    /// </remarks>
    /// <param name="id">公告Id</param>
    /// <param name="token"></param>
    /// <param name="model"></param>
    /// <response code="200">成功删除公告</response>
    /// <response code="404">未找到公告</response>
    [HttpDelete("Notices/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotice(int id, [FromBody] NoticeModel model, CancellationToken token)
    {
        var notice = await noticeRepository.GetNoticeById(id, token);

        if (notice is null)
            return NotFound(new RequestResponse("公告未找到", 404));

        await noticeRepository.RemoveNotice(notice, token);

        return Ok();
    }
}
