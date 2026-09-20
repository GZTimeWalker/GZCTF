using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.LearningProgress.Application;

public sealed class LearningRecordService(AppDbContext db)
{
    private const int RecentActivityLimit = 20;

    public async Task<MyLearningResponse> GetAsync(
        Guid userId, string? locale, CancellationToken token)
    {
        var enrollments = await db.Enrollments
            .AsNoTracking()
            .Include(item => item.Path)
                .ThenInclude(item => item.Localizations)
            .Include(item => item.Path)
                .ThenInclude(item => item.CurrentPublishedRevision!)
                    .ThenInclude(item => item.Modules)
                        .ThenInclude(item => item.Localizations)
            .Include(item => item.Path)
                .ThenInclude(item => item.CurrentPublishedRevision!)
                    .ThenInclude(item => item.Modules)
                        .ThenInclude(item => item.Items)
                            .ThenInclude(item => item.Challenge)
                                .ThenInclude(item => item!.Localizations)
            .Include(item => item.Path)
                .ThenInclude(item => item.CurrentPublishedRevision!)
                    .ThenInclude(item => item.Modules)
                        .ThenInclude(item => item.Items)
                            .ThenInclude(item => item.Lesson)
                                .ThenInclude(item => item!.Localizations)
            .Where(item => item.UserId == userId && item.Path.CurrentPublishedRevisionId != null)
            .OrderBy(item => item.EnrolledAtUtc)
            .ToListAsync(token);

        var lessonProgress = await db.LessonProgress
            .AsNoTracking()
            .Include(item => item.Lesson)
                .ThenInclude(item => item.Localizations)
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CompletedAtUtc)
            .ToListAsync(token);
        var challengeProgress = await db.ChallengeProgress
            .AsNoTracking()
            .Include(item => item.Challenge)
                .ThenInclude(item => item.Localizations)
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.SolvedAtUtc)
            .ToListAsync(token);

        var completedLessons = lessonProgress.Select(item => item.LessonId).ToHashSet();
        var solvedChallenges = challengeProgress.Select(item => item.ChallengeId).ToHashSet();
        var routes = enrollments.Select(item => ToRouteRecord(
            item, locale, completedLessons, solvedChallenges)).ToArray();
        var recentActivity = BuildRecentActivity(lessonProgress, challengeProgress, locale);

        return new MyLearningResponse(
            routes,
            enrollments.SingleOrDefault(item => item.IsCurrent)?.PathId,
            completedLessons.OrderBy(item => item).ToArray(),
            challengeProgress
                .Select(item => new LearningChallengeRecord(
                    item.ChallengeId, item.SolvedAtUtc, item.SolveMode.ToString()))
                .ToArray(),
            solvedChallenges.Count,
            recentActivity);
    }

    private static LearningRouteRecord ToRouteRecord(
        Enrollment enrollment,
        string? locale,
        IReadOnlySet<Guid> completedLessons,
        IReadOnlySet<Guid> solvedChallenges)
    {
        var path = enrollment.Path;
        var revision = path.CurrentPublishedRevision!;
        var pathText = Pick(path.Localizations, locale);
        var modules = revision.Modules
            .OrderBy(item => item.SortOrder)
            .Select(module => ToModuleRecord(module, locale, completedLessons, solvedChallenges))
            .ToArray();
        var totalItems = modules.Sum(item => item.TotalItems);
        var completedItems = modules.Sum(item => item.CompletedItems);
        var completedLessonCount = revision.Modules
            .SelectMany(item => item.Items)
            .Where(item => item.LessonId is not null && completedLessons.Contains(item.LessonId.Value))
            .Select(item => item.LessonId!.Value)
            .Distinct()
            .Count();

        return new LearningRouteRecord(
            path.Id,
            path.Slug,
            pathText?.Title ?? path.Slug,
            enrollment.IsCurrent,
            Percent(completedItems, totalItems),
            modules.Count(item => item.IsComplete),
            modules.Length,
            completedLessonCount,
            revision.Modules.SelectMany(item => item.Items).Count(item => item.LessonId is not null),
            completedItems,
            totalItems,
            modules);
    }

    private static LearningModuleRecord ToModuleRecord(
        LearningModule module,
        string? locale,
        IReadOnlySet<Guid> completedLessons,
        IReadOnlySet<Guid> solvedChallenges)
    {
        var text = Pick(module.Localizations, locale);
        var totalItems = module.Items.Count;
        var completedItems = module.Items.Count(item =>
            item.LessonId is { } lessonId && completedLessons.Contains(lessonId) ||
            item.ChallengeId is { } challengeId && solvedChallenges.Contains(challengeId));
        return new LearningModuleRecord(
            module.Id,
            text?.Title ?? string.Empty,
            Percent(completedItems, totalItems),
            completedItems,
            totalItems,
            totalItems > 0 && completedItems == totalItems);
    }

    private static IReadOnlyList<LearningActivityRecord> BuildRecentActivity(
        IReadOnlyList<LessonProgress> lessons,
        IReadOnlyList<ChallengeProgress> challenges,
        string? locale) =>
        lessons.Select(item => new LearningActivityRecord(
                "lesson", item.LessonId, item.CompletedAtUtc, null,
                PickLesson(item.Lesson?.Localizations, locale)?.Title))
            .Concat(challenges.Select(item => new LearningActivityRecord(
                "challenge", item.ChallengeId, item.SolvedAtUtc, item.SolveMode.ToString(),
                PickChallenge(item.Challenge?.Localizations, locale)?.Title)))
            .OrderByDescending(item => item.CompletedAtUtc)
            .Take(RecentActivityLimit)
            .ToArray();

    private static double Percent(int completed, int total) =>
        total == 0 ? 0 : Math.Round(completed * 100d / total, 2);

    private static LearningPathLocalization? Pick(
        IEnumerable<LearningPathLocalization> localizations, string? locale)
    {
        var values = localizations.ToArray();
        return values.FirstOrDefault(item => string.Equals(item.Locale, locale, StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault(item => string.Equals(item.Locale, "en", StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault();
    }

    private static LearningModuleLocalization? Pick(
        IEnumerable<LearningModuleLocalization> localizations, string? locale)
    {
        var values = localizations.ToArray();
        return values.FirstOrDefault(item => string.Equals(item.Locale, locale, StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault(item => string.Equals(item.Locale, "en", StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault();
    }

    private static LessonLocalization? PickLesson(
        IEnumerable<LessonLocalization>? localizations, string? locale)
    {
        var values = localizations?.ToArray() ?? [];
        return values.FirstOrDefault(item => string.Equals(item.Locale, locale, StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault(item => string.Equals(item.Locale, "en", StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault();
    }

    private static ChallengeLocalization? PickChallenge(
        IEnumerable<ChallengeLocalization>? localizations, string? locale)
    {
        var values = localizations?.ToArray() ?? [];
        return values.FirstOrDefault(item => string.Equals(item.Locale, locale, StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault(item => string.Equals(item.Locale, "en", StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault();
    }
}

public sealed record MyLearningResponse(
    IReadOnlyList<LearningRouteRecord> Routes,
    Guid? CurrentPathId,
    IReadOnlyList<Guid> CompletedLessonIds,
    IReadOnlyList<LearningChallengeRecord> SolvedChallenges,
    int SolvedChallengeCount,
    IReadOnlyList<LearningActivityRecord> RecentActivity);

public sealed record LearningRouteRecord(
    Guid PathId,
    string Slug,
    string Title,
    bool IsCurrent,
    double ProgressPercent,
    int CompletedModules,
    int TotalModules,
    int CompletedLessons,
    int TotalLessons,
    int CompletedItems,
    int TotalItems,
    IReadOnlyList<LearningModuleRecord> Modules);

public sealed record LearningModuleRecord(
    Guid ModuleId,
    string Title,
    double ProgressPercent,
    int CompletedItems,
    int TotalItems,
    bool IsComplete);

public sealed record LearningChallengeRecord(
    Guid ChallengeId,
    DateTimeOffset SolvedAtUtc,
    string SolveMode);

public sealed record LearningActivityRecord(
    string Kind,
    Guid ContentId,
    DateTimeOffset CompletedAtUtc,
    string? SolveMode,
    string? Title);
