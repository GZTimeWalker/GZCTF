using System.Data;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.SkillTrees.Application;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.LearningPaths.Application;

public sealed class LessonProgressService(AppDbContext db, SkillTreeEnrollmentService enrollments)
{
    public async Task CompleteAsync(Guid userId, Guid lessonId, CancellationToken token)
    {
        var access = await enrollments.GetLessonAccessAsync(userId, lessonId, token);
        if (access == LessonAccess.NotFound)
            throw new LearningLessonNotFoundException();
        if (access == LessonAccess.EnrollmentRequired)
            throw new LearningEnrollmentRequiredException();

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, token);

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
