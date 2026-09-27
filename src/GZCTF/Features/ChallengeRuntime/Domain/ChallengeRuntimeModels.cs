using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Models;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Features.ChallengeRuntime.Domain;

public enum ChallengeInstanceStatus : byte
{
    Pending = 0,
    Running = 1,
    Stopped = 2,
    Expired = 3,
    Failed = 4
}

public sealed class UserChallengeInstance
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public UserInfo User { get; set; } = null!;
    public Guid ChallengeId { get; set; }
    public CanonicalChallenge Challenge { get; set; } = null!;
    public ChallengeInstanceStatus Status { get; set; } = ChallengeInstanceStatus.Pending;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? StoppedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public Guid? ContainerId { get; set; }
    public string? AssignedAttachmentKey { get; set; }
    public string? AssignedAttachmentSha256 { get; set; }
    public string? AssignedFlag { get; set; }
    public string? RuntimeMetadataJson { get; set; }
    public List<ChallengeSubmission> Submissions { get; set; } = [];
    public List<ChallengeHelpUsage> HelpUsages { get; set; } = [];
}

public sealed class ChallengeSubmission
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public UserInfo User { get; set; } = null!;
    public Guid ChallengeId { get; set; }
    public CanonicalChallenge Challenge { get; set; } = null!;
    public Guid? InstanceId { get; set; }
    public UserChallengeInstance? Instance { get; set; }
    public string SubmittedFlagHash { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public bool FirstSolve { get; set; }
    public ChallengeSolveMode? SolveMode { get; set; }
    public string? RejectionCode { get; set; }
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ChallengeHelpUsage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public UserInfo User { get; set; } = null!;
    public Guid ChallengeId { get; set; }
    public CanonicalChallenge Challenge { get; set; } = null!;
    public Guid? InstanceId { get; set; }
    public UserChallengeInstance? Instance { get; set; }
    public Guid? HintId { get; set; }
    public ChallengeHint? Hint { get; set; }
    public bool IsWriteup { get; set; }
    public DateTimeOffset ViewedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
