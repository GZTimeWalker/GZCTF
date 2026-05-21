namespace GZCTF.Services.Container.Build;

/// <summary>
/// Placeholder for the Kubernetes-runtime path. Auto-build in k8s needs
/// a sandboxed builder (kaniko, BuildKit-in-pod, etc.) that pushes to a
/// registry the cluster can pull from — a much bigger feature than
/// what fits in v1. For now this just surfaces a clear failure so the
/// admin gets pointed at the right alternative.
/// </summary>
public sealed class K8sChallengeImageBuilder : IChallengeImageBuilder
{
    public Task<ChallengeBuildResult> BuildAsync(ChallengeBuildRequest req, CancellationToken token) =>
        Task.FromResult(new ChallengeBuildResult(
            Success: false,
            ImageTag: null,
            Digest: null,
            LogTail: string.Empty,
            ErrorMessage:
                "Auto-build is not supported in kubernetes runtime in v1. " +
                "Publish the image to a registry the cluster can pull from, " +
                "then point container_image at that registry reference."));
}
