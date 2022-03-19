using Microsoft.AspNetCore.Mvc;
using CTFServer.Utils;
using NLog;
using System.Net.Mime;
using CTFServer.Repositories.Interface;
using CTFServer.Middlewares;
using System.Net.Http.Headers;

namespace CTFServer.Controllers;

/// <summary>
/// 文件交互接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
    /// <param name="token"></param>
    /// <response code="200">成功获取文件</response>
    /// <response code="404">文件未找到</response>
    [HttpGet("{hash}/{filename}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetFile(string hash, string filename, CancellationToken token)
    {
        var path = $"{hash[..2]}/{hash[2..4]}/{hash}";
        var basepath = configuration.GetSection("UploadFolder").Value ?? "files";
        path = Path.GetFullPath(Path.Combine(basepath, path));

        if (!System.IO.File.Exists(path))
            return NotFound();

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
    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(List<IFormFile> files, CancellationToken token)
    {
        try
        {
            foreach (var file in files)
                if (file.Length > 0)
                    await fileRepository.CreateOrUpdateFile(file, token);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            return BadRequest(new RequestResponse("遇到IO错误"));
        }

        return Ok();
    }

    /// <summary>
    /// 上传文件接口
    /// </summary>
    /// <remarks>
    /// 按照文件哈希删除文件
    /// </remarks>
    /// <param name="hash"></param>
    /// <param name="token"></param>
    /// <response code="200">成功上传文件</response>
    /// <response code="400">上传文件失败</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">无权访问</response>
    [HttpDelete("{hash}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(string hash, CancellationToken token)
    {
        var result = await fileRepository.DeleteFileByHash(hash, token);

        return result switch
        {
            TaskStatus.Success => Ok(),
            TaskStatus.NotFound => NotFound(),
            _ => BadRequest(new RequestResponse("文件删除失败"))
        };
    }
}
