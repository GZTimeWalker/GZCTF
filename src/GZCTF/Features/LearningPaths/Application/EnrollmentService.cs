using System.Data;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.LearningPaths.Application;

public sealed class EnrollmentService(AppDbContext db)
{
    public async Task<EnrollmentResponse?> EnrollAsync(
        Guid userId, Guid pathId, string? locale, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, token);
        var path = await db.LearningPaths
            .Include(item => item.Localizations)
            .SingleOrDefaultAsync(item => item.Id == pathId && item.CurrentPublishedRevisionId != null, token);
        if (path is null)
            return null;

        var enrollment = await db.Enrollments
            .SingleOrDefaultAsync(item => item.UserId == userId && item.PathId == pathId, token);
        if (enrollment is null)
        {
            var hasCurrent = await db.Enrollments.AnyAsync(item => item.UserId == userId && item.IsCurrent, token);
            enrollment = new Enrollment
            {
                UserId = userId,
                PathId = pathId,
                IsCurrent = !hasCurrent
            };
            db.Enrollments.Add(enrollment);
            await db.SaveChangesAsync(token);
        }

        await transaction.CommitAsync(token);
        return ToResponse(enrollment, path, locale);
    }

    public async Task<bool> LeaveAsync(Guid userId, Guid pathId, CancellationToken token)
    {
        var enrollment = await db.Enrollments
            .SingleOrDefaultAsync(item => item.UserId == userId && item.PathId == pathId, token);
        if (enrollment is null)
            return false;

        db.Enrollments.Remove(enrollment);
        await db.SaveChangesAsync(token);
        return true;
    }

    public async Task<EnrollmentResponse?> SelectCurrentAsync(
        Guid userId, Guid pathId, string? locale, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, token);
        var enrollment = await db.Enrollments
            .SingleOrDefaultAsync(item => item.UserId == userId && item.PathId == pathId, token);
        if (enrollment is null)
            return null;

        await db.Enrollments
            .Where(item => item.UserId == userId && item.IsCurrent)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsCurrent, false), token);
        await db.Entry(enrollment).ReloadAsync(token);
        enrollment.IsCurrent = true;
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);

        var path = await db.LearningPaths
            .Include(item => item.Localizations)
            .SingleAsync(item => item.Id == pathId, token);
        return ToResponse(enrollment, path, locale);
    }

    public async Task<IReadOnlyList<EnrollmentResponse>> ListAsync(
        Guid userId, string? locale, CancellationToken token)
    {
        var enrollments = await db.Enrollments
            .AsNoTracking()
            .Include(item => item.Path)
                .ThenInclude(item => item.Localizations)
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.EnrolledAtUtc)
            .ToListAsync(token);
        return enrollments.Select(item => ToResponse(item, item.Path, locale)).ToArray();
    }

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

    private static EnrollmentResponse ToResponse(
        Enrollment enrollment, LearningPath path, string? locale)
    {
        var localization = Pick(path.Localizations, locale);
        return new EnrollmentResponse(
            enrollment.Id, path.Id, path.Slug, localization?.Title ?? path.Slug,
            enrollment.IsCurrent, enrollment.EnrolledAtUtc);
    }

    private static LearningPathLocalization? Pick(
        IEnumerable<LearningPathLocalization> localizations, string? locale)
    {
        var values = localizations.ToArray();
        return values.FirstOrDefault(item => string.Equals(item.Locale, locale, StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault(item => string.Equals(item.Locale, "en", StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault();
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

public sealed record EnrollmentResponse(
    Guid EnrollmentId,
    Guid PathId,
    string Slug,
    string Title,
    bool IsCurrent,
    DateTimeOffset EnrolledAtUtc);

public sealed record LessonContentResponse(Guid LessonId, string Locale, string Title, string Body);

public sealed class LearningLessonNotFoundException : Exception;
public sealed class LearningEnrollmentRequiredException : Exception;
