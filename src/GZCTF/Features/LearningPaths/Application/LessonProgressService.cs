using System.Data;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.LearningPaths.Application;

public sealed class LessonProgressService(AppDbContext db)
{
    public async Task CompleteAsync(Guid userId, Guid lessonId, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, token);
        var pathIds = await db.LearningPathRevisions
            .Where(revision => revision.Status == LearningPathRevisionStatus.Published &&
                               revision.Path.CurrentPublishedRevisionId == revision.Id &&
                               revision.Modules.Any(module => module.Items.Any(item => item.LessonId == lessonId)))
            .Select(revision => revision.PathId)
            .ToListAsync(token);
        if (pathIds.Count == 0)
            throw new LearningLessonNotFoundException();

        if (!await db.Enrollments.AnyAsync(
                item => item.UserId == userId && pathIds.Contains(item.PathId), token))
            throw new LearningEnrollmentRequiredException();

        var progress = await db.LessonProgress
            .SingleOrDefaultAsync(item => item.UserId == userId && item.LessonId == lessonId, token);
        if (progress is null)
        {
            db.LessonProgress.Add(new LessonProgress
            {
                UserId = userId,
                LessonId = lessonId,
                CompletedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(token);
        }

        await transaction.CommitAsync(token);
    }
}
