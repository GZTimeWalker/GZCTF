using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningPaths.Domain;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Features.Imports.Domain;

public enum MigrationBatchState : byte
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

public sealed class MigrationBatch
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string SourceType { get; set; } = string.Empty;
    public string PackageFingerprintSha256 { get; set; } = string.Empty;
    public MigrationBatchState State { get; set; } = MigrationBatchState.Pending;
    public int ChallengeCount { get; set; }
    public int PathCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public string? WarningsJson { get; set; }
    public string? ErrorsJson { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }

    public List<LegacyChallengeMap> ChallengeMappings { get; set; } = [];
    public List<LegacyPathMap> PathMappings { get; set; } = [];
}

public sealed class LegacyChallengeMap
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public Guid ChallengeId { get; set; }
    public CanonicalChallenge Challenge { get; set; } = null!;
    public Guid? MigrationBatchId { get; set; }
    public MigrationBatch? MigrationBatch { get; set; }
}

public sealed class LegacyPathMap
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public Guid PathId { get; set; }
    public LearningPath Path { get; set; } = null!;
    public Guid? MigrationBatchId { get; set; }
    public MigrationBatch? MigrationBatch { get; set; }
}
