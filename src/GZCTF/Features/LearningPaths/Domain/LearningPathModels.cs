using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Models.Data;

namespace GZCTF.Features.LearningPaths.Domain;

public enum LearningPathRevisionStatus : byte
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

[JsonConverter(typeof(JsonStringEnumConverter<LessonPublicationState>))]
public enum LessonPublicationState : byte
{
    Draft = 0,
    Published = 1,
    Retired = 2
}

public sealed class LearningPath
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Slug { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CurrentPublishedRevisionId { get; set; }
    public LearningPathRevision? CurrentPublishedRevision { get; set; }

    [JsonIgnore]
    [Timestamp]
    public uint RowVersion { get; set; }

    public List<LearningPathLocalization> Localizations { get; set; } = [];
    public List<LearningPathRevision> Revisions { get; set; } = [];
    public List<Enrollment> Enrollments { get; set; } = [];
}

public sealed class LearningPathLocalization
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid PathId { get; set; }
    public LearningPath Path { get; set; } = null!;
    public string Locale { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public sealed class LearningPathRevision
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid PathId { get; set; }
    public LearningPath Path { get; set; } = null!;
    public LearningPathRevisionStatus Status { get; set; } = LearningPathRevisionStatus.Draft;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAtUtc { get; set; }

    [JsonIgnore]
    [Timestamp]
    public uint RowVersion { get; set; }

    public List<LearningModule> Modules { get; set; } = [];
}

public sealed class LearningModule
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RevisionId { get; set; }
    public LearningPathRevision Revision { get; set; } = null!;
    public int SortOrder { get; set; }
    public int ExpectedMinutes { get; set; }
    public List<LearningModuleLocalization> Localizations { get; set; } = [];
    public List<ModuleItem> Items { get; set; } = [];
}

public sealed class LearningModuleLocalization
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ModuleId { get; set; }
    public LearningModule Module { get; set; } = null!;
    public string Locale { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public sealed class ModuleItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ModuleId { get; set; }
    public LearningModule Module { get; set; } = null!;
    public int SortOrder { get; set; }
    public Guid? LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public Guid? ChallengeId { get; set; }
    public CanonicalChallenge? Challenge { get; set; }
}

public sealed class Lesson
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public LessonPublicationState PublicationState { get; set; } = LessonPublicationState.Draft;
    [JsonIgnore, Timestamp]
    public uint RowVersion { get; set; }
    public List<LessonLocalization> Localizations { get; set; } = [];
    public List<ModuleItem> ModuleItems { get; set; } = [];
    public List<CategoryContent> CategoryContents { get; set; } = [];
    public List<LessonProgress> Progress { get; set; } = [];
}

public sealed class LessonLocalization
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public string Locale { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed class Enrollment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public UserInfo User { get; set; } = null!;
    public Guid PathId { get; set; }
    public LearningPath Path { get; set; } = null!;
    public bool IsCurrent { get; set; }
    public DateTimeOffset EnrolledAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LessonProgress
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public UserInfo User { get; set; } = null!;
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public DateTimeOffset CompletedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
