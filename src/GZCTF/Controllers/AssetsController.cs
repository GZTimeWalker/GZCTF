using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Localization;

namespace GZCTF.Controllers;

/// <summary>
/// 文件交互接口
/// </summary>
[ApiController]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class AssetsController(
    IFileRepository fileService,
    ILogger<AssetsController> logger,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    readonly FileExtensionContentTypeProvider _extProvider = new();

    /// <summary>
    /// 获取文件接口
    /// </summary>
    /// <remarks>
    /// 根据哈希获取文件，不匹配文件名
    /// </remarks>
    /// <param name="hash">文件哈希</param>
    /// <param name="filename">下载文件名</param>
    /// <response code="200">成功获取文件</response>
    /// <response code="404">文件未找到</response>
    [HttpGet("[controller]/{hash:length(64)}/{filename:minlength(1)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public IActionResult GetFile([RegularExpression("[0-9a-f]{64}")] string hash, string filename)
    {
        var path = Path.GetFullPath(Path.Combine(FilePath.Uploads, hash[..2], hash[2..4], hash));

        if (!System.IO.File.Exists(path))
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Assets_FileNotFound), hash[..8], filename], ip,
                TaskStatus.NotFound,
                LogLevel.Warning);
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.File_NotFound)],
                StatusCodes.Status404NotFound));
        }

        if (!_extProvider.TryGetContentType(filename, out var contentType))
            contentType = MediaTypeNames.Application.Octet;

        HttpContext.Response.Headers.CacheControl = $"public, max-age={60 * 60 * 24 * 7}";

        return new PhysicalFileResult(path, contentType) { FileDownloadName = filename };
    }

    /// <summary>
    /// 上传文件接口
    /// </summary>
    /// <remarks>
    /// 上传一个或多个文件
    /// </remarks>
    /// <param name="files"></param>
    /// <param name="filename">统一文件名</param>
    /// <param name="token"></param>
    /// <response code="200">成功上传文件</response>
    /// <response code="400">上传文件失败</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">无权访问</response>
    [RequireAdmin]
    [HttpPost("api/[controller]")]
    [ProducesResponseType(typeof(List<LocalFile>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(List<IFormFile> files, [FromQuery] string? filename,
        CancellationToken token)
    {
        try
        {
            List<LocalFile> results = new();
            foreach (IFormFile? file in files.Where(file => file.Length > 0))
            {
                LocalFile res = await fileService.CreateOrUpdateFile(file, filename, token);
                logger.SystemLog(
                    Program.StaticLocalizer[nameof(Resources.Program.Assets_UpdateFile), res.Hash[..8],
                        filename ?? file.FileName, file.Length],
                    TaskStatus.Success, LogLevel.Debug);
                results.Add(res);
            }

            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Assets_IOError)]));
        }
    }

    /// <summary>
    /// 删除文件接口
    /// </summary>
    /// <remarks>
    /// 按照文件哈希删除文件
    /// </remarks>
    /// <param name="hash"></param>
    /// <param name="token"></param>
    /// <response code="200">成功删除文件</response>
    /// <response code="400">上传文件失败</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">无权访问</response>
    [RequireAdmin]
    [HttpDelete("api/[controller]/{hash:length(64)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(string hash, CancellationToken token)
    {
        TaskStatus result = await fileService.DeleteFileByHash(hash, token);

        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.Assets_DeleteFile), hash[..8]], result,
            LogLevel.Information);

        return result switch
        {
            TaskStatus.Success => Ok(),
            TaskStatus.NotFound => NotFound(),
            _ => BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_DeletionFailed)]))
        };
    }
}
