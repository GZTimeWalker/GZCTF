using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(SourceId))]
[Index(nameof(TargetId))]
[PrimaryKey(nameof(SourceId), nameof(TargetId))]
public abstract class Dependency<T> where T : class
{
    /// <summary>
    /// Source entity ID
    /// </summary>
    [Required]
    public int SourceId { get; set; }

    /// <summary>
    /// Source entity
    /// </summary>
    [Required]
    public T? Source { get; set; }

    /// <summary>
    /// Dependent entity ID
    /// </summary>
    [Required]
    public int TargetId { get; set; }

    /// <summary>
    /// Dependent entity
    /// </summary>
    [Required]
    public T? Target { get; set; }
}

/// <summary>
/// Dependency relationship for exercise challenges
/// </summary>
public class ExerciseDependency : Dependency<ExerciseChallenge>;
