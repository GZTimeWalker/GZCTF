using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GZCTF.Utils;

namespace GZCTF.Features.ChallengeLibrary.Domain;

public enum ChallengePublicationState : byte
{
    Draft = 0,
    Published = 1,
    Retired = 2
}

public enum ChallengeFlagKind : byte
{
    Static = 0,
    DynamicAttachment = 1,
    Template = 2
}

public sealed class Challenge
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;
    public ChallengePublicationState PublicationState { get; set; } = ChallengePublicationState.Draft;
    public bool IsEnabled { get; set; } = true;
    public int ExpectedMinutes { get; set; } = 60;

    public string SourceType { get; set; } = "native";
    public string SourceId { get; set; } = string.Empty;
    public string? SourceName { get; set; }
    public string? SourceMetadataJson { get; set; }

    [JsonIgnore]
    [Timestamp]
    public uint RowVersion { get; set; }

    public List<ChallengeLocalization> Localizations { get; set; } = [];
    public List<ChallengeFlag> Flags { get; set; } = [];
    public List<ChallengeHint> Hints { get; set; } = [];
    public List<ChallengeWriteup> Writeups { get; set; } = [];
}

public sealed class ChallengeLocalization
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;
    public string Locale { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed class ChallengeFlag
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;
    public ChallengeFlagKind Kind { get; set; } = ChallengeFlagKind.Static;
    public string? Value { get; set; }
    public string? Template { get; set; }
    public string? AttachmentPoolKey { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class ChallengeHint
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;
    public int SortOrder { get; set; }
    public string Locale { get; set; } = "en";
    public string Content { get; set; } = string.Empty;
}

public sealed class ChallengeWriteup
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;
    public string Locale { get; set; } = "en";
    public string Content { get; set; } = string.Empty;
}
