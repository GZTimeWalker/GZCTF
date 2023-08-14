using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using PacketDotNet;
using ProtocolType = System.Net.Sockets.ProtocolType;

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
    private const int BUFFER_SIZE = 1024 * 4;
    private const uint CONNECTION_LIMIT = 128;

    public ProxyController(ILogger<ProxyController> logger, IDistributedCache cache,
        IOptions<ContainerProvider> provider, IContainerRepository containerRepository)
    {
        _cache = cache;
        _logger = logger;
        _enablePlatformProxy = provider.Value.PortMappingType == ContainerPortMappingType.PlatformProxy;
        _containerRepository = containerRepository;
    }

    /// <summary>
    /// 采用 websocket 代理 TCP 流量
    /// </summary>
    /// <param name="id">容器 id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [Route("{id:length(36)}")]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProxyForInstance([RegularExpression(@"[0-9a-f\-]{36}")] string id, CancellationToken token = default)
    {
        if (!_enablePlatformProxy)
            return BadRequest(new RequestResponse("TCP 代理已禁用"));

        if (!HttpContext.WebSockets.IsWebSocketRequest)
            return BadRequest(new RequestResponse("仅支持 Websocket 请求"));

        var container = await _containerRepository.GetContainerById(id, token);

        if (container is null || !container.IsProxy)
            return NotFound(new RequestResponse("不存在的容器"));

        if (!await IncrementConnectionCount(id))
            return BadRequest(new RequestResponse("容器连接数已达上限"));

        var ipAddress = (await Dns.GetHostAddressesAsync(container.IP, token)).FirstOrDefault();

        if (ipAddress is null)
            return BadRequest(new RequestResponse("容器地址解析失败"));

        NetworkStream? stream;
        try
        {
            IPEndPoint ipEndPoint = new(ipAddress, container.Port);
            var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(ipEndPoint, token);

            if (!socket.Connected)
                return BadRequest(new RequestResponse("容器连接失败"));

            stream = new NetworkStream(socket);
        }
        catch
        {
            return BadRequest(new RequestResponse("容器连接失败"));
        }

        var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var clientIp = HttpContext.Connection.RemoteIpAddress;
        var clientPort = HttpContext.Connection.RemotePort;

        _logger.SystemLog($"[{id}] {clientIp}:{clientPort} -> {container.IP}:{container.Port}", TaskStatus.Pending, LogLevel.Debug);

        try
        {
            var (tx, rx) = await RunProxy(stream, ws, token);
            _logger.SystemLog($"[{id}] tx {tx}, rx {rx}", TaskStatus.Success, LogLevel.Debug);
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
    internal static async Task<(ulong, ulong)> RunProxy(NetworkStream stream, WebSocket ws, CancellationToken token = default)
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
                        await stream.DisposeAsync();
                        cts.Cancel();
                        break;
                    }
                    tx += (ulong)status.Count;
                    await stream.WriteAsync(buffer.AsMemory(0, status.Count), ct);
                }
            }
            catch (TaskCanceledException) { }
            catch { cts.Cancel(); }
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
                        cts.Cancel();
                        break;
                    }
                    rx += (ulong)count;
                    await ws.SendAsync(buffer.AsMemory(0, count), WebSocketMessageType.Binary, true, ct);
                }
            }
            catch (TaskCanceledException) { }
            catch { cts.Cancel(); }
        }, ct);

        await Task.WhenAny(sender, receiver);

        return (tx, rx);
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
        var count = bytes is null ? 0 : BitConverter.ToUInt32(bytes);

        if (count >= CONNECTION_LIMIT)
            return false;

        await _cache.SetAsync(key, BitConverter.GetBytes(count + 1), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10)
        });

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

        if (count <= 1)
        {
            await _cache.RemoveAsync(key);
        }
        else
        {
            await _cache.SetAsync(key, BitConverter.GetBytes(count - 1), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10)
            });
        }
    }
}
