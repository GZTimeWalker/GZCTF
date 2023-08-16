using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
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

        var container = await _containerRepository.GetContainerById(id, token);

        if (container is null || container.Instance is null || !container.IsProxy)
            return NotFound(new RequestResponse("不存在的容器"));

        var ipAddress = (await Dns.GetHostAddressesAsync(container.IP, token)).FirstOrDefault();

        if (ipAddress is null)
            return BadRequest(new RequestResponse("容器地址解析失败"));

        var clientIp = HttpContext.Connection.RemoteIpAddress;
        var clientPort = HttpContext.Connection.RemotePort;

        if (clientIp is null)
            return BadRequest(new RequestResponse("无效的访问地址"));

        CapturableNetworkStream? stream;
        try
        {
            IPEndPoint ipEndPoint = new(ipAddress, container.Port);
            using var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(ipEndPoint, token);

            if (!socket.Connected)
            {
                _logger.SystemLog($"容器连接失败，请检查网络配置 -> {container.IP}:{container.Port}", TaskStatus.Failed, LogLevel.Warning);
                return BadRequest(new RequestResponse("容器连接失败"));
            }

            stream = new CapturableNetworkStream(socket, new()
            {
                Source = new(clientIp, clientPort),
                Dest = ipEndPoint,
                EnableCapture = _enableTrafficCapture && container.Instance.Challenge.EnableTrafficCapture,
                FilePath = container.TrafficPath(HttpContext.Connection.Id),
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "容器连接失败");
            return BadRequest(new RequestResponse("容器连接失败"));
        }

        var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

        try
        {
            var (tx, rx) = await RunProxy(stream, ws, token);
            _logger.SystemLog($"[{id}] {clientIp} -> {container.IP}:{container.Port}, tx {tx}, rx {rx}", TaskStatus.Success, LogLevel.Debug);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "代理错误");
        }
        finally
        {
            await DecrementConnectionCount(id);
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
                    {
                        break;
                    }
                    if (status.Count > 0)
                    {
                        tx += (ulong)status.Count;
                        await stream.WriteAsync(buffer.AsMemory(0, status.Count), ct);
                    }
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                stream.Close();
                cts.Cancel();
            }
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
                        stream.Close();
                        cts.Cancel();
                        break;
                    }
                    rx += (ulong)count;
                    await ws.SendAsync(buffer.AsMemory(0, count), WebSocketMessageType.Binary, true, ct);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                stream.Close();
                cts.Cancel();
            }
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

        if (bytes is not null)
            return true;

        var valid = await _containerRepository.ValidateContainer(id, token);

        if (valid)
            await _cache.SetAsync(key, BitConverter.GetBytes(0), _validOption, token);

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

        var count = BitConverter.ToUInt32(bytes);

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

        var count = BitConverter.ToUInt32(bytes);

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
