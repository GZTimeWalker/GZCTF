using GZCTF.Middlewares;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace GZCTF.Controllers;

/// <summary>
/// User metadata field management APIs
/// </summary>
[Route("api/admin/[controller]")]
[ApiController]
[RequireAdmin]
public class MetadataController(IUserMetadataFieldRepository metadataRepository, IStringLocalizer<Program> localizer)
    : ControllerBase
{
    /// <summary>
    /// Get all metadata fields
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(UserMetadataField[]), StatusCodes.Status200OK)]
    public async Task<IEnumerable<UserMetadataField>> Get(CancellationToken token)
    {
        var fields = await metadataRepository.GetAllAsync(token);
        return fields.Values;
    }

    /// <summary>
    /// Create a new metadata field
    /// </summary>
    /// <param name="field"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] UserMetadataField field, CancellationToken token)
    {
        if (await metadataRepository.GetByKeyAsync(field.Key, token) != null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Model_FieldKeyAlreadyExists)]));


        await metadataRepository.CreateAsync(field, token);
        return Ok();
    }

    /// <summary>
    /// Update a metadata field
    /// </summary>
    /// <param name="key"></param>
    /// <param name="field"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string key, [FromBody] UserMetadataField field, CancellationToken token)
    {
        if (key != field.Key)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Model_KeyMismatch)]));

        if (await metadataRepository.GetByKeyAsync(key, token) == null)
            return NotFound();

        await metadataRepository.UpdateAsync(field, token);
        return Ok();
    }

    /// <summary>
    /// Delete a metadata field
    /// </summary>
    /// <param name="key"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(string key, CancellationToken token)
    {
        await metadataRepository.DeleteAsync(key, token);
        return Ok();
    }
}
