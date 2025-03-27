using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentStorage.Blobs;
using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
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
public class ProxyController(
    ILogger<ProxyController> logger,
    IDistributedCache cache,
    IBlobStorage storage,
    IOptions<ContainerProvider> provider,
    IContainerRepository containerRepository,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    const int BufferSize = 1024 * 4;
    const uint ConnectionLimit = 64;

    static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            TypeInfoResolver = new AppJsonSerializerContext()
        };

    static readonly DistributedCacheEntryOptions StoreOption =
        new() { SlidingExpiration = TimeSpan.FromHours(10) };

    static readonly DistributedCacheEntryOptions ValidOption =
        new() { SlidingExpiration = TimeSpan.FromMinutes(10) };

    readonly bool _enablePlatformProxy = provider.Value.PortMappingType == ContainerPortMappingType.PlatformProxy;
    readonly bool _enableTrafficCapture = provider.Value.EnableTrafficCapture;

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

        var key = CacheKey.ConnectionCount(id);

        if (!HttpContext.WebSockets.IsWebSocketRequest)
            return NoContent();

        if (!await IncrementConnectionCount(key))
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Container_ConnectionLimitExceeded)]));

        var container = await containerRepository.GetContainerWithInstanceById(id, token);

        if (container is null || !container.IsProxy)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Container_NotFound)],
                StatusCodes.Status404NotFound));

        var ipAddress = (await Dns.GetHostAddressesAsync(container.IP, token)).FirstOrDefault();

        if (ipAddress is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Container_AddressResolveFailed)]));

        var clientIp = HttpContext.Connection.RemoteIpAddress;
        var clientPort = HttpContext.Connection.RemotePort;

        if (clientIp is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Container_InvalidClientAddress)]));

        var enable = _enableTrafficCapture && container.EnableTrafficCapture;

        var metadata = enable ? container.GenerateMetadata(JsonOptions) : null;

        IPEndPoint client = new(clientIp, clientPort);
        IPEndPoint target = new(ipAddress, container.Port);

        return await DoContainerProxy(id, client, target, metadata,
            new()
            {
                Source = client,
                Dest = target,
                EnableCapture = enable,
                BlobPath = container.TrafficPath(HttpContext.Connection.Id)
            }, token);
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

        var clientIp = HttpContext.Connection.RemoteIpAddress;
        var clientPort = HttpContext.Connection.RemotePort;

        if (clientIp is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Container_InvalidClientAddress)]));

        IPEndPoint client = new(clientIp, clientPort);
        IPEndPoint target = new(ipAddress, container.Port);

        return await DoContainerProxy(id, client, target, null, new(), token);
    }

    async Task<IActionResult> DoContainerProxy(Guid id, IPEndPoint client, IPEndPoint target,
        byte[]? metadata, RecordableNetworkStreamOptions options, CancellationToken token = default)
    {
        using var socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        RecordableNetworkStream? stream = null;

        try
        {
            try
            {
                await socket.ConnectAsync(target, token);

                if (!socket.Connected)
                    throw new SocketException((int)SocketError.NotConnected);

                stream = new RecordableNetworkStream(socket, metadata, storage, options);
            }
            catch (SocketException e)
            {
                logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.Proxy_ContainerConnectionFailedLog),
                        e.SocketErrorCode,
                        $"{target.Address}:{target.Port}"],
                    TaskStatus.Failed, LogLevel.Debug);
                return new JsonResult(new RequestResponse(
                    localizer[nameof(Resources.Program.Proxy_ContainerConnectionFailed), e.SocketErrorCode],
                    StatusCodes.Status418ImATeapot))
                { StatusCode = StatusCodes.Status418ImATeapot };
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
            finally
            {
                await DecrementConnectionCount(CacheKey.ConnectionCount(id));
            }

            return new EmptyResult();
        }
        finally
        {
            if (stream is not null)
                await stream.DisposeAsync();
        }
    }

    void LogProxyResult(Guid id, IPEndPoint client, IPEndPoint target, ulong tx, ulong rx)
    {
        var shortId = id.ToString("N")[..8];
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
    static async Task<(ulong, ulong)> RunProxy(RecordableNetworkStream stream, WebSocket ws,
        CancellationToken token = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(TimeSpan.FromMinutes(30));

        var ct = cts.Token;
        ulong tx = 0, rx = 0;

        var sender = Task.Run(async () =>
        {
            var buffer = new byte[BufferSize];
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
                    await stream.WriteAsync(buffer.AsMemory(0, status.Count), ct);
                }
            }
            catch (TaskCanceledException) { }
            finally { await cts.CancelAsync(); }
        }, ct);

        var receiver = Task.Run(async () =>
        {
            var buffer = new byte[BufferSize];
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
                    await ws.SendAsync(
                        buffer.AsMemory(0, count), WebSocketMessageType.Binary, true, ct);
                }
            }
            catch (TaskCanceledException) { }
            finally { await cts.CancelAsync(); }
        }, ct);

        await Task.WhenAny(sender, receiver);

        return (tx, rx);
    }

    /// <summary>
    /// Validate container existence
    /// </summary>
    /// <param name="id">Container ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    async Task<bool> ValidateContainer(Guid id, CancellationToken token = default)
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
    /// Implement Fetch-Add operation for container TCP connection count
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns></returns>
    async Task<bool> IncrementConnectionCount(string key)
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
    /// Implement decrement operation for container TCP connection count
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns></returns>
    async Task DecrementConnectionCount(string key)
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
