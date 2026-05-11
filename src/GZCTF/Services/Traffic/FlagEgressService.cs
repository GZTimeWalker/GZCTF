using System.Collections.Concurrent;
using System.Net;
using System.Text;
using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Admin;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GZCTF.Services.Traffic;

/// <summary>
/// Singleton service that owns per-recorder flag inspectors, aggregates
/// hits in a sliding window, persists events, and broadcasts notices to
/// admin clients.
///
/// Lifecycle:
///   RegisterRecorder is called from TrafficRecorderRegistry.CreateRecorder
///   when a container with a per-team flag starts proxying traffic.
///   UnregisterRecorder is called from TrafficRecorderRegistry.ArchiveAsync
///   when the container's recorder is archived.
/// </summary>
public sealed class FlagEgressService : IAsyncDisposable
{
    readonly IServiceScopeFactory _scopeFactory;
    readonly IHubContext<AdminHub, IAdminClient> _hubContext;
    readonly IOptionsMonitor<FlagEgressConfig> _options;
    readonly ILogger<FlagEgressService> _logger;

    readonly ConcurrentDictionary<Guid, RecorderState> _recorders = new();
    readonly ConcurrentDictionary<AggKey, AggState> _agg = new();
    readonly Timer _flushTimer;

    int _disposed;

    public FlagEgressService(
        IServiceScopeFactory scopeFactory,
        IHubContext<AdminHub, IAdminClient> hubContext,
        IOptionsMonitor<FlagEgressConfig> options,
        ILogger<FlagEgressService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _options = options;
        _logger = logger;

        var interval = TimeSpan.FromSeconds(Math.Max(1, options.CurrentValue.FlushIntervalSeconds));
        _flushTimer = new Timer(_ => _ = FlushAsync(), null, interval, interval);
    }

    public void RegisterRecorder(TrafficRecorderDescriptor desc)
    {
        var cfg = _options.CurrentValue;
        if (!cfg.Enabled || string.IsNullOrEmpty(desc.Flag))
            return;

        var flagBytes = Encoding.UTF8.GetBytes(desc.Flag);
        if (flagBytes.Length < 4)
            return;

        var state = new RecorderState(
            Egress: new FlagEgressInspector(flagBytes),
            Ingress: new FlagEgressInspector(flagBytes),
            ParticipationId: desc.ParticipationId,
            ChallengeId: desc.ChallengeId,
            GameId: desc.GameId,
            ContainerId: desc.ContainerId,
            IsStaticFlag: desc.IsStaticFlag);

        _recorders[desc.ContainerId] = state;
    }

    public void UnregisterRecorder(Guid containerId)
    {
        _recorders.TryRemove(containerId, out _);
    }

    /// <summary>
    /// Inspect a captured buffer. Called synchronously from CaptureNetworkStream.
    /// Fast path — does not allocate when the data is below the configured
    /// minimum length or when no inspector is registered.
    /// </summary>
    public void Inspect(
        Guid containerId,
        ReadOnlySpan<byte> data,
        FlagEgressDirection direction,
        IPEndPoint source,
        IPEndPoint dest,
        DateTimeOffset timestamp)
    {
        var cfg = _options.CurrentValue;
        if (!cfg.Enabled || data.Length < cfg.MinPacketDataLength)
            return;

        if (!_recorders.TryGetValue(containerId, out var state))
            return;

        var inspector = direction == FlagEgressDirection.ContainerToTeam ? state.Egress : state.Ingress;
        if (!inspector.Inspect(data))
            return;

        var remoteEndPoint = direction == FlagEgressDirection.ContainerToTeam ? dest : source;
        var remoteAddress = remoteEndPoint.Address;
        if (remoteAddress.IsIPv4MappedToIPv6)
            remoteAddress = remoteAddress.MapToIPv4();

        OnHit(state, remoteAddress, remoteEndPoint.Port, direction, timestamp);
    }

    void OnHit(RecorderState state, IPAddress remote, int remotePort, FlagEgressDirection dir, DateTimeOffset ts)
    {
        var remoteIp = remote.ToString();
        var key = new AggKey(state.ParticipationId, state.ChallengeId, remoteIp, dir);

        var windowSeconds = Math.Max(1, _options.CurrentValue.WindowSeconds);
        var now = ts;

        while (true)
        {
            if (_agg.TryGetValue(key, out var existing))
            {
                if (now - existing.LastSeen <= TimeSpan.FromSeconds(windowSeconds))
                {
                    lock (existing)
                    {
                        existing.HitCount++;
                        if (now > existing.LastSeen) existing.LastSeen = now;
                        existing.PendingFlush = true;
                    }
                    return;
                }

                // Window expired — drop the stale entry; a new one will be inserted below.
                if (!_agg.TryRemove(new KeyValuePair<AggKey, AggState>(key, existing)))
                    continue;
            }

            var fresh = new AggState
            {
                EventId = 0,
                HitCount = 1,
                FirstSeen = now,
                LastSeen = now,
                RemoteIp = remoteIp,
                RemotePort = remotePort,
                ContainerId = state.ContainerId,
                Direction = dir,
                PendingFlush = false
            };

            if (_agg.TryAdd(key, fresh))
            {
                _ = PersistFirstHitAsync(state, fresh, dir);
                return;
            }
        }
    }

    async Task PersistFirstHitAsync(RecorderState state, AggState agg, FlagEgressDirection dir)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var suspicion = scope.ServiceProvider.GetRequiredService<ISuspicionService>();

