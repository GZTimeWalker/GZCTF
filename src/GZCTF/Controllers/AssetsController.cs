using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace GZCTF.Controllers;

/// <summary>
/// 文件交互接口
/// </summary>
[ApiController]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class AssetsController : ControllerBase
{
    private readonly ILogger<AssetsController> _logger;
    private readonly IFileRepository _fileRepository;
    private readonly FileExtensionContentTypeProvider _extProvider = new();

    public AssetsController(IFileRepository fileService, ILogger<AssetsController> logger)
    {
        _fileRepository = fileService;
        _logger = logger;
    }

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
            _logger.Log($"尝试获取不存在的文件 [{hash[..8]}] {filename}", HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "0.0.0.0", TaskStatus.NotFound, LogLevel.Warning);
            return NotFound(new RequestResponse("文件不存在", 404));
        }

        if (!_extProvider.TryGetContentType(filename, out string? contentType))
            contentType = MediaTypeNames.Application.Octet;

        return new PhysicalFileResult(path, contentType)
        {
            FileDownloadName = filename
        };
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
    public async Task<IActionResult> Upload(List<IFormFile> files, [FromQuery] string? filename, CancellationToken token)
    {
        try
        {
            List<LocalFile> results = new();
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var res = await _fileRepository.CreateOrUpdateFile(file, filename, token);
                    _logger.SystemLog($"更新文件 [{res.Hash[..8]}] {filename ?? file.FileName} @ {file.Length} bytes", TaskStatus.Success, LogLevel.Debug);
                    results.Add(res);
                }
            }
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest(new RequestResponse("遇到IO错误"));
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
        var result = await _fileRepository.DeleteFileByHash(hash, token);

        _logger.SystemLog($"删除文件 [{hash[..8]}]...", result, LogLevel.Information);

        return result switch
        {
            TaskStatus.Success => Ok(),
            TaskStatus.NotFound => NotFound(),
            _ => BadRequest(new RequestResponse("文件删除失败"))
        };
    }
}
