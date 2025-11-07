using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace GZCTF.Controllers;

[ApiController]
[Route("/error")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController(IStringLocalizer<Program> localizer) : ControllerBase
{
    [Route("500")]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> InternalServerError(CancellationToken cancellationToken) =>
        Task.FromResult<IActionResult>(StatusCode(500,
            new RequestResponse(localizer[nameof(Resources.Program.Error_InternalServerError)],
                StatusCodes.Status500InternalServerError)));
}
