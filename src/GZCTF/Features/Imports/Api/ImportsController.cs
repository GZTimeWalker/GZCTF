using System.Net.Mime;
using GZCTF.Features.Imports.Application;
using GZCTF.Features.Imports.Domain;
using GZCTF.Features.Imports.Infrastructure;
using GZCTF.Middlewares;
using GZCTF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.Imports.Api;

[RequireAdmin]
[ApiController]
[Route("api/admin/imports")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ImportsController(
    AppDbContext db,
    LegacyZipSource source,
    ImportBlobStaging staging,
    CanonicalImportService importer) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ImportBatchResponse>>> List(CancellationToken token)
    {
        var batches = await db.MigrationBatches.AsNoTracking()
            .OrderByDescending(item => item.StartedAtUtc)
            .Select(item => new ImportBatchResponse(
                item.Id, item.SourceType, item.State, item.ChallengeCount, item.PathCount,
                item.WarningCount, item.ErrorCount, item.ParityReportJson, item.StartedAtUtc,
                item.CompletedAtUtc))
            .ToArrayAsync(token);
        return Ok(batches);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ImportBatchResponse>> Get(Guid id, CancellationToken token)
    {
        var batch = await db.MigrationBatches.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new ImportBatchResponse(
                item.Id, item.SourceType, item.State, item.ChallengeCount, item.PathCount,
                item.WarningCount, item.ErrorCount, item.ParityReportJson, item.StartedAtUtc,
                item.CompletedAtUtc))
            .SingleOrDefaultAsync(token);
        return batch is null ? NotFound() : Ok(batch);
    }

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

public sealed record ImportBatchResponse(
    Guid Id,
    string SourceType,
    MigrationBatchState State,
    int ChallengeCount,
    int PathCount,
    int WarningCount,
    int ErrorCount,
    string? ParityReportJson,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);
