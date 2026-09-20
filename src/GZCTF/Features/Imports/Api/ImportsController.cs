using System.Net.Mime;
using GZCTF.Features.Imports.Application;
using GZCTF.Features.Imports.Infrastructure;
using GZCTF.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.Imports.Api;

[RequireAdmin]
[ApiController]
[Route("api/admin/imports")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ImportsController(
    LegacyZipSource source,
    ImportBlobStaging staging,
    CanonicalImportService importer) : ControllerBase
{
    [HttpPost("zip")]
    [RequestSizeLimit(LegacyZipSource.MaxExpandedBytes)]
    public async Task<ActionResult<CanonicalImportResult>> Upload(
        IFormFile package, CancellationToken token)
    {
        if (package.Length == 0)
            return BadRequest("import.empty_package");
        await using var input = package.OpenReadStream();
        var parsed = await source.ReadAsync(input, token);
        var batchId = Guid.CreateVersion7();
        var staged = await staging.StageAsync(parsed, batchId, token);
        try
        {
            return Ok(await importer.ImportAsync(parsed.Graph, token));
        }
        catch
        {
            await staging.CompensateAsync(staged, token);
            throw;
        }
    }
}
