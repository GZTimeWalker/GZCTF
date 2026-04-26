using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Traffic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Controllers;

/// <summary>
/// Container TCP traffic proxy and logging APIs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ExcludeFromCodeCoverage]
public class ProxyController(
    ILogger<ProxyController> logger,
    IDistributedCache cache,
    IOptions<ContainerProvider> provider,
    IContainerRepository containerRepository,
    TrafficRecorderRegistry trafficRegistry,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    private const int BufferSize = 4096;
    private const uint ConnectionLimit = 32;

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            TypeInfoResolver = new AppJsonSerializerContext()
        };

    private static readonly DistributedCacheEntryOptions StoreOption =
        new() { SlidingExpiration = TimeSpan.FromHours(10) };

    private static readonly DistributedCacheEntryOptions ValidOption =
        new() { SlidingExpiration = TimeSpan.FromMinutes(10) };

    private readonly bool _enablePlatformProxy =
        provider.Value.PortMappingType == ContainerPortMappingType.PlatformProxy;

    private readonly bool _enableTrafficCapture = provider.Value.EnableTrafficCapture;

    /// <summary>
    /// Proxy TCP over websocket
    /// </summary>
    /// <param name="id">Container ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status418ImATeapot)]
    public async Task<IActionResult> ProxyForInstance(Guid id, CancellationToken token = default)
    {
        if (!_enablePlatformProxy)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Proxy_TcpDisabled)]));

        if (!await ValidateContainer(id, token))
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Container_NotFound)],
                StatusCodes.Status404NotFound));

        if (!HttpContext.WebSockets.IsWebSocketRequest)
            return NoContent();

        var key = CacheKey.ConnectionCount(id);

        if (!await IncreaseConnectionCount(key))
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Container_ConnectionLimitExceeded)]));

        var container = await containerRepository.GetContainerWithInstanceById(id, token);

        if (container is null || !container.IsProxy)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Container_NotFound)],
                StatusCodes.Status404NotFound));

        var ipAddress = (await Dns.GetHostAddressesAsync(container.IP, token)).FirstOrDefault();

        if (ipAddress is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Container_AddressResolveFailed)]));

        var clientIp = HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback;
        var clientPort = HttpContext.Connection.RemotePort;

        var enable = _enableTrafficCapture && container.EnableTrafficCapture;

        IPEndPoint client = new(clientIp, clientPort);
        IPEndPoint target = new(ipAddress, container.Port);

        TrafficWriter? writer = null;
        if (enable)
        {
            var descriptor = new TrafficRecorderDescriptor(
                ContainerId: id,
                ChallengeId: container.GameInstance!.ChallengeId,
                ParticipationId: container.GameInstance!.ParticipationId,
                ConnectionId: HttpContext.Connection.Id,
                Metadata: container.GenerateMetadata(JsonOptions),
                ClientEndpoint: client);
            writer = trafficRegistry.AcquireWriter(descriptor);
        }

        return await DoContainerProxy(id, client, target, writer, token);
    }

    /// <summary>
    /// Proxy TCP over websocket for admins
    /// </summary>
    /// <param name="id">Test container ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [Route("NoInst/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status418ImATeapot)]
    [SuppressMessage("ReSharper", "RouteTemplates.ParameterTypeCanBeMadeStricter")]
    public async Task<IActionResult> ProxyForNoInstance(Guid id, CancellationToken token = default)
    {
        if (!_enablePlatformProxy)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Proxy_TcpDisabled)]));

        if (!await ValidateContainer(id, token))
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Container_NotFound)],
                StatusCodes.Status404NotFound));

        if (!HttpContext.WebSockets.IsWebSocketRequest)
            return NoContent();

        var container = await containerRepository.GetContainerById(id, token);

        if (container is null || container.GameInstanceId is not null || !container.IsProxy)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Container_NotFound)],
                StatusCodes.Status404NotFound));

        var ipAddress = (await Dns.GetHostAddressesAsync(container.IP, token)).FirstOrDefault();

        if (ipAddress is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Container_AddressResolveFailed)]));

        var clientIp = HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback;
        var clientPort = HttpContext.Connection.RemotePort;

        IPEndPoint client = new(clientIp, clientPort);
        IPEndPoint target = new(ipAddress, container.Port);

        return await DoContainerProxy(id, client, target, null, token);
    }

    private async Task<IActionResult> DoContainerProxy(Guid id, IPEndPoint client, IPEndPoint target,
        TrafficWriter? writer, CancellationToken token = default)
    {
        using var socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        CaptureNetworkStream? stream = null;

        try
        {
            try
            {
                await socket.ConnectAsync(target, token);

                if (!socket.Connected)
                    throw new SocketException((int)SocketError.NotConnected);

                stream = new CaptureNetworkStream(socket, writer, client, target);
            }
            catch (SocketException e)
            {
                logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.Proxy_ContainerConnectionFailedLog),
                        e.SocketErrorCode,
                        $"{target.Address}:{target.Port}"],
                    TaskStatus.Failed, LogLevel.Debug);

                return RequestResponse.Result(
                    localizer[nameof(Resources.Program.Proxy_ContainerConnectionFailed), e.SocketErrorCode],
                    StatusCodes.Status418ImATeapot);
            }

            using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

            try
            {
                var (tx, rx) = await RunProxy(stream, ws, token);
                LogProxyResult(id, client, target, tx, rx);
            }
            catch (Exception e)
            {
                logger.LogErrorMessage(e, StaticLocalizer[nameof(Resources.Program.Proxy_Error)]);
            }
        }
        finally
        {
            if (stream is not null)
                await stream.DisposeAsync();
            else if (writer is not null)
                await writer.DisposeAsync();

            await DecreaseConnectionCount(CacheKey.ConnectionCount(id));
        }

        return new EmptyResult();
    }

    private void LogProxyResult(Guid id, IPEndPoint client, IPEndPoint target, ulong tx, ulong rx)
    {
        var shortId = id.ToString("N")[..12];
        var clientAddress = client.Address.IsIPv4MappedToIPv6 ? client.Address.MapToIPv4() : client.Address;
        var targetAddress = target.Address.IsIPv4MappedToIPv6 ? target.Address.MapToIPv4() : target.Address;

        logger.SystemLog($"[{shortId}] {clientAddress} -> {targetAddress}:{target.Port}, tx {tx}, rx {rx}",
            TaskStatus.Success, LogLevel.Debug);
    }

    /// <summary>
    /// Proxy TCP traffic using websocket
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="ws"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task<(ulong, ulong)> RunProxy(CaptureNetworkStream stream, WebSocket ws,
        CancellationToken token = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

        cts.CancelAfter(TimeSpan.FromMinutes(30));

        var ct = cts.Token;
        ulong tx = 0, rx = 0;

        var sender = Task.Run(async () =>
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                while (true)
                {
                    var status = await ws.ReceiveAsync(buffer, ct);
                    if (status.CloseStatus.HasValue)
                        break;
                    if (status.Count <= 0)
                        continue;

                    tx += (ulong)status.Count;
                    var memory = buffer.AsMemory(0, status.Count);
                    await stream.WriteAsync(memory, ct);
                }
            }
            catch
            {
                // ignore
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }, ct);

        var receiver = Task.Run(async () =>
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                while (true)
                {
                    var count = await stream.ReadAsync(buffer, ct);
                    if (count == 0)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.Empty, null, ct);
                        break;
                    }

                    rx += (ulong)count;
                    var memory = buffer.AsMemory(0, count);
                    await ws.SendAsync(memory, WebSocketMessageType.Binary, true, ct);
                }
            }
            catch
            {
                // ignore
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }, ct);

        await Task.WhenAny(sender, receiver);
        await cts.CancelAsync();
        await Task.WhenAll(sender, receiver);

        return (tx, rx);
    }

    /// <summary>
    /// Validate container existence
    /// </summary>
    /// <param name="id">Container ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task<bool> ValidateContainer(Guid id, CancellationToken token = default)
    {
        var key = CacheKey.ConnectionCount(id);
        var bytes = await cache.GetAsync(key, token);

        // avoid DoS attack with cache -1
        if (bytes is not null)
            return BitConverter.ToInt32(bytes) >= 0;

        var valid = await containerRepository.ValidateContainer(id, token);

        await cache.SetAsync(key, BitConverter.GetBytes(valid ? 0 : -1), ValidOption, token);

        return valid;
    }

    /// <summary>
    /// Increase Fetch-Add operation for container TCP connection count
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns></returns>
    private async Task<bool> IncreaseConnectionCount(string key)
    {
        var bytes = await cache.GetAsync(key);

        if (bytes is null)
            return false;

        var count = BitConverter.ToInt32(bytes);

        if (count > ConnectionLimit)
            return false;

        await cache.SetAsync(key, BitConverter.GetBytes(count + 1), StoreOption);

        return true;
    }

    /// <summary>
    /// Implement decrease operation for container TCP connection count
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns></returns>
    private async Task DecreaseConnectionCount(string key)
    {
        var bytes = await cache.GetAsync(key);

        if (bytes is null)
            return;

        var count = BitConverter.ToInt32(bytes);

        if (count > 1)
            await cache.SetAsync(key, BitConverter.GetBytes(count - 1), StoreOption);
        else
            await cache.SetAsync(key, BitConverter.GetBytes(0), ValidOption);
    }
}
