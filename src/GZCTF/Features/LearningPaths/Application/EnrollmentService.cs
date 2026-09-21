using GZCTF.Features.LearningPaths.Domain;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.LearningPaths.Application;

/// <summary>
/// Serves retained lesson content. Enrollment authorization still lives here because
/// <see cref="LessonProgressService"/> shares the same exception contract; the old
/// LearningPath enrollment surface was retired in the skill tree cutover.
/// </summary>
public sealed class EnrollmentService(AppDbContext db)
{
    public async Task<LessonContentResponse> GetLessonAsync(
        Guid userId, Guid lessonId, string? locale, CancellationToken token)
    {
        var pathIds = await CurrentPublishedPathIdsForLessonAsync(lessonId, token);
        if (pathIds.Count == 0)
            throw new LearningLessonNotFoundException();

        var enrolled = await db.Enrollments.AnyAsync(
            item => item.UserId == userId && pathIds.Contains(item.PathId), token);
        if (!enrolled)
            throw new LearningEnrollmentRequiredException();

        var lesson = await db.Lessons
            .AsNoTracking()
            .Include(item => item.Localizations)
            .SingleOrDefaultAsync(item => item.Id == lessonId, token);
        if (lesson is null)
            throw new LearningLessonNotFoundException();

        var localization = PickLesson(lesson.Localizations, locale);
        return new LessonContentResponse(
            lesson.Id, localization?.Locale ?? "en", localization?.Title ?? string.Empty,
            localization?.Body ?? string.Empty);
    }

    private async Task<List<Guid>> CurrentPublishedPathIdsForLessonAsync(
        Guid lessonId, CancellationToken token) =>
        await db.LearningPathRevisions
            .Where(revision => revision.Status == LearningPathRevisionStatus.Published &&
                               revision.Path.CurrentPublishedRevisionId == revision.Id &&
                               revision.Modules.Any(module => module.Items.Any(item => item.LessonId == lessonId)))
            .Select(revision => revision.PathId)
            .ToListAsync(token);

    private static LessonLocalization? PickLesson(
        IEnumerable<LessonLocalization> localizations, string? locale)
    {
        var values = localizations.ToArray();
        return values.FirstOrDefault(item => string.Equals(item.Locale, locale, StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault(item => string.Equals(item.Locale, "en", StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault();
    }
}

public sealed record LessonContentResponse(Guid LessonId, string Locale, string Title, string Body);

public sealed class LearningLessonNotFoundException : Exception;
public sealed class LearningEnrollmentRequiredException : Exception;
