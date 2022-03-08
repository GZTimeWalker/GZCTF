using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using CTFServer.Middlewares;
using CTFServer.Models;
using CTFServer.Models.Request;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using NLog;
using System.Net.Mime;
using System.Security.Claims;

namespace CTFServer.Controllers;

/// <summary>
/// 题目数据交互接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
public class PuzzleController : ControllerBase
{
    private readonly int MAX_ACCESS_LEVEL = 0;
    private static readonly Logger logger = LogManager.GetLogger("PuzzleController");
    private readonly UserManager<UserInfo> userManager;
    private readonly IRankRepository rankRepository;
    private readonly ISubmissionRepository submissionRepository;
    private readonly IPuzzleRepository puzzleRepository;
    private readonly IMemoryCache cache;

    public PuzzleController(
        IMemoryCache memoryCache,
        UserManager<UserInfo> _userManager,
        IPuzzleRepository _puzzleRepository,
        ISubmissionRepository _submissionRepository,
        IRankRepository _rankRepository)
    {
        cache = memoryCache;
        userManager = _userManager;
        rankRepository = _rankRepository;
        puzzleRepository = _puzzleRepository;
        submissionRepository = _submissionRepository;

        if (!cache.TryGetValue(CacheKey.MaxAccessLevel, out MAX_ACCESS_LEVEL))
        {
            MAX_ACCESS_LEVEL = puzzleRepository.GetMaxAccessLevel();
            LogHelper.SystemLog(logger, $"题目最高访问等级：{MAX_ACCESS_LEVEL}");
            cache.Set(CacheKey.MaxAccessLevel, MAX_ACCESS_LEVEL, TimeSpan.FromDays(7));
        }
    }

