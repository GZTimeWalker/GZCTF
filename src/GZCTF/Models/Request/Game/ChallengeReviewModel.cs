using System.ComponentModel.DataAnnotations;
using GZCTF.Models.Data;

namespace GZCTF.Models.Request.Game;

public class ChallengeReviewModel
{
    [Required]
    public ReviewRating Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}
