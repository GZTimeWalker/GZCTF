using Microsoft.AspNetCore.Mvc;
using CTFServer.Utils;
using NLog;
using System.Net.Mime;
using CTFServer.Repositories.Interface;
using CTFServer.Middlewares;
using System.Net.Http.Headers;
using CTFServer.Models;

namespace CTFServer.Controllers;

/// <summary>
/// 文件交互接口
/// </summary>
[ApiController]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class AssetsController : ControllerBase
{
    private static readonly Logger logger = LogManager.GetLogger("AssetsController");
    private readonly IFileRepository fileRepository;
    private readonly IConfiguration configuration;

    public AssetsController(IFileRepository _fileRepository, IConfiguration _configuration)
    {
        fileRepository = _fileRepository;
        configuration = _configuration;
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
    [HttpGet("[controller]/{hash}/{filename}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public IActionResult GetFile(string hash, string filename)
    {
        if(hash.Length != 64 || filename.Length == 0)
            return BadRequest(new RequestResponse("请求非法"));

        var path = $"{hash[..2]}/{hash[2..4]}/{hash}";
        var basepath = configuration.GetSection("UploadFolder").Value ?? "uploads";
        path = Path.GetFullPath(Path.Combine(basepath, path));

        if (!System.IO.File.Exists(path))
        {
            LogHelper.Log(logger, $"尝试获取不存在的文件 [{hash[..8]}] {filename}", HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "0.0.0.0", TaskStatus.NotFound, NLog.LogLevel.Warn);
            return NotFound(new RequestResponse("文件不存在", 404));
        }

        return new PhysicalFileResult(path, MediaTypeNames.Application.Octet)
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
    /// <param name="token"></param>
    /// <response code="200">成功上传文件</response>
    /// <response code="400">上传文件失败</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">无权访问</response>
    [RequireAdmin]
    [HttpPost("api/[controller]")]
    [ProducesResponseType(typeof(List<LocalFile>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(List<IFormFile> files, CancellationToken token)
    {
        try
        {
            List<LocalFile> results = new();
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var res = await fileRepository.CreateOrUpdateFile(file, null, token);
                    LogHelper.SystemLog(logger, $"更新文件 [{res.Hash[..8]}] {file.FileName} - {file.Length} Bytes", TaskStatus.Success, NLog.LogLevel.Info); 
                    results.Add(res);
                }
            }
            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
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
    [HttpDelete("api/[controller]/{hash}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(string hash, CancellationToken token)
    {
        var result = await fileRepository.DeleteFileByHash(hash, token);

        LogHelper.SystemLog(logger, $"删除文件 [{hash[..8]}]...", result, NLog.LogLevel.Info);

        return result switch
        {
            TaskStatus.Success => Ok(),
            TaskStatus.NotFound => NotFound(),
            _ => BadRequest(new RequestResponse("文件删除失败"))
        };
    }
}
