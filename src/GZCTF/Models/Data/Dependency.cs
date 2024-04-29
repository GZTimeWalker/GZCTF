using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(SourceId))]
[Index(nameof(TargetId))]
[PrimaryKey(nameof(SourceId), nameof(TargetId))]
public abstract class Dependency<T> where T : class
{
    /// <summary>
    /// 源实体
    /// </summary>
    [Required]
    public int SourceId { get; set; }

    /// <summary>
    /// 源实体
    /// </summary>
    [Required]
    public T? Source { get; set; }

    /// <summary>
    /// 依赖实体
    /// </summary>
    [Required]
    public int TargetId { get; set; }

    /// <summary>
    /// 依赖实体
    /// </summary>
    [Required]
    public T? Target { get; set; }
}

/// <summary>
/// 练习题目依赖关系
/// </summary>
public class ExerciseDependency : Dependency<ExerciseChallenge>;
