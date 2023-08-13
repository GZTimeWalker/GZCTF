
using System.Net.Sockets;
using System.Net.WebSockets;
using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IContainerRepository _containerRepository;

    private readonly bool _enablePlatformProxy = false;
    private const int BUFFER_SIZE = 1024 * 4;
    private const int CONNECTION_LIMIT = 128;

    public ProxyController(ILogger<ProxyController> logger, IOptions<ContainerProvider> provider, IContainerRepository containerRepository)
    {
        _logger = logger;
        _enablePlatformProxy = provider.Value.PortMappingType == ContainerPortMappingType.PlatformProxy;
        _containerRepository = containerRepository;
    }

    /// <summary>
    /// 采用 websocket 代理 TCP 流量
    /// </summary>
    /// <returns></returns>
    [Route("/inst/{id}")]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProxyForInstance(string id)
    {
        if (!_enablePlatformProxy)
            return BadRequest(new RequestResponse("TCP 代理已禁用"));

        if (!HttpContext.WebSockets.IsWebSocketRequest)
            return BadRequest(new RequestResponse("仅支持 Websocket 请求"));

        var container = await _containerRepository.GetContainerById(id);

        if (container is null || !container.IsProxy)
            return NotFound(new RequestResponse("不存在的容器"));

        var socket = new Socket(AddressFamily.Unspecified, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(container.IP, container.Port);

        if (!socket.Connected)
            return BadRequest(new RequestResponse("容器连接失败"));

        var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var stream = new NetworkStream(socket);

        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var sender = Task.Run(async () =>
        {
            var buffer = new byte[BUFFER_SIZE];
            while (true)
            {
                var status = await ws.ReceiveAsync(buffer, ct);
                if (status.Count == 0)
                {
                    cts.Cancel();
                    break;
                }
                await stream.WriteAsync(buffer.AsMemory(0, status.Count), ct);
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
                await ws.SendAsync(new ArraySegment<byte>(buffer, 0, count), WebSocketMessageType.Binary, true, ct);
            }
        }, ct);

        await Task.WhenAny(sender, receiver);

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", ct);
        await stream.DisposeAsync();

        return Ok();
    }
}