            var entity = new FlagEgressEvent
            {
                GameId = state.GameId,
                ParticipationId = state.ParticipationId,
                ChallengeId = state.ChallengeId,
                ContainerId = state.ContainerId,
                RemoteIp = agg.RemoteIp,
                RemotePort = agg.RemotePort,
                FirstSeenUtc = agg.FirstSeen,
                LastSeenUtc = agg.LastSeen,
                HitCount = agg.HitCount,
                Direction = dir
            };

            db.FlagEgressEvents.Add(entity);
            await db.SaveChangesAsync();
            agg.EventId = entity.Id;

            var participation = await db.Participations
                .AsNoTracking()
                .Include(p => p.Team)
                .Include(p => p.Game)
                .FirstOrDefaultAsync(p => p.Id == state.ParticipationId);

            string teamName = string.Empty;
            if (participation is not null)
            {
                teamName = participation.Team.Name;

                // For dynamic flags, observing the flag in this team's proxied traffic is
                // strong evidence of a successful solve / exfil — raise SuspicionEvent.
                // For static flags every successful team will trip the same flag, so
                // raising suspicion would over-fire on normal play. We still record the
                // FlagEgressEvent row + admin broadcast so operators can see who is
                // pulling the static flag and how often (frequency anomalies still
                // indicate automated tooling), but skip the per-team score bump.
                if (!state.IsStaticFlag)
                {
                    var details = $"remoteIp={agg.RemoteIp}:{agg.RemotePort} direction={dir} container={state.ContainerId}";
                    try
                    {
                        await suspicion.AddSuspicion(participation, SuspicionType.FlagEgress, details);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "FlagEgressService suspicion record failed for participation={Pid}", state.ParticipationId);
                    }
                }
            }

            var challengeTitle = await db.GameChallenges
                .AsNoTracking()
                .Where(c => c.Id == state.ChallengeId)
                .Select(c => c.Title)
                .FirstOrDefaultAsync();

            var notice = new FlagEgressHitModel
            {
                Id = entity.Id,
                GameId = state.GameId,
                ParticipationId = state.ParticipationId,
                ChallengeId = state.ChallengeId,
                ContainerId = state.ContainerId,
                TeamName = teamName,
                ChallengeTitle = challengeTitle ?? string.Empty,
                RemoteIp = agg.RemoteIp,
                RemotePort = agg.RemotePort,
                HitCount = agg.HitCount,
                FirstSeenUtc = agg.FirstSeen,
                LastSeenUtc = agg.LastSeen,
                Direction = dir
            };

            try
            {
                await _hubContext.Clients.All.ReceivedFlagEgress(notice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FlagEgressService broadcast failed for participation={Pid}", state.ParticipationId);
            }

            _logger.LogWarning(
                "Flag egress: participation={Pid} challenge={Cid} container={Container} remote={Remote}:{Port} direction={Direction}",
                state.ParticipationId, state.ChallengeId, state.ContainerId, agg.RemoteIp, agg.RemotePort, dir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FlagEgressService persist failed for participation={Pid}", state.ParticipationId);
        }
    }

    async Task FlushAsync()
    {
        if (Volatile.Read(ref _disposed) == 1)
            return;

        var windowSeconds = Math.Max(1, _options.CurrentValue.WindowSeconds);
        var now = DateTimeOffset.UtcNow;
        var stale = TimeSpan.FromSeconds(windowSeconds);

        List<(int Id, int HitCount, DateTimeOffset LastSeen)>? pending = null;

        foreach (var (key, agg) in _agg)
        {
            int hitCount;
            DateTimeOffset lastSeen;
            int eventId;
            bool shouldFlush;

            lock (agg)
            {
                hitCount = agg.HitCount;
                lastSeen = agg.LastSeen;
                eventId = agg.EventId;
                shouldFlush = agg.PendingFlush;
                agg.PendingFlush = false;
            }

            if (eventId > 0 && shouldFlush)
            {
                pending ??= new();
                pending.Add((eventId, hitCount, lastSeen));
            }

            if (now - lastSeen > stale)
                _agg.TryRemove(new KeyValuePair<AggKey, AggState>(key, agg));
        }

        if (pending is null || pending.Count == 0)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var (id, hitCount, lastSeen) in pending)
            {
                await db.FlagEgressEvents
                    .Where(e => e.Id == id)
                    .ExecuteUpdateAsync(setter => setter
                        .SetProperty(e => e.HitCount, hitCount)
                        .SetProperty(e => e.LastSeenUtc, lastSeen));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FlagEgressService flush failed ({Count} pending)", pending.Count);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await _flushTimer.DisposeAsync();

        try
        {
            await FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FlagEgressService final flush failed");
        }
    }

    sealed record RecorderState(
        FlagEgressInspector Egress,
        FlagEgressInspector Ingress,
        int ParticipationId,
        int ChallengeId,
        int GameId,
        Guid ContainerId,
        bool IsStaticFlag);

    readonly record struct AggKey(int ParticipationId, int ChallengeId, string RemoteIp, FlagEgressDirection Direction);

    sealed class AggState
    {
        public int EventId;
        public int HitCount;
        public DateTimeOffset FirstSeen;
        public DateTimeOffset LastSeen;
        public string RemoteIp = string.Empty;
        public int RemotePort;
        public Guid ContainerId;
        public FlagEgressDirection Direction;
        public bool PendingFlush;
    }
}
