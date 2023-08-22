using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace GZCTF.Controllers;

/// <summary>
/// 容器 TCP 流量代理、记录
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProxyController : ControllerBase
{
    private readonly ILogger<ProxyController> _logger;
    private readonly IDistributedCache _cache;
    private readonly IContainerRepository _containerRepository;

    private readonly bool _enablePlatformProxy = false;
    private readonly bool _enableTrafficCapture = false;
    private const int BUFFER_SIZE = 1024 * 4;
    private const uint CONNECTION_LIMIT = 64;
    private readonly JsonSerializerOptions _JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    public ProxyController(ILogger<ProxyController> logger, IDistributedCache cache,
        IOptions<ContainerProvider> provider, IContainerRepository containerRepository)
    {
        _cache = cache;
        _logger = logger;
        _enablePlatformProxy = provider.Value.PortMappingType == ContainerPortMappingType.PlatformProxy;
        _enableTrafficCapture = provider.Value.EnableTrafficCapture;
        _containerRepository = containerRepository;
    }

    /// <summary>
    /// 采用 websocket 代理 TCP 流量
    /// </summary>
    /// <param name="id">容器 id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status418ImATeapot)]
    public async Task<IActionResult> ProxyForInstance(string id, CancellationToken token = default)
    {
        if (!_enablePlatformProxy)
            return BadRequest(new RequestResponse("TCP 代理已禁用"));

        if (!await ValidateContainer(id, token))
            return NotFound(new RequestResponse("不存在的容器"));

        if (!HttpContext.WebSockets.IsWebSocketRequest)
            return NoContent();

        if (!await IncrementConnectionCount(id))
            return BadRequest(new RequestResponse("容器连接数已达上限"));

        var container = await _containerRepository.GetContainerWithInstanceById(id, token);

        if (container is null || container.Instance is null || !container.IsProxy)
            return NotFound(new RequestResponse("不存在的容器"));

        var ipAddress = (await Dns.GetHostAddressesAsync(container.IP, token)).FirstOrDefault();

        if (ipAddress is null)
            return BadRequest(new RequestResponse("容器地址解析失败"));

        var clientIp = HttpContext.Connection.RemoteIpAddress;
        var clientPort = HttpContext.Connection.RemotePort;

        if (clientIp is null)
            return BadRequest(new RequestResponse("无效的访问地址"));

        var enable = _enableTrafficCapture && container.Instance.Challenge.EnableTrafficCapture;
        byte[]? metadata = null;

        if (enable)
        {
            metadata = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Challenge = container.Instance.Challenge.Title,
                container.Instance.ChallengeId,

                Team = container.Instance.Participation.Team.Name,
                container.Instance.Participation.TeamId,

                container.ContainerId,
                container.Instance.FlagContext?.Flag
            }, _JsonOptions);
        }

        IPEndPoint client = new(clientIp, clientPort);
        IPEndPoint target = new(ipAddress, container.Port);

        return await DoContainerProxy(id, client, target, metadata, new()
        {
            Source = client,
            Dest = target,
            EnableCapture = enable,
            FilePath = container.TrafficPath(HttpContext.Connection.Id),
        }, token);
    }

    /// <summary>
    /// 采用 websocket 代理 TCP 流量，为测试容器使用
    /// </summary>
    /// <param name="id">测试容器 id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [Route("NoInst/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status418ImATeapot)]
    public async Task<IActionResult> ProxyForNoInstance(string id, CancellationToken token = default)
    {
        if (!_enablePlatformProxy)
            return BadRequest(new RequestResponse("TCP 代理已禁用"));

        if (!await ValidateContainer(id, token))
            return NotFound(new RequestResponse("不存在的容器"));

        if (!HttpContext.WebSockets.IsWebSocketRequest)
            return NoContent();

        var container = await _containerRepository.GetContainerById(id, token);

        if (container is null || container.InstanceId != 0 || !container.IsProxy)
            return NotFound(new RequestResponse("不存在的容器"));

        var ipAddress = (await Dns.GetHostAddressesAsync(container.IP, token)).FirstOrDefault();

        if (ipAddress is null)
            return BadRequest(new RequestResponse("容器地址解析失败"));

        var clientIp = HttpContext.Connection.RemoteIpAddress;
        var clientPort = HttpContext.Connection.RemotePort;

        if (clientIp is null)
            return BadRequest(new RequestResponse("无效的访问地址"));

        IPEndPoint client = new(clientIp, clientPort);
        IPEndPoint target = new(ipAddress, container.Port);

        return await DoContainerProxy(id, client, target, null, new(), token);
    }

    internal async Task<IActionResult> DoContainerProxy(string id, IPEndPoint client, IPEndPoint target,
        byte[]? metadata, CapturableNetworkStreamOptions options, CancellationToken token = default)
    {
        using var socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        CapturableNetworkStream? stream;
        try
        {
            await socket.ConnectAsync(target, token);

            if (!socket.Connected)
                throw new SocketException((int)SocketError.NotConnected);

            stream = new CapturableNetworkStream(socket, metadata, options);
        }
        catch (SocketException e)
        {
            _logger.SystemLog($"容器连接失败（{e.SocketErrorCode}），可能正在启动中或请检查网络配置 -> {target.Address}:{target.Port}", TaskStatus.Failed, LogLevel.Warning);
            return new JsonResult(new RequestResponse($"容器连接失败（{e.SocketErrorCode}）", 418)) { StatusCode = 418 };
        }

        using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

        try
        {
            var (tx, rx) = await RunProxy(stream, ws, token);
            _logger.SystemLog($"[{id}] {client.Address} -> {target.Address}:{target.Port}, tx {tx}, rx {rx}", TaskStatus.Success, LogLevel.Debug);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "代理过程发生错误");
        }
        finally
        {
            await DecrementConnectionCount(id);
            stream.Close();
        }

        return new EmptyResult();
    }

    /// <summary>
    /// 采用 websocket 代理 TCP 流量
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="ws"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    internal static async Task<(ulong, ulong)> RunProxy(CapturableNetworkStream stream, WebSocket ws, CancellationToken token = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(TimeSpan.FromMinutes(30));

        var ct = cts.Token;
        ulong tx = 0, rx = 0;

        var sender = Task.Run(async () =>
        {
            var buffer = new byte[BUFFER_SIZE];
            try
            {
                while (true)
                {
                    var status = await ws.ReceiveAsync(buffer, ct);
                    if (status.CloseStatus.HasValue)
                        break;
                    if (status.Count > 0)
                    {
                        tx += (ulong)status.Count;
                        await stream.WriteAsync(buffer.AsMemory(0, status.Count), ct);
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception) { throw; }
            finally { cts.Cancel(); }
        }, ct);

        var receiver = Task.Run(async () =>
        {
            var buffer = new byte[BUFFER_SIZE];
            try
            {
                while (true)
                {
                    var count = await stream.ReadAsync(buffer, ct);
                    if (count == 0)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.Empty, null, token);
                        break;
                    }
                    rx += (ulong)count;
                    await ws.SendAsync(buffer.AsMemory(0, count), WebSocketMessageType.Binary, true, ct);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception) { throw; }
            finally { cts.Cancel(); }
        }, ct);

        await Task.WhenAny(sender, receiver);

        return (tx, rx);
    }

    private readonly DistributedCacheEntryOptions _validOption = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    private readonly DistributedCacheEntryOptions _storeOption = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10)
    };

    /// <summary>
    /// 容器存在性校验
    /// </summary>
    /// <param name="id">容器 id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    internal async Task<bool> ValidateContainer(string id, CancellationToken token = default)
    {
        var key = CacheKey.ConnectionCount(id);
        var bytes = await _cache.GetAsync(key, token);

        // avoid DoS attack with cache -1
        if (bytes is not null)
            return BitConverter.ToInt32(bytes) >= 0;

        var valid = await _containerRepository.ValidateContainer(id, token);

        await _cache.SetAsync(key, BitConverter.GetBytes(valid ? 0 : -1), _validOption, token);

        return valid;
    }

    /// <summary>
    /// 实现容器 TCP 连接计数的 Fetch-Add 操作
    /// </summary>
    /// <param name="id">容器 id</param>
    /// <returns></returns>
    internal async Task<bool> IncrementConnectionCount(string id)
    {
        var key = CacheKey.ConnectionCount(id);
        var bytes = await _cache.GetAsync(key);

        if (bytes is null)
            return false;

        var count = BitConverter.ToInt32(bytes);

        if (count > CONNECTION_LIMIT)
            return false;

        await _cache.SetAsync(key, BitConverter.GetBytes(count + 1), _storeOption);

        return true;
    }

    /// <summary>
    /// 实现容器 TCP 连接计数的减少操作
    /// </summary>
    /// <param name="id">容器 id</param>
    /// <returns></returns>
    internal async Task DecrementConnectionCount(string id)
    {
        var key = CacheKey.ConnectionCount(id);
        var bytes = await _cache.GetAsync(key);

        if (bytes is null)
            return;

        var count = BitConverter.ToInt32(bytes);

        if (count > 1)
        {
            await _cache.SetAsync(key, BitConverter.GetBytes(count - 1), _storeOption);
        }
        else
        {
            await _cache.SetAsync(key, BitConverter.GetBytes(0), _validOption);
        }
    }
}
