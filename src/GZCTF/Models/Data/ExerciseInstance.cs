using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(UserId))]
[PrimaryKey(nameof(UserId), nameof(ExerciseId))]
public class ExerciseInstance : Instance
{
    /// <summary>
    /// Get instance attachment
    /// </summary>
    internal Attachment? Attachment =>
        Exercise.Type == ChallengeType.DynamicAttachment
            ? FlagContext?.Attachment
            : Exercise.Attachment;

    /// <summary>
    /// Get instance attachment URL
    /// </summary>
    internal string? AttachmentUrl =>
        Exercise.Type == ChallengeType.DynamicAttachment
            ? FlagContext?.Attachment?.UrlWithName(Exercise.FileName)
            : Exercise.Attachment?.UrlWithName();

    /// <summary>
    /// Time when the answer was solved
    /// </summary>
    public DateTimeOffset SolveTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    #region Db Relationship

    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Participating user
    /// </summary>
    public UserInfo User { get; set; } = null!;

    /// <summary>
    /// Exercise ID
    /// </summary>
    [Required]
    public int ExerciseId { get; set; }

    /// <summary>
    /// Exercise object
    /// </summary>
    public ExerciseChallenge Exercise { get; set; } = null!;

    #endregion
}
