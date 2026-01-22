using GZCTF.Models.Data;

namespace GZCTF.Models.Response.Admin;

public class ChallengeReviewDetailModel
{
    public int Id { get; set; }
    public int ChallengeId { get; set; }
    public string ChallengeName { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public ReviewRating Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset SubmitTimeUtc { get; set; }

    public static ChallengeReviewDetailModel FromReview(ChallengeReview review) =>
        new()
        {
            Id = review.Id,
            ChallengeId = review.ChallengeId,
            ChallengeName = review.Challenge?.Title ?? string.Empty,
            GameTitle = review.Challenge?.Game?.Title ?? string.Empty,
            UserId = review.UserId,
            UserName = review.User?.UserName ?? string.Empty,
            Rating = review.Rating,
            Comment = review.Comment,
            SubmitTimeUtc = review.SubmitTimeUtc
        };
}
