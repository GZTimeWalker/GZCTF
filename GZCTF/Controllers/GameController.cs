using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Net.Mime;

namespace CTFServer.Controllers;

/// <summary>
/// 比赛数据交互接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class GameController : ControllerBase
{
    private static readonly Logger logger = LogManager.GetLogger("GameController");
    private readonly IGameRepository gameRepository;

    public GameController(IGameRepository _gameRepository)
    {
        gameRepository = _gameRepository;
    }
}
