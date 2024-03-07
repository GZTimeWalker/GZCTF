using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

[ApiController]
[Route("/error")]
public class ErrorController : ControllerBase
{
    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpDelete]
    [Route("500")]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> InternalServerError(CancellationToken cancellationToken) => Task.FromResult<IActionResult>(StatusCode(500, new RequestResponse(GZCTF.Program.StaticLocalizer[nameof(GZCTF.Resources.Program.Identity_DefaultError)], StatusCodes.Status500InternalServerError)));
}