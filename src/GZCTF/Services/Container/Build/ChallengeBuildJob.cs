namespace GZCTF.Services.Container.Build;

/// <summary>
/// One unit of work for <see cref="ChallengeBuildQueueService"/>.
/// Carries the build context as filesystem paths because the worker
/// runs on the same host as the import that extracted the archive —
/// we don't ship blobs through the channel.
///
/// <para><b>Lifetime caveat:</b> <see cref="ContextDir"/> typically
/// points at a <see cref="System.IO.Path.GetTempPath"/> subdirectory
/// owned by the enqueueing code. The queue worker is responsible for
/// cleaning it up after the build (success or failure). Don't enqueue
/// a job whose context dir might be deleted out from under it.</para>
/// </summary>
/// <param name="ChallengeId">The GameChallenge id being built.</param>
/// <param name="GameId">Owning game id (denormalized so the audit row
/// can be filtered without loading the challenge).</param>
/// <param name="Slug">Human-readable challenge title; used to derive
/// the image tag and for log breadcrumbs.</param>
/// <param name="ContextDir">Filesystem directory shipped to docker.</param>
/// <param name="Dockerfile">Path to the Dockerfile relative to
/// <paramref name="ContextDir"/>.</param>
/// <param name="Trigger">Why this job was enqueued; preserved in the
/// audit row so retries and bulk runs are distinguishable later.</param>
/// <param name="Attempt">1-based attempt counter; auto-retry bumps to
/// 2/3.</param>
/// <param name="OwnsContextDir">If true, the worker deletes
/// <see cref="ContextDir"/> when done. False when the caller is going
/// to clean it up itself (e.g. a synchronous request handler that
/// happens to enqueue).</param>
public sealed record ChallengeBuildJob(
    int ChallengeId,
    int GameId,
    string Slug,
    string ContextDir,
    string Dockerfile,
    BuildTrigger Trigger,
    int Attempt = 1,
    bool OwnsContextDir = true);