    /// <summary>
    /// 新建题目接口
    /// </summary>
    /// <remarks>
    /// 使用此接口添加新题目，需要Admin权限
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token">操作取消token</param>
    /// <response code="200">成功新建题目</response>
    /// <response code="400">校验失败</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">无权访问</response>
    [HttpPost("New")]
    [RequireAdmin]
    [ProducesResponseType(typeof(PuzzleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> New([FromBody] PuzzleBase model, CancellationToken token)
    {
        var puzzle = await puzzleRepository.AddPuzzle(model, token);

        if (puzzle is null)
            return BadRequest(new RequestResponse("无效的题目"));

        for (int i = model.AccessLevel; i <= MAX_ACCESS_LEVEL; ++i)
            cache.Remove(CacheKey.AccessiblePuzzles(i));

        cache.Remove(CacheKey.MaxAccessLevel);

        return Ok(new PuzzleResponse(puzzle.Id));
    }

    /// <summary>
    /// 更新题目接口
    /// </summary>
    /// <remarks>
    /// 使用此接口更新题目，需要Admin权限
    /// </remarks>
    /// <param name="id">题目Id</param>
    /// <param name="model"></param>
    /// <param name="token">操作取消token</param>
    /// <response code="200">成功更新题目</response>
    /// <response code="400">校验失败</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">无权访问</response>
    [HttpPut("{id:int}")]
    [RequireAdmin]
    [ProducesResponseType(typeof(PuzzleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] PuzzleBase model, CancellationToken token)
    {
        var puzzle = await puzzleRepository.UpdatePuzzle(id, model, token);

        if (puzzle is null)
            return BadRequest(new RequestResponse("未找到题目"));

        for (int i = model.AccessLevel; i <= MAX_ACCESS_LEVEL; ++i)
            cache.Remove(CacheKey.AccessiblePuzzles(i));

        cache.Remove(CacheKey.MaxAccessLevel);

        return Ok(new PuzzleResponse(puzzle.Id));
    }

    /// <summary>
    /// 获取题目接口
    /// </summary>
    /// <remarks>
    /// 使用此接口更新题目，需要SignedIn权限
    /// </remarks>
    /// <param name="id">题目Id</param>
    /// <param name="token">操作取消token</param>
    /// <response code="200">成功获取题目</response>
    /// <response code="401">题目无效</response>
    /// <response code="403">无权访问</response>
    [HttpGet("{id:int}")]
    [TimeCheck]
    [RequireSignedIn]
    [ProducesResponseType(typeof(UserPuzzleModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var puzzle = await puzzleRepository.GetUserPuzzle(id, user.AccessLevel, token);

        if (puzzle is null)
        {
            LogHelper.Log(logger, $"试图获取未授权题目#{id}", user, TaskStatus.Denied);
            return new JsonResult(new RequestResponse("无权访问或题目无效", 403))
            {
                StatusCode = 403
            };
        }

        return Ok(puzzle);
    }

    /// <summary>
    /// 删除题目接口
    /// </summary>
    /// <remarks>
    /// 使用此接删除题目，需要Admin权限
    /// </remarks>
    /// <param name="id">题目Id</param>
    /// <param name="token">操作取消token</param>
    /// <response code="200">成功删除题目</response>
    /// <response code="400">题目删除失败</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">无权访问</response>
    [HttpDelete("{id:int}")]
    [RequireAdmin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        var (res, title) = await puzzleRepository.DeletePuzzle(id, token);

        if (!res)
            return BadRequest(new RequestResponse("题目删除失败"));

        for (int i = 0; i <= MAX_ACCESS_LEVEL; ++i)
            cache.Remove(CacheKey.AccessiblePuzzles(i));

        cache.Remove(CacheKey.MaxAccessLevel);

        var user = await userManager.GetUserAsync(User);

        LogHelper.Log(logger, $"删除题目#{id} {title}", user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// 获取当前可访问题目列表
    /// </summary>
    /// <remarks>
    /// 使用此接口获取当前用户可访问题目列表，需要SignedIn权限
    /// </remarks>
    /// <param name="token">操作取消token</param>
    /// <response code="200">答案正确</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">无权访问</response>
    [HttpGet("List")]
    [TimeCheck]
    [RequireSignedIn]
    [ProducesResponseType(typeof(PuzzleListModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPuzzleList(CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        int accessLevel = user.AccessLevel;

        if (!cache.TryGetValue(CacheKey.AccessiblePuzzles(accessLevel), out List<PuzzleItem> accessible))
        {
            accessible = await puzzleRepository.GetAccessiblePuzzles(accessLevel, token);
            // LogHelper.SystemLog(logger, $"重构缓存：AccessiblePuzzles#{accessLevel}");
            cache.Set(CacheKey.AccessiblePuzzles(accessLevel), accessible, TimeSpan.FromMinutes(10));
        }

        PuzzleListModel puzzleList = new()
        {
            Accessible = accessible,
            Solved = await submissionRepository.GetSolvedPuzzles(user.Id, token)
        };

        return Ok(puzzleList);
    }

    /// <summary>
    /// 提交题目答案接口
    /// </summary>
    /// <remarks>
    /// 使用此接口提交题目答案，此接口限制为3次每60秒，需要SignedIn权限
    /// </remarks>
    /// <param name="id">题目Id</param>
    /// <param name="model"></param>
    /// <param name="token">操作取消token</param>
    /// <response code="200">答案正确</response>
    /// <response code="400">答案错误</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">无权访问</response>
    [HttpPost("Submit/{id:int}")]
    [TimeCheck]
    [RequireSignedIn]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit(int id, [FromBody] AnswerSubmitModel model, CancellationToken token)
    {
        var user = await userManager.Users.Include(u => u.Rank)
            .SingleAsync(u => u.Id == User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken: token);

        var hasSolved = await submissionRepository.HasSubmitted(id, user.Id, token);

        var result = await puzzleRepository.VerifyAnswer(id, model.Answer, user, hasSolved, token);

        Submission sub = new()
        {
            UserId = user.Id,
            PuzzleId = id,
            Answer = model.Answer,
            Solved = result.Result == AnswerResult.Accepted,
            Score = hasSolved ? 0 : result.Score,
            SubmitTimeUTC = DateTimeOffset.UtcNow,
            UserName = user.UserName
        };

        await submissionRepository.AddSubmission(sub, token);

        if (result.Result == AnswerResult.Unauthorized)
        {
            LogHelper.Log(logger, "提交未授权的题目。", user, TaskStatus.Denied);
            return Unauthorized(new RequestResponse("无权访问或题目无效", 401));
        }

        if(user.Privilege == Privilege.User)
            for (int i = 0; i <= MAX_ACCESS_LEVEL; ++i)
                cache.Remove(CacheKey.AccessiblePuzzles(i));

        if (result.Result == AnswerResult.WrongAnswer)
        {
            LogHelper.Log(logger, $"#{id} 答案错误：{model.Answer}", user, TaskStatus.Fail);
            return BadRequest(new RequestResponse("答案错误"));
        }

        if (!hasSolved)
        {
            // These changes will be saved when following functions are called.

            if (user.Rank is null)
                user.Rank = new Rank() { UserId = user.Id };

            if(user.AccessLevel < result.UpgradeAccessLevel)
                user.AccessLevel = result.UpgradeAccessLevel;

            await rankRepository.UpdateRank(user.Rank, result.Score, token);

            cache.Remove(CacheKey.ScoreBoard);
        }

        LogHelper.Log(logger, "答案正确：" + model.Answer, user, TaskStatus.Success);

        return Ok();
    }
}
