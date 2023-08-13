using System.Net.Mime;
using GZCTF.Extensions;
using GZCTF.Middlewares;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Game;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

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
    private readonly ILogger<EditController> _logger;
    private readonly UserManager<UserInfo> _userManager;
    private readonly IPostRepository _postRepository;
    private readonly IGameNoticeRepository _gameNoticeRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IChallengeRepository _challengeRepository;
    private readonly IFileRepository _fileService;
    private readonly IContainerService _containerService;
    private readonly IContainerRepository _containerRepository;

    public EditController(UserManager<UserInfo> userManager,
        ILogger<EditController> logger,
        IPostRepository postRepository,
        IContainerRepository containerRepository,
        IChallengeRepository challengeRepository,
        IGameNoticeRepository gameNoticeRepository,
        IGameRepository gameRepository,
        IContainerService containerService,
        IFileRepository fileService)
    {
        _logger = logger;
        _fileService = fileService;
        _userManager = userManager;
        _gameRepository = gameRepository;
        _postRepository = postRepository;
        _containerService = containerService;
        _challengeRepository = challengeRepository;
        _containerRepository = containerRepository;
        _gameNoticeRepository = gameNoticeRepository;
    }

    /// <summary>
    /// 添加文章
    /// </summary>
    /// <remarks>
    /// 添加文章，需要管理员权限
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加文章</response>
    [HttpPost("Posts")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddPost([FromBody] PostEditModel model, CancellationToken token)
    {
        var user = await _userManager.GetUserAsync(User);
        var res = await _postRepository.CreatePost(new Post().Update(model, user!), token);
        return Ok(res.Id);
    }

    /// <summary>
    /// 修改文章
    /// </summary>
    /// <remarks>
    /// 修改文章，需要管理员权限
    /// </remarks>
    /// <param name="id">文章Id</param>
    /// <param name="token"></param>
    /// <param name="model"></param>
    /// <response code="200">成功修改文章</response>
    /// <response code="404">未找到文章</response>
    [HttpPut("Posts/{id}")]
    [ProducesResponseType(typeof(PostDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePost(string id, [FromBody] PostEditModel model, CancellationToken token)
    {
        var post = await _postRepository.GetPostById(id, token);

        if (post is null)
            return NotFound(new RequestResponse("文章未找到", 404));

        var user = await _userManager.GetUserAsync(User);

        await _postRepository.UpdatePost(post.Update(model, user!), token);

        return Ok(PostDetailModel.FromPost(post));
    }

    /// <summary>
    /// 删除文章
    /// </summary>
    /// <remarks>
    /// 删除文章，需要管理员权限
    /// </remarks>
    /// <param name="id">文章Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功删除文章</response>
    /// <response code="404">未找到文章</response>
    [HttpDelete("Posts/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(string id, CancellationToken token)
    {
        var post = await _postRepository.GetPostById(id, token);

        if (post is null)
            return NotFound(new RequestResponse("文章未找到", 404));

        await _postRepository.RemovePost(post, token);

        return Ok();
    }

    /// <summary>
    /// 添加比赛
    /// </summary>
    /// <remarks>
    /// 添加比赛，需要管理员权限
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛</response>
    [HttpPost("Games")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddGame([FromBody] GameInfoModel model, CancellationToken token)
    {
        var game = await _gameRepository.CreateGame(new Game().Update(model), token);

        if (game is null)
            return BadRequest(new RequestResponse("比赛创建失败", 400));

        _gameRepository.FlushGameInfoCache();

        return Ok(GameInfoModel.FromGame(game));
    }

    /// <summary>
    /// 获取比赛列表
    /// </summary>
    /// <remarks>
    /// 获取比赛列表，需要管理员权限
    /// </remarks>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛列表</response>
    [HttpGet("Games")]
    [ProducesResponseType(typeof(ArrayResponse<GameInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGames([FromQuery] int count, [FromQuery] int skip, CancellationToken token)
        => Ok((await _gameRepository.GetGames(count, skip, token))
            .Select(GameInfoModel.FromGame)
            .ToResponse(await _gameRepository.CountAsync(token)));

    /// <summary>
    /// 获取比赛
    /// </summary>
    /// <remarks>
    /// 获取比赛，需要管理员权限
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛</response>
    [HttpGet("Games/{id}")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGame([FromRoute] int id, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        return Ok(GameInfoModel.FromGame(game));
    }

    /// <summary>
    /// 获取比赛队伍 Hash 的加盐
    /// </summary>
    /// <remarks>
    /// 获取比赛队伍 Hash 的加盐，需要管理员权限
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛哈希加盐</response>
    [HttpGet("Games/{id}/TeamHashSalt")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamHashSalt([FromRoute] int id, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        // ref: TeamToken =>
        // Codec.StrSHA256($"{part.Token}::{part.Game.TeamHashSalt}::{Id}");
        return Ok(game.TeamHashSalt);
    }

    /// <summary>
    /// 修改比赛
    /// </summary>
    /// <remarks>
    /// 修改比赛，需要管理员权限
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功修改比赛</response>
    [HttpPut("Games/{id}")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGame([FromRoute] int id, [FromBody] GameInfoModel model, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        game.Update(model);
        await _gameRepository.SaveAsync(token);
        _gameRepository.FlushGameInfoCache();
        await _gameRepository.FlushScoreboardCache(game.Id, token);

        return Ok(GameInfoModel.FromGame(game));
    }

    /// <summary>
    /// 删除比赛
    /// </summary>
    /// <remarks>
    /// 删除比赛，需要管理员权限
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">成功删除比赛</response>
    [HttpDelete("Games/{id}")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGame([FromRoute] int id, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        await _gameRepository.DeleteGame(game, token);

        return Ok();
    }

    /// <summary>
    /// 更新比赛头图
    /// </summary>
    /// <remarks>
    /// 使用此接口更新比赛头图，需要Admin权限
    /// </remarks>
    /// <response code="200">比赛头图URL</response>
    /// <response code="400">非法请求</response>
    /// <response code="401">未授权用户</response>
    [HttpPut("Games/{id}/Poster")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGamePoster([FromRoute] int id, IFormFile file, CancellationToken token)
    {
        if (file.Length == 0)
            return BadRequest(new RequestResponse("文件非法"));

        if (file.Length > 3 * 1024 * 1024)
            return BadRequest(new RequestResponse("文件过大"));

        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var poster = await _fileService.CreateOrUpdateImage(file, "poster", token, 0);

        if (poster is null)
            return BadRequest(new RequestResponse("文件创建失败"));

        game.PosterHash = poster.Hash;
        await _gameRepository.SaveAsync(token);
        _gameRepository.FlushGameInfoCache();

        return Ok(poster.Url());
    }

    /// <summary>
    /// 添加比赛通知
    /// </summary>
    /// <remarks>
    /// 添加比赛通知，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="model">通知内容</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛通知</response>
    [HttpPost("Games/{id}/Notices")]
    [ProducesResponseType(typeof(GameNotice), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddGameNotice([FromRoute] int id, [FromBody] GameNoticeModel model, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var res = await _gameNoticeRepository.AddNotice(new()
        {
            Content = model.Content,
            GameId = game.Id,
            Type = NoticeType.Normal,
            PublishTimeUTC = DateTimeOffset.UtcNow
        }, token);

        return Ok(res);
    }

    /// <summary>
    /// 获取比赛通知
    /// </summary>
    /// <remarks>
    /// 获取比赛通知，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛通知</response>
    [HttpGet("Games/{id}/Notices")]
    [ProducesResponseType(typeof(GameNotice[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameNotices([FromRoute] int id, CancellationToken token = default)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        return Ok(await _gameNoticeRepository.GetNormalNotices(id, token));
    }

    /// <summary>
    /// 更新比赛通知
    /// </summary>
    /// <remarks>
    /// 更新比赛通知，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="noticeId">通知Id</param>
    /// <param name="model">通知内容</param>
    /// <param name="token"></param>
    /// <response code="200">成功更新通知</response>
    [HttpPut("Games/{id}/Notices/{noticeId}")]
    [ProducesResponseType(typeof(GameNotice), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGameNotice([FromRoute] int id, [FromRoute] int noticeId, [FromBody] GameNoticeModel model, CancellationToken token = default)
    {
        var notice = await _gameNoticeRepository.GetNoticeById(id, noticeId, token);

        if (notice is null)
            return NotFound(new RequestResponse("通知未找到", 404));

        if (notice.Type != NoticeType.Normal)
            return BadRequest(new RequestResponse("不能更改系统通知"));

        notice.Content = model.Content;
        return Ok(await _gameNoticeRepository.UpdateNotice(notice, token));
    }

    /// <summary>
    /// 删除比赛通知
    /// </summary>
    /// <remarks>
    /// 删除比赛通知，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="noticeId">文章Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功删除文章</response>
    /// <response code="404">未找到文章</response>
    [HttpDelete("Games/{id}/Notices/{noticeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGameNotice([FromRoute] int id, [FromRoute] int noticeId, CancellationToken token)
    {
        var notice = await _gameNoticeRepository.GetNoticeById(id, noticeId, token);

        if (notice is null)
            return NotFound(new RequestResponse("通知未找到", 404));

        if (notice.Type != NoticeType.Normal)
            return BadRequest(new RequestResponse("不能删除系统通知"));

        await _gameNoticeRepository.RemoveNotice(notice, token);

        return Ok();
    }

    /// <summary>
    /// 添加比赛题目
    /// </summary>
    /// <remarks>
    /// 添加比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpPost("Games/{id}/Challenges")]
    [ProducesResponseType(typeof(ChallengeEditDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddGameChallenge([FromRoute] int id, [FromBody] ChallengeInfoModel model, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var res = await _challengeRepository.CreateChallenge(game, new Challenge()
        {
            Title = model.Title,
            Type = model.Type,
            Tag = model.Tag
        }, token);

        return Ok(ChallengeEditDetailModel.FromChallenge(res));
    }

    /// <summary>
    /// 获取全部比赛题目
    /// </summary>
    /// <remarks>
    /// 获取全部比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目</response>
    [HttpGet("Games/{id}/Challenges")]
    [ProducesResponseType(typeof(ChallengeInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameChallenges([FromRoute] int id, CancellationToken token)
        => Ok((await _challengeRepository.GetChallenges(id, token)).Select(ChallengeInfoModel.FromChallenge));

    /// <summary>
    /// 获取比赛题目
    /// </summary>
    /// <remarks>
    /// 获取比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpGet("Games/{id}/Challenges/{cId}")]
    [ProducesResponseType(typeof(ChallengeEditDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameChallenge([FromRoute] int id, [FromRoute] int cId, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var res = await _challengeRepository.GetChallenge(id, cId, true, token);

        if (res is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        return Ok(ChallengeEditDetailModel.FromChallenge(res));
    }

    /// <summary>
    /// 修改比赛题目信息，Flags 不受更改，使用 Flag 相关 API 修改
    /// </summary>
    /// <remarks>
    /// 修改比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="model">题目信息</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpPut("Games/{id}/Challenges/{cId}")]
    [ProducesResponseType(typeof(ChallengeEditDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGameChallenge([FromRoute] int id, [FromRoute] int cId, [FromBody] ChallengeUpdateModel model, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var res = await _challengeRepository.GetChallenge(id, cId, true, token);

        if (res is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        // NOTE: IsEnabled can only be updated outside of the edit page
        if (model.IsEnabled == true && !res.Flags.Any() && res.Type != ChallengeType.DynamicContainer)
            return BadRequest(new RequestResponse("题目无 flag，不可启用"));

        if (model.FileName is not null && string.IsNullOrWhiteSpace(model.FileName))
            return BadRequest(new RequestResponse("动态附件名不可为空"));

        bool hintUpdated = model.IsHintUpdated(res.Hints?.GetSetHashCode());

        if (!string.IsNullOrWhiteSpace(model.FlagTemplate) && res.Type == ChallengeType.DynamicContainer && !model.IsValidFlagTemplate())
            return BadRequest(new RequestResponse("flag 复杂度不足，请考虑添加队伍哈希或增加长度"));

        res.Update(model);

        if (model.IsEnabled == true)
        {
            // will also update IsEnabled
            await _challengeRepository.EnsureInstances(res, game, token);

            if (game.IsActive)
            {
                await _gameNoticeRepository.AddNotice(new()
                {
                    Game = game,
                    Type = NoticeType.NewChallenge,
                    Content = $"新增了题目 「{res.Title}」",
                }, token);
            }
        }
        else
            await _challengeRepository.SaveAsync(token);

        if (game.IsActive && res.IsEnabled && hintUpdated)
        {
            await _gameNoticeRepository.AddNotice(new()
            {
                Game = game,
                Type = NoticeType.NewHint,
                Content = $"「{res.Title}」 更新了提示",
            }, token);
        }

        // always flush scoreboard
        await _gameRepository.FlushScoreboardCache(game.Id, token);

        return Ok(ChallengeEditDetailModel.FromChallenge(res));
    }

    /// <summary>
    /// 测试比赛题目容器
    /// </summary>
    /// <remarks>
    /// 测试比赛题目容器，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功开启比赛题目容器</response>
    [HttpPost("Games/{id}/Challenges/{cId}/Container")]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTestContainer([FromRoute] int id, [FromRoute] int cId, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var challenge = await _challengeRepository.GetChallenge(id, cId, true, token);

        if (challenge is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        if (!challenge.Type.IsContainer())
            return BadRequest(new RequestResponse("题目类型不可创建容器"));

        if (challenge.ContainerImage is null || challenge.ContainerExposePort is null)
            return BadRequest(new RequestResponse("容器配置错误"));

        var user = await _userManager.GetUserAsync(User);

        var container = await _containerService.CreateContainerAsync(new()
        {
            TeamId = "admin",
            UserId = user!.Id,
            Flag = challenge.Type.IsDynamic() ? challenge.GenerateTestFlag() : null,
            Image = challenge.ContainerImage,
            CPUCount = challenge.CPUCount ?? 1,
            MemoryLimit = challenge.MemoryLimit ?? 64,
            StorageLimit = challenge.StorageLimit ?? 256,
            ExposedPort = challenge.ContainerExposePort ?? throw new ArgumentException("创建容器时遇到无效的端口"),
        }, token);

        if (container is null)
            return BadRequest(new RequestResponse("容器创建失败"));

        challenge.TestContainer = container;
        await _challengeRepository.SaveAsync(token);

        _logger.Log($"成功创建测试容器 {container.ContainerId}", user, TaskStatus.Success);

        return Ok(ContainerInfoModel.FromContainer(container));
    }

    /// <summary>
    /// 关闭测试比赛题目容器
    /// </summary>
    /// <remarks>
    /// 关闭测试比赛题目容器，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpDelete("Games/{id}/Challenges/{cId}/Container")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DestroyTestContainer([FromRoute] int id, [FromRoute] int cId, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var challenge = await _challengeRepository.GetChallenge(id, cId, true, token);

        if (challenge is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        if (challenge.TestContainer is null)
            return Ok();

        await _containerService.DestroyContainerAsync(challenge.TestContainer, token);
        await _containerRepository.RemoveContainer(challenge.TestContainer, token);

        return Ok();
    }

    /// <summary>
    /// 删除比赛题目
    /// </summary>
    /// <remarks>
    /// 删除比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpDelete("Games/{id}/Challenges/{cId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveGameChallenge([FromRoute] int id, [FromRoute] int cId, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var res = await _challengeRepository.GetChallenge(id, cId, true, token);

        if (res is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        await _challengeRepository.RemoveChallenge(res, token);

        // always flush scoreboard
        await _gameRepository.FlushScoreboardCache(game.Id, token);

        return Ok();
    }

    /// <summary>
    /// 更新比赛题目附件
    /// </summary>
    /// <remarks>
    /// 更新比赛题目附件，需要管理员权限，仅用于非动态附件题目
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目数量</response>
    [HttpPost("Games/{id}/Challenges/{cId}/Attachment")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAttachment([FromRoute] int id, [FromRoute] int cId, [FromBody] AttachmentCreateModel model, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var challenge = await _challengeRepository.GetChallenge(id, cId, true, token);

        if (challenge is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        if (challenge.Type == ChallengeType.DynamicAttachment)
            return BadRequest(new RequestResponse("动态附件题目请使用 assets API 上传附件"));

        await _challengeRepository.UpdateAttachment(challenge, model, token);

        return Ok();
    }

    /// <summary>
    /// 添加比赛题目 Flag
    /// </summary>
    /// <remarks>
    /// 添加比赛题目 Flag，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="models"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目数量</response>
    [HttpPost("Games/{id}/Challenges/{cId}/Flags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFlags([FromRoute] int id, [FromRoute] int cId, [FromBody] FlagCreateModel[] models, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var challenge = await _challengeRepository.GetChallenge(id, cId, true, token);

        if (challenge is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        await _challengeRepository.AddFlags(challenge, models, token);

        return Ok();
    }

    /// <summary>
    /// 删除比赛题目 Flag
    /// </summary>
    /// <remarks>
    /// 删除比赛题目 Flag，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="fId">Flag ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpDelete("Games/{id}/Challenges/{cId}/Flags/{fId}")]
    [ProducesResponseType(typeof(TaskStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFlag([FromRoute] int id, [FromRoute] int cId, [FromRoute] int fId, CancellationToken token)
    {
        var game = await _gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var challenge = await _challengeRepository.GetChallenge(id, cId, true, token);

        if (challenge is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        return Ok(await _challengeRepository.RemoveFlag(challenge, fId, token));
    }
}
