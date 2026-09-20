using System.Text.Json.Serialization;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;
using GZCTF.Models.Data;

namespace GZCTF.Features.LearningProgress.Domain;

public enum ChallengeSolveMode : byte
{
    Independent = 0,
    AfterHint = 1,
    AfterWriteup = 2
}

public sealed class ChallengeProgress
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public UserInfo User { get; set; } = null!;
    public Guid ChallengeId { get; set; }
    public CanonicalChallenge Challenge { get; set; } = null!;
    public DateTimeOffset SolvedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ChallengeSolveMode SolveMode { get; set; } = ChallengeSolveMode.Independent;

    [JsonIgnore]
    public string? AttributionMetadataJson { get; set; }
}
