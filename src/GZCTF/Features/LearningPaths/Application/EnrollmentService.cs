using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.SkillTrees.Application;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.LearningPaths.Application;

/// <summary>
/// Serves retained lesson content. Enrollment authorization still lives here because
/// <see cref="LessonProgressService"/> shares the same exception contract; the old
/// LearningPath enrollment surface was retired in the skill tree cutover.
/// </summary>
public sealed class EnrollmentService(AppDbContext db, SkillTreeEnrollmentService enrollments)
{
    public async Task<LessonContentResponse> GetLessonAsync(
        Guid userId, Guid lessonId, string? locale, CancellationToken token)
    {
        switch (await enrollments.GetLessonAccessAsync(userId, lessonId, token))
        {
            case LessonAccess.NotFound:
                throw new LearningLessonNotFoundException();
            case LessonAccess.EnrollmentRequired:
                throw new LearningEnrollmentRequiredException();
        }

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
