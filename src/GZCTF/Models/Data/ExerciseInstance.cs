using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(UserId))]
[Index(nameof(FlagId))]
[PrimaryKey(nameof(UserId), nameof(ExerciseId))]
public class ExerciseInstance : Instance
{
    /// <summary>
    /// 获取实例附件
    /// </summary>
    internal Attachment? Attachment => Exercise.Type == ChallengeType.DynamicAttachment
        ? FlagContext?.Attachment
        : Exercise.Attachment;

    /// <summary>
    /// 获取实例附件链接
    /// </summary>
    internal string? AttachmentUrl => Exercise.Type == ChallengeType.DynamicAttachment
        ? FlagContext?.Attachment?.UrlWithName(Exercise.FileName)
        : Exercise.Attachment?.UrlWithName();

    #region Db Relationship

    /// <summary>
    /// 用户 Id
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// 参赛用户
    /// </summary>
    public UserInfo User { get; set; } = default!;

    /// <summary>
    /// 题目 Id
    /// </summary>
    [Required]
    public int ExerciseId { get; set; }

    /// <summary>
    /// 练习题目对象
    /// </summary>
    public ExerciseChallenge Exercise { get; set; } = default!;

    #endregion
}
