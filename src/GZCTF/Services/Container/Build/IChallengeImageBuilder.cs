namespace GZCTF.Services.Container.Build;

/// <summary>
/// Inputs for a one-shot challenge image build. The context directory
/// is what gets tar'd up and shipped to <c>docker build</c>; the
/// dockerfile path is relative to that directory.
/// </summary>
public sealed record ChallengeBuildRequest(
    int GameId,
    string ChallengeSlug,
    string ContextDir,
    string Dockerfile);

/// <summary>
/// Outcome of a single build. On success <see cref="ImageTag"/> is what
/// the caller assigns to <c>GameChallenge.ContainerImage</c>; on
/// failure <see cref="LogTail"/> carries the last few KiB of build
/// output for the admin audit modal.
/// </summary>
public sealed record ChallengeBuildResult(
    bool Success,
    string? ImageTag,
    string? Digest,
    string LogTail,
    string? ErrorMessage);

/// <summary>
/// Builds an OCI image from a directory of files (the extracted
/// challenge package) and tags it for the local runtime to pick up.
/// Docker impl uses <c>BuildImageFromDockerfileAsync</c> against the
/// mounted socket; the Kubernetes impl is a placeholder that surfaces
/// "not supported in kubernetes runtime" until kaniko / BuildKit-in-pod
/// is wired up.
/// </summary>
public interface IChallengeImageBuilder
{
    /// <summary>
    /// Run a single image build.
    /// </summary>
    /// <param name="req">Build inputs (context dir + dockerfile + slug).</param>
    /// <param name="token">Cancel token plumbed to the underlying docker
    /// call.</param>
    /// <param name="onProgress">Optional sink invoked once per line of
    /// build output. The implementation buffers these locally too —
    /// this callback exists so callers can stream the live log to
    /// somewhere visible (e.g. update the challenge row periodically so
    /// the admin UI can watch in real time). May be called from a
    /// non-UI thread; the sink must be threadsafe.</param>
    Task<ChallengeBuildResult> BuildAsync(
        ChallengeBuildRequest req,
        CancellationToken token,
        Action<string>? onProgress = null);
}
