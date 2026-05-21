using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Container.Exec;
using Microsoft.AspNetCore.SignalR;

namespace GZCTF.Hubs;

/// <summary>
/// Admin-only bidirectional terminal over a running participant
/// container. Lifecycle is keyed to the SignalR connection id; a
/// disconnect tears down every open exec session for that connection
/// so we don't leak Docker exec instances on tab close.
///
/// <para>The earlier implementation streamed bytes back through
/// <c>IAsyncEnumerable&lt;string&gt;</c>, but the JSON protocol's
/// enumerator lifecycle cancelled the stream immediately after the
/// first yield. This rewrite uses a background pump task that calls
/// <c>Clients.Caller.SendAsync("Receive", ...)</c> for every chunk
/// instead — much simpler, and there's no enumerator to fight with.
/// </para>
/// </summary>
[ExcludeFromCodeCoverage]
public class ContainerExecHub(
    IContainerRepository containerRepository,
    IContainerExecChannel execChannel,
    ILogger<ContainerExecHub> logger) : Hub
{
    static readonly ConcurrentDictionary<string, ConnectionSessions> _byConnection = new();

    public override async Task OnConnectedAsync()
    {
        var ctx = Context.GetHttpContext();
        if (ctx is null || !await ContextHelper.HasAdmin(ctx))
        {
            Context.Abort();
            return;
        }
        _byConnection.TryAdd(Context.ConnectionId, new());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_byConnection.TryRemove(Context.ConnectionId, out var connSessions))
        {
            foreach (var (_, s) in connSessions.Sessions)
            {
                try { s.Cancel.Cancel(); } catch { /* ignore */ }
                try { await s.Session.DisposeAsync(); } catch { /* ignore */ }
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Open a new exec session, start the background pump that pushes
    /// stdout/stderr chunks back to the caller via the "Receive" client
    /// method, and return the session id the client uses for Input /
    /// Resize / Close calls.
    /// </summary>
    public async Task<string> Open(Guid containerGuid, string shell)
    {
        var container = await containerRepository.GetContainerById(containerGuid, default);
        if (container is null)
            throw new HubException("Container not found.");

        IExecSession session;
        try
        {
            var sessionCts = new CancellationTokenSource();
            session = await execChannel.OpenAsync(container, shell ?? "sh", sessionCts.Token);
            var sessionId = Guid.NewGuid().ToString("N");
            if (!_byConnection.TryGetValue(Context.ConnectionId, out var conn))
            {
                await session.DisposeAsync();
                throw new HubException("Session bag missing — reconnect.");
            }
            conn.Sessions[sessionId] = new SessionEntry(session, sessionCts);

            // Capture caller proxy: SignalR allows holding this ref past
            // the hub method return as long as the connection is alive.
            var caller = Clients.Caller;
            _ = Task.Run(() => PumpAsync(session, sessionId, caller, sessionCts.Token, logger));

            // Sentinel so the client can verify the channel is actually
            // alive end-to-end before the user types anything. Sent via
            // the same Receive channel so any encoding bug is visible
            // immediately.
            await caller.SendAsync("Receive", sessionId,
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                    $"[gzctf] connected to {container.ContainerId[..Math.Min(12, container.ContainerId.Length)]} ({shell ?? "sh"})\r\n")));

            return sessionId;
        }
        catch (NotSupportedException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (HubException) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContainerExecHub: open failed");
            throw new HubException($"Could not open exec: {ex.Message}");
        }
    }

    static async Task PumpAsync(
        IExecSession session, string sessionId, IClientProxy caller,
        CancellationToken token, ILogger logger)
    {
        var buf = new byte[4096];
        try
        {
            while (!token.IsCancellationRequested)
            {
                int n;
                try { n = await session.ReadAsync(buf, token); }
                catch (OperationCanceledException) { return; }

                if (n == 0)
                {
                    await SafeSend(caller, "Closed", sessionId, "eof");
                    return;
                }
                var b64 = Convert.ToBase64String(buf, 0, n);
                try
                {
                    await caller.SendAsync("Receive", sessionId, b64, token);
                }
                catch (OperationCanceledException) { return; }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "ContainerExecHub: failed to send chunk for {Sid}", sessionId);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContainerExecHub: pump crashed for {Sid}", sessionId);
            await SafeSend(caller, "Closed", sessionId, ex.Message);
        }
    }

    static async Task SafeSend(IClientProxy caller, string method, string sessionId, string payload)
    {
        try { await caller.SendAsync(method, sessionId, payload, CancellationToken.None); }
        catch { /* connection already gone */ }
    }

    /// <summary>
    /// Client → server. <paramref name="chunk"/> is base64-encoded so
    /// the SignalR JSON protocol doesn't have to muck with byte[]
    /// argument binding.
    /// </summary>
    public async Task Input(string sessionId, string chunk)
    {
        if (string.IsNullOrEmpty(chunk)) return;
        byte[] bytes;
        try { bytes = Convert.FromBase64String(chunk); }
        catch (FormatException) { return; }
        if (_byConnection.TryGetValue(Context.ConnectionId, out var conn) &&
            conn.Sessions.TryGetValue(sessionId, out var entry))
        {
            try { await entry.Session.WriteAsync(bytes, entry.Cancel.Token); }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "ContainerExecHub: write error on {Sid}", sessionId);
            }
        }
    }

    public Task Resize(string sessionId, uint cols, uint rows)
    {
        if (_byConnection.TryGetValue(Context.ConnectionId, out var conn) &&
            conn.Sessions.TryGetValue(sessionId, out var entry))
        {
            return entry.Session.ResizeAsync(cols, rows, entry.Cancel.Token);
        }
        return Task.CompletedTask;
    }

    public async Task Close(string sessionId)
    {
        if (_byConnection.TryGetValue(Context.ConnectionId, out var conn) &&
            conn.Sessions.TryRemove(sessionId, out var entry))
        {
            try { entry.Cancel.Cancel(); } catch { /* ignore */ }
            try { await entry.Session.DisposeAsync(); } catch { /* ignore */ }
        }
    }

    sealed class ConnectionSessions
    {
        public ConcurrentDictionary<string, SessionEntry> Sessions { get; } = new();
    }

    sealed record SessionEntry(IExecSession Session, CancellationTokenSource Cancel);
}
