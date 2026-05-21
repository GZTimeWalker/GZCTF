using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Container.Exec;
using Microsoft.AspNetCore.SignalR;

namespace GZCTF.Hubs;

/// <summary>
/// Admin-only bidirectional terminal over a running participant
/// container. Lifecycle is keyed to the SignalR connection id; a
/// disconnect tears down every open exec session for that
/// connection so we don't leak Docker exec instances on tab close.
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
    /// Open a new exec session against the container identified by
    /// <paramref name="containerGuid"/>. Returns an opaque session id the
    /// client passes back to <see cref="Stream"/> / <see cref="Input"/> /
    /// <see cref="Resize"/> / <see cref="Close"/>.
    /// </summary>
    public async Task<string> Open(Guid containerGuid, string shell)
    {
        var container = await containerRepository.GetContainerById(containerGuid, default);
        if (container is null)
            throw new HubException("Container not found.");

        try
        {
            var cts = new CancellationTokenSource();
            var session = await execChannel.OpenAsync(container, shell ?? "sh", cts.Token);
            var sessionId = Guid.NewGuid().ToString("N");
            if (_byConnection.TryGetValue(Context.ConnectionId, out var conn))
            {
                conn.Sessions[sessionId] = new SessionEntry(session, cts);
            }
            else
            {
                // Shouldn't happen if OnConnected was reached, but be safe.
                await session.DisposeAsync();
                throw new HubException("Session bag missing — reconnect.");
            }
            logger.LogInformation("ContainerExecHub: opened session {Sid} for {Container}", sessionId, container.LogId);
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

    /// <summary>
    /// Server → client byte stream. Each yielded chunk is forwarded
    /// straight to the browser's xterm <c>term.write</c>.
    /// </summary>
    public async IAsyncEnumerable<byte[]> Stream(string sessionId, [EnumeratorCancellation] CancellationToken token)
    {
        if (!_byConnection.TryGetValue(Context.ConnectionId, out var conn) ||
            !conn.Sessions.TryGetValue(sessionId, out var entry))
            yield break;

        var buf = new byte[4096];
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, entry.Cancel.Token);
        while (!linked.IsCancellationRequested)
        {
            int n;
            try
            {
                n = await entry.Session.ReadAsync(buf, linked.Token);
            }
            catch (OperationCanceledException) { yield break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "ContainerExecHub: read error on {Sid}", sessionId);
                yield break;
            }
            if (n == 0) yield break;
            yield return buf[..n];
        }
    }

    public async Task Input(string sessionId, byte[] chunk)
    {
        if (chunk is null || chunk.Length == 0) return;
        if (_byConnection.TryGetValue(Context.ConnectionId, out var conn) &&
            conn.Sessions.TryGetValue(sessionId, out var entry))
        {
            try { await entry.Session.WriteAsync(chunk, entry.Cancel.Token); }
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
