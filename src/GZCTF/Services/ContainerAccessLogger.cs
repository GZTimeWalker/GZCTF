using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using Microsoft.Extensions.Options;

namespace GZCTF.Services;

/// <summary>
/// Context describing a single proxy WebSocket open. Built by
/// <see cref="GZCTF.Controllers.ProxyController"/> and passed to
/// <see cref="IContainerAccessLogger.LogAccess"/>.
/// </summary>
public sealed record ContainerAccessContext(
    Guid ContainerId,
    int ChallengeId,
    int ContainerOwnerParticipationId,
    int GameId,
    Guid? AccessingUserId,
    string? AccessingUserName,
    int? AccessingParticipationId,
    string RemoteIp,
    string? UserAgent,
    bool IsAdmin,
    DateTimeOffset ConnectedAtUtc);

public interface IContainerAccessLogger
{
    /// <summary>
    /// Persist a <see cref="ContainerAccessEvent"/> for the connect and,
    /// if the access is from a different team and the user is not an
    /// admin/monitor, raise <see cref="SuspicionType.CrossTeamContainerAccess"/>.
    /// </summary>
    Task LogAccess(ContainerAccessContext ctx, CancellationToken token = default);
}

public sealed class ContainerAccessLogger(
    AppDbContext db,
    ISuspicionService suspicion,
    IOptions<CheatDetectionConfig> options,
    ILogger<ContainerAccessLogger> logger) : IContainerAccessLogger
{
    public async Task LogAccess(ContainerAccessContext ctx, CancellationToken token = default)
    {
        var cfg = options.Value;
        if (!cfg.LogContainerAccess)
            return;

        try
        {
            var row = new ContainerAccessEvent
            {
                GameId = ctx.GameId,
                ChallengeId = ctx.ChallengeId,
                ContainerOwnerParticipationId = ctx.ContainerOwnerParticipationId,
                ContainerId = ctx.ContainerId,
                AccessingUserId = ctx.AccessingUserId,
                AccessingUserName = ctx.AccessingUserName,
                AccessingParticipationId = ctx.AccessingParticipationId,
                RemoteIp = ctx.RemoteIp,
                UserAgent = ctx.UserAgent,
                ConnectedAtUtc = ctx.ConnectedAtUtc,
            };

            db.ContainerAccessEvents.Add(row);
            await db.SaveChangesAsync(token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "ContainerAccessLogger persist failed for container {Container} (game {Game}, owner pid {Owner})",
                ctx.ContainerId, ctx.GameId, ctx.ContainerOwnerParticipationId);
            // Persistence failure must not block proxy. The suspicion raise below
            // is independent and may still succeed — but if we don't have a row,
            // the resulting SuspicionEvent's Details still carries the context.
        }

        // Raise CrossTeamContainerAccess only when:
        //   - the connecting user is authenticated and resolvable to a participation in this game,
        //   - that participation is NOT the container-owning participation,
        //   - the user is not an admin/monitor (admins legitimately access any container).
        if (ctx.IsAdmin) return;
        if (ctx.AccessingUserId is null) return;
        if (ctx.AccessingParticipationId is null) return;
        if (ctx.AccessingParticipationId == ctx.ContainerOwnerParticipationId) return;

        try
        {
            var stub = new Participation
            {
                Id = ctx.ContainerOwnerParticipationId,
                GameId = ctx.GameId,
            };

            var details =
                $"accessingUserId={ctx.AccessingUserId} accessingUserName={ctx.AccessingUserName} " +
                $"accessingParticipationId={ctx.AccessingParticipationId} containerId={ctx.ContainerId} " +
                $"remoteIp={ctx.RemoteIp}";

            await suspicion.AddSuspicion(
                stub,
                SuspicionType.CrossTeamContainerAccess,
                details,
                relatedParticipationId: ctx.AccessingParticipationId,
                token: token);

            logger.LogWarning(
                "CrossTeamContainerAccess raised: container={Container} owner-pid={Owner} accessing-pid={Accessing} user={User} ip={Ip}",
                ctx.ContainerId, ctx.ContainerOwnerParticipationId, ctx.AccessingParticipationId,
                ctx.AccessingUserName, ctx.RemoteIp);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "ContainerAccessLogger suspicion-raise failed for container {Container}", ctx.ContainerId);
        }
    }
}
