using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

public enum ReviewRating
{
    None,
    Dislike,
    Like
}

[Index(nameof(GameId))]
[Index(nameof(ChallengeId))]
[Index(nameof(UserId))]
[Index(nameof(ChallengeId), nameof(UserId), IsUnique = true)]
public class ChallengeReview
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ChallengeId { get; set; }

    [JsonIgnore]
    public GameChallenge Challenge { get; set; } = null!;

    [Required]
    public Guid UserId { get; set; }

    [JsonIgnore]
    public UserInfo User { get; set; } = null!;

    [Required]
    public int GameId { get; set; }

    [JsonIgnore]
    public Game Game { get; set; } = null!;

    public ReviewRating Rating { get; set; } = ReviewRating.None;

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public DateTimeOffset SubmitTimeUtc { get; set; } = DateTimeOffset.UtcNow;
}
