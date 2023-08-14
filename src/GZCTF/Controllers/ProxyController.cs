
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
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

        if (!await IncrementConnectionCount(id, token))
            return BadRequest(new RequestResponse("容器连接数已达上限"));

        var socket = new Socket(AddressFamily.Unspecified, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(container.IP, container.Port);

        if (!socket.Connected)
            return BadRequest(new RequestResponse("容器连接失败"));

        var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var stream = new NetworkStream(socket);

        try
        {
            _logger.SystemLog($"[{id}] {HttpContext.Connection.RemoteIpAddress} -> {container.IP}:{container.Port}", TaskStatus.Pending, LogLevel.Debug);
            await RunProxy(stream, token);
        }
        finally
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", token);
            await stream.DisposeAsync();
            await DecrementConnectionCount(id, token);
        }

        return Ok();
    }

    /// <summary>
    /// 采用 websocket 代理 TCP 流量
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    internal async Task RunProxy(NetworkStream stream, CancellationToken token = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(TimeSpan.FromMinutes(30));

        var ct = cts.Token;

        var sender = Task.Run(async () =>
        {
            var buffer = new byte[BUFFER_SIZE];
            while (true)
            {
                var status = await HttpContext.Request.Body.ReadAsync(buffer, ct);
                if (status == 0)
                {
                    cts.Cancel();
                    break;
                }
                await stream.WriteAsync(buffer.AsMemory(0, status), ct);
            }
        }, ct);

        var receiver = Task.Run(async () =>
        {
            var buffer = new byte[BUFFER_SIZE];
            while (true)
            {
                var count = await stream.ReadAsync(buffer, ct);
                if (count == 0)
                {
                    cts.Cancel();
                    break;
                }
                await HttpContext.Response.Body.WriteAsync(buffer.AsMemory(0, count), ct);
            }
        }, ct);

        await Task.WhenAny(sender, receiver);
    }

    /// <summary>
    /// 实现容器 TCP 连接计数的 Fetch-Add 操作
    /// </summary>
    /// <param name="id">容器 id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    internal async Task<bool> IncrementConnectionCount(string id, CancellationToken token = default)
    {
        var key = CacheKey.ConnectionCount(id);
        var bytes = await _cache.GetAsync(key);
        var count = bytes is null ? 0 : BitConverter.ToUInt32(bytes);

        if (count >= CONNECTION_LIMIT)
            return false;

        await _cache.SetAsync(key, BitConverter.GetBytes(count + 1), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10)
        }, token);

        return true;
    }

    /// <summary>
    /// 实现容器 TCP 连接计数的减少操作
    /// </summary>
    /// <param name="id">容器 id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    internal async Task DecrementConnectionCount(string id, CancellationToken token = default)
    {
        var key = CacheKey.ConnectionCount(id);
        var bytes = await _cache.GetAsync(key);

        if (bytes is null)
            return;

        var count = BitConverter.ToUInt32(bytes);

        if (count <= 1)
        {
            await _cache.RemoveAsync(key, token);
        }
        else
        {
            await _cache.SetAsync(key, BitConverter.GetBytes(count - 1), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10)
            }, token);
        }
    }
}
