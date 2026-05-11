namespace GZCTF.Models.Internal;

/// <summary>
/// Configuration for the container-access cheat-detection layer
/// (<see cref="GZCTF.Services.ContainerAccessLogger"/> +
/// <see cref="GZCTF.Services.ContainerAccessSubmissionDetector"/>).
/// </summary>
public class CheatDetectionConfig
{
    /// <summary>
    /// When true, every successful proxy WebSocket open writes a
    /// <see cref="Data.ContainerAccessEvent"/> row. When false, the logger
    /// is a no-op and submission-time access-based signals receive no input.
    /// </summary>
    public bool LogContainerAccess { get; set; } = true;

    /// <summary>
    /// Minutes between the submitter's first proxy access and their
    /// submission, above which <c>DelayedSolveSubmission</c> fires.
    /// </summary>
    public int DelayedSubmissionThresholdMinutes { get; set; } = 60;

    /// <summary>
    /// Seconds between the submitter's first proxy access and their
    /// submission, below which <c>InstantSubmitAfterAccess</c> fires.
    /// </summary>
    public int InstantSubmitThresholdSeconds { get; set; } = 3;
}
