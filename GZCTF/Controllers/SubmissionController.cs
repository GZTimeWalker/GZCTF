using Microsoft.AspNetCore.Mvc;
using CTFServer.Middlewares;
using CTFServer.Models;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using NLog;
using System.Net.Mime;
using System.Security.Claims;

namespace CTFServer.Controllers;

/// <summary>
/// 提交数据交互接口
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class SubmissionController : ControllerBase
{
    private static readonly Logger logger = LogManager.GetLogger("SubmissionController");
    private readonly ISubmissionRepository submissionRepository;

    public SubmissionController(
        ISubmissionRepository _submissionRepository)
    {
        submissionRepository = _submissionRepository;
    }

    /// <summary>
    /// 获取当前用户最新提交接口
    /// </summary>
    /// <remarks>
    /// 使用此接口获取当前用户最新提交，限制为10个，需要SignedIn权限
    /// </remarks>
    /// <param name="Id">题目Id</param>
    /// <param name="token">操作取消token</param>
    /// <response code="200">成功获取提交</response>
    /// <response code="401">无权访问</response>
    [HttpGet("/api/[controller]/{Id:int}")]
    [RequireSignedIn]
    [ProducesResponseType(typeof(List<Submission>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SelfHistory(int Id, CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var submissions = await submissionRepository.GetSubmissions(token, 0, 10, Id, userId);

        return Ok(submissions);
    }

    /// <summary>
    /// 获取全部用户最新提交接口
    /// </summary>
    /// <remarks>
    /// 使用此接口获取当前用户最新提交，限制为50个，Id为空可获取全部，需要Monitor权限
    /// </remarks>
    /// <param name="Id">题目Id</param>
    /// <param name="token">操作取消token</param>
    /// <response code="200">成功获取提交</response>
    /// <response code="401">无权访问</response>
    [HttpGet("{Id:int?}")]
    [RequireMonitor]
    [ProducesResponseType(typeof(List<Submission>), StatusCodes.Status200OK)]
    public async Task<IActionResult> History(int? Id, CancellationToken token)
        => Ok(await submissionRepository.GetSubmissions(token, 0, 10, Id ?? 0));
}
