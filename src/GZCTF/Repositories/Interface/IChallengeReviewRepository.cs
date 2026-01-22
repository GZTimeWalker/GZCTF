using GZCTF.Models.Data;

namespace GZCTF.Repositories.Interface;

public interface IChallengeReviewRepository
{
    Task<ChallengeReview?> GetReviewAsync(Guid userId, int challengeId, CancellationToken token = default);
    Task<ChallengeReview[]> GetReviewsAsync(int gameId, int skip, int count, string? search = null, ReviewRating? rating = null, CancellationToken token = default);
    Task AddOrUpdateReviewAsync(ChallengeReview review, CancellationToken token = default);
    Task<int> GetReviewCountAsync(int gameId, string? search = null, ReviewRating? rating = null, CancellationToken token = default);
    Task<ChallengeReview[]> GetAllReviewsAsync(int count, int skip, CancellationToken token = default);
    Task<Models.Response.Admin.ReviewAnalyticsModel> GetAnalyticsAsync(int gameId, CancellationToken token = default);
}
