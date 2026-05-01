using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeReviewRepository(AppDbContext context) : IChallengeReviewRepository
{
    public Task<ChallengeReview?> GetReviewAsync(Guid userId, int challengeId, CancellationToken token = default)
    {
        return context.ChallengeReviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ChallengeId == challengeId, token);
    }

    public Task<ChallengeReview[]> GetReviewsAsync(int gameId, int skip, int count, string? search = null, ReviewRating? rating = null, CancellationToken token = default)
    {
        var query = context.ChallengeReviews
            .Include(r => r.User)
            .Include(r => r.Challenge)
            .Where(r => r.GameId == gameId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.User!.UserName!.Contains(search) || r.Challenge!.Title!.Contains(search));
        }

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        return query
            .OrderByDescending(r => r.SubmitTimeUtc)
            .Skip(skip)
            .Take(count)
            .ToArrayAsync(token);
    }

    public Task<ChallengeReview[]> GetAllReviewsAsync(int count, int skip, CancellationToken token = default)
    {
        return context.ChallengeReviews
            .Include(r => r.User)
            .Include(r => r.Challenge)
                .ThenInclude(c => c.Game)
            .OrderByDescending(r => r.SubmitTimeUtc)
            .Skip(skip)
            .Take(count)
            .ToArrayAsync(token);
    }

    public Task<int> GetReviewCountAsync(int gameId, string? search = null, ReviewRating? rating = null, CancellationToken token = default)
    {
        var query = context.ChallengeReviews.Where(r => r.GameId == gameId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.User!.UserName!.Contains(search) || r.Challenge!.Title!.Contains(search));
        }

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        return query.CountAsync(token);
    }

    public async Task AddOrUpdateReviewAsync(ChallengeReview review, CancellationToken token = default)
    {
        var existing = await context.ChallengeReviews
            .FirstOrDefaultAsync(r => r.UserId == review.UserId && r.ChallengeId == review.ChallengeId, token);

        if (existing == null)
        {
            await context.ChallengeReviews.AddAsync(review, token);
        }
        else
        {
            existing.Rating = review.Rating;
            existing.Comment = review.Comment;
            existing.SubmitTimeUtc = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(token);
    }

    public Task<Models.Response.Game.ChallengeRatingSummary[]> GetRatingSummariesAsync(int gameId, CancellationToken token = default)
    {
        return context.ChallengeReviews
            .Where(r => r.GameId == gameId && r.Rating != ReviewRating.None)
            .GroupBy(r => r.ChallengeId)
            .Select(g => new Models.Response.Game.ChallengeRatingSummary
            {
                ChallengeId = g.Key,
                Likes = g.Count(r => r.Rating == ReviewRating.Like),
                Dislikes = g.Count(r => r.Rating == ReviewRating.Dislike),
            })
            .ToArrayAsync(token);
    }

    public async Task<Models.Response.Admin.ReviewAnalyticsModel> GetAnalyticsAsync(int gameId, CancellationToken token = default)
    {
        var reviews = await context.ChallengeReviews
            .Where(r => r.GameId == gameId)
            .Include(r => r.Challenge)
            .ToArrayAsync(token);

        var total = reviews.Length;
        var likes = reviews.Count(r => r.Rating == ReviewRating.Like);
        var dislikes = reviews.Count(r => r.Rating == ReviewRating.Dislike);

        var topLiked = reviews
            .Where(r => r.Rating == ReviewRating.Like)
            .GroupBy(r => r.Challenge)
            .Where(g => g.Key != null)
            .Select(g => new Models.Response.Admin.TopChallengeModel { Id = g.Key!.Id, Title = g.Key!.Title, Count = g.Count() })
            .OrderByDescending(c => c.Count)
            .Take(5)
            .ToArray();

        var topDisliked = reviews
            .Where(r => r.Rating == ReviewRating.Dislike)
            .GroupBy(r => r.Challenge)
            .Where(g => g.Key != null)
            .Select(g => new Models.Response.Admin.TopChallengeModel { Id = g.Key!.Id, Title = g.Key!.Title, Count = g.Count() })
            .OrderByDescending(c => c.Count)
            .Take(5)
            .ToArray();

        return new Models.Response.Admin.ReviewAnalyticsModel
        {
            Total = total,
            Likes = likes,
            Dislikes = dislikes,
            TopLiked = topLiked,
            TopDisliked = topDisliked
        };
    }
}
