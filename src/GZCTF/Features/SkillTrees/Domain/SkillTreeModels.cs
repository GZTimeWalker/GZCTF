using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Models.Data;

namespace GZCTF.Features.SkillTrees.Domain;

public enum SkillTreeRevisionStatus : byte
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public sealed class SkillTree
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string IconKey { get; set; } = SkillTreeIconCatalog.Default;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? CurrentPublishedRevisionId { get; set; }
    public SkillTreeRevision? CurrentPublishedRevision { get; set; }
    [JsonIgnore, Timestamp]
    public uint RowVersion { get; set; }
    public List<SkillTreeRevision> Revisions { get; set; } = [];
    public List<SkillTreeEnrollment> Enrollments { get; set; } = [];
}

public sealed class SkillTreeRevision
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SkillTreeId { get; set; }
    public SkillTree SkillTree { get; set; } = null!;
    public SkillTreeRevisionStatus Status { get; set; } = SkillTreeRevisionStatus.Draft;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAtUtc { get; set; }
    [JsonIgnore, Timestamp]
    public uint RowVersion { get; set; }
    public List<SkillTreeCategoryRef> Categories { get; set; } = [];
}

public sealed class SkillCategory
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string IconKey { get; set; } = SkillTreeIconCatalog.Default;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? MergedIntoCategoryId { get; set; }
    public SkillCategory? MergedIntoCategory { get; set; }
    [JsonIgnore, Timestamp]
    public uint RowVersion { get; set; }
    public List<SkillTreeCategoryRef> SkillTrees { get; set; } = [];
    public List<CategoryContent> Contents { get; set; } = [];
}

public sealed class SkillTreeCategoryRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RevisionId { get; set; }
    public SkillTreeRevision Revision { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public SkillCategory Category { get; set; } = null!;
    public int SortOrder { get; set; }
}

public sealed class CategoryContent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CategoryId { get; set; }
    public SkillCategory Category { get; set; } = null!;
    public int SortOrder { get; set; }
    public Guid? LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public Guid? ChallengeId { get; set; }
    public CanonicalChallenge? Challenge { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SkillTreeEnrollment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public UserInfo User { get; set; } = null!;
    public Guid SkillTreeId { get; set; }
    public SkillTree SkillTree { get; set; } = null!;
    public bool IsCurrent { get; set; }
    public DateTimeOffset EnrolledAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LearningPathRedirect
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid LearningPathId { get; set; }
    public string OldSlug { get; set; } = string.Empty;
    public Guid SkillTreeId { get; set; }
    public SkillTree SkillTree { get; set; } = null!;
}
