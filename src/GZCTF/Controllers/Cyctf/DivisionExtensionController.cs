using GZCTF.Middlewares;
using GZCTF.Models.Request.Cyctf;
using GZCTF.Models.Response.Cyctf;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace GZCTF.Controllers.Cyctf;

/// <summary>
/// CYCTF 组别扩展 API
/// </summary>
[Route("api/cyctf/divisions/{divisionId:int}/extension")]
[ApiController]
[Authorize(Policy = "Admin")]
public class DivisionExtensionController(
    IDivisionExtensionRepository divisionExtensionRepository,
    IDivisionRepository divisionRepository,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    /// <summary>
    /// 获取组别扩展信息
    /// </summary>
    /// <param name="divisionId">组别 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取组别扩展信息</response>
    /// <response code="404">组别或扩展信息不存在</response>
    [HttpGet]
    [ProducesResponseType(typeof(DivisionExtensionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDivisionExtension(int divisionId, CancellationToken token)
    {
        var extension = await divisionExtensionRepository.GetDivisionExtensionByDivisionId(divisionId, token);
        if (extension is null)
            return NotFound(new RequestResponse("组别扩展信息不存在", StatusCodes.Status404NotFound));

        return Ok(DivisionExtensionResponse.FromEntity(extension));
    }

    /// <summary>
    /// 创建或更新组别扩展信息
    /// </summary>
    /// <param name="divisionId">组别 ID</param>
    /// <param name="request">扩展信息请求</param>
    /// <param name="token"></param>
    /// <response code="200">成功创建或更新组别扩展信息</response>
    /// <response code="404">组别不存在</response>
    [HttpPut]
    [ProducesResponseType(typeof(DivisionExtensionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrUpdateDivisionExtension(int divisionId, [FromBody] DivisionExtensionRequest request, CancellationToken token)
    {
        var extension = new Models.Data.Cyctf.DivisionExtension
        {
            DivisionId = divisionId,
            MinTeamSize = request.MinTeamSize,
            MaxTeamSize = request.MaxTeamSize,
            RegistrationFields = request.RegistrationFields
        };

        var result = await divisionExtensionRepository.CreateOrUpdateDivisionExtension(extension, token);
        return Ok(DivisionExtensionResponse.FromEntity(result));
    }

    /// <summary>
    /// 删除组别扩展信息
    /// </summary>
    /// <param name="divisionId">组别 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功删除组别扩展信息</response>
    /// <response code="404">组别或扩展信息不存在</response>
    [HttpDelete]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDivisionExtension(int divisionId, CancellationToken token)
    {
        var success = await divisionExtensionRepository.DeleteDivisionExtension(divisionId, token);
        if (!success)
            return NotFound(new RequestResponse("组别扩展信息不存在", StatusCodes.Status404NotFound));

        return Ok(new RequestResponse("成功删除组别扩展信息", StatusCodes.Status200OK));
    }

    /// <summary>
    /// 获取比赛下所有组别的扩展信息
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取组别扩展信息列表</response>
    [HttpGet("/api/cyctf/games/{gameId:int}/division-extensions")]
    [ProducesResponseType(typeof(DivisionExtensionResponse[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDivisionExtensionsByGameId(int gameId, CancellationToken token)
    {
        var extensions = await divisionExtensionRepository.GetDivisionExtensionsByGameId(gameId, token);
        return Ok(extensions.Select(DivisionExtensionResponse.FromEntity));
    }
}
