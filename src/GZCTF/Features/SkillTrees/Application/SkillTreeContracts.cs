using System.Text.Json.Serialization;

namespace GZCTF.Features.SkillTrees.Application;

public static class SkillTreeProblemCodes
{
    public const string NotFound = "skill_tree_not_found";
    public const string CategoryNotFound = "skill_category_not_found";
    public const string RevisionConflict = "skill_tree_revision_conflict";
    public const string InvalidIcon = "skill_tree_invalid_icon";
    public const string InvalidCategoryOrder = "skill_tree_invalid_category_order";
    public const string ConfirmationMismatch = "skill_tree_confirmation_mismatch";
    public const string ContentCategoryRequired = "content_category_required";
    public const string CategoryHasNoTree = "content_category_has_no_active_tree";
    public const string MergeConflict = "skill_category_merge_conflict";
}

/*
 * Public discovery contracts (ST06).
 */

public sealed record SkillTreeSummaryResponse(
    Guid SkillTreeId, string Name, string Summary, string IconKey,
    int CategoryCount, int ChallengeCount, int LessonCount);

public sealed record SkillTreeDetailResponse(
    Guid SkillTreeId, string Name, string Summary, string IconKey,
    IReadOnlyList<SkillCategoryPublicResponse> Categories);

public sealed record SkillCategoryPublicResponse(
    Guid CategoryId, string Name, string Summary, string IconKey, int SortOrder,
    IReadOnlyList<SkillTreeContentSummaryResponse> Contents);

public sealed record SkillTreeContentSummaryResponse(
    Guid ContentId, string Kind, int SortOrder, string Title, string Summary,
    int ExpectedMinutes, string Difficulty,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? State = null);

public sealed record CategoryTreeReferenceResponse(Guid SkillTreeId, string Name, bool IsPublished);

/*
 * Category administration contracts (ST08-ST09).
 */

public sealed record SkillCategoryCommand(string Name, string Summary, string IconKey, uint? RowVersion);
public sealed record CategoryContentOrderCommand(string Kind, Guid ContentId, int SortOrder);
public sealed record UpdateCategoryContentsCommand(
    uint RowVersion, IReadOnlyList<CategoryContentOrderCommand> Contents);
public sealed record CategoryTreeMembershipCommand(
    Guid SkillTreeId, bool Included, uint SkillTreeRowVersion);
public sealed record UpdateCategoryTreeMembershipsCommand(
    uint CategoryRowVersion, IReadOnlyList<CategoryTreeMembershipCommand> Trees);
public sealed record SkillCategoryAdminResponse(
    Guid CategoryId, string Name, string Summary, string IconKey, uint RowVersion,
    IReadOnlyList<CategoryTreeReferenceResponse> Trees,
    IReadOnlyList<SkillTreeContentSummaryResponse> Contents);
public sealed record CategoryDeleteImpactResponse(
    Guid CategoryId, string Name, int DraftTreeCount, int PublishedTreeCount,
    int ChallengeCount, int LessonCount, bool RequiresTypedConfirmation);
public sealed record DeleteCategoryCommand(string ConfirmationName, uint RowVersion);
public sealed record MergeSkillCategoryCommand(
    Guid SurvivorCategoryId, Guid DuplicateCategoryId,
    uint SurvivorRowVersion, uint DuplicateRowVersion);
public sealed record UpdateCategoryTreeMembershipsResponse(
    Guid CategoryId, IReadOnlyList<Guid> AffectedSkillTreeIds);

/*
 * Content publication contracts (ST10-ST11).
 */

public sealed record InlineSkillCategoryCommand(
    Guid SkillTreeId, string Name, string Summary, string IconKey);
public sealed record PublishContentCommand(
    uint RowVersion,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<InlineSkillCategoryCommand> InlineCategories);
public sealed record ChallengePublicationEditState(
    uint RowVersion, string PublicationState, IReadOnlyList<Guid> CategoryIds);
public sealed record LessonPublicationEditState(
    uint RowVersion, string PublicationState, IReadOnlyList<Guid> CategoryIds);

/*
 * Enrollment and personal record contracts (ST12).
 */

public sealed record SkillTreeEnrollmentResponse(
    Guid EnrollmentId, Guid SkillTreeId, string Name, string IconKey,
    bool IsCurrent, DateTimeOffset EnrolledAtUtc);
public sealed record MyLearningResponse(
    Guid? CurrentSkillTreeId, IReadOnlyList<MySkillTreeRecordResponse> SkillTrees,
    IReadOnlyList<RecentLearningActivityResponse> RecentActivity);
public sealed record MySkillTreeRecordResponse(
    Guid SkillTreeId, string Name, string IconKey, bool IsCurrent, bool IsDeleted,
    int CategoryCount, int CompletedCategoryCount,
    int ChallengeCount, int CompletedChallengeCount,
    int LessonCount, int CompletedLessonCount);
public sealed record RecentLearningActivityResponse(
    string Kind, Guid ContentId, string Title, DateTimeOffset CompletedAtUtc, string? SolveMode);
public sealed record LearningRedirectResponse(string TargetPath);
