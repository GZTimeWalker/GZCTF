using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

/// <summary>Request to clone an existing game into a new one.</summary>
public class GameCloneModel
{
    [Required]
    [MinLength(3)]
    [MaxLength(64)]
    public string Title { get; set; } = string.Empty;

    public DateTimeOffset StartTimeUtc { get; set; } = DateTimeOffset.UtcNow.AddDays(7);
    public DateTimeOffset EndTimeUtc { get; set; } = DateTimeOffset.UtcNow.AddDays(14);

    /// <summary>When true, copy all enabled challenges (flags included for static types).</summary>
    public bool IncludeChallenges { get; set; } = true;
}
