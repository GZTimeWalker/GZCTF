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

    public AdminController()
    {

    }

}