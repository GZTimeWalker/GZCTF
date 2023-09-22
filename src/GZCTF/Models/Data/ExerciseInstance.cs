using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(UserId))]
public class ExerciseInstance : Instance
{
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
    public int ChallengeId { get; set; }

    /// <summary>
    /// 练习题目对象
    /// </summary>
    public ExerciseChallenge Challenge { get; set; } = default!;
    
    #endregion
}