using System.Data;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.SkillTrees.Application;

public sealed class SkillTreeEnrollmentService(AppDbContext db)
{
    private const int RecentActivityLimit = 20;

    public async Task<IReadOnlyList<SkillTreeEnrollmentResponse>> ListAsync(
        Guid userId, CancellationToken token)
    {
        return await db.SkillTreeEnrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.UserId == userId &&
                                 enrollment.SkillTree.DeletedAtUtc == null)
            .OrderBy(enrollment => enrollment.EnrolledAtUtc)
            .Select(enrollment => new SkillTreeEnrollmentResponse(
                enrollment.Id,
                enrollment.SkillTreeId,
                enrollment.SkillTree.Name,
                enrollment.SkillTree.IconKey,
                enrollment.IsCurrent,
                enrollment.EnrolledAtUtc))
            .ToListAsync(token);
    }

    public async Task<SkillTreeEnrollmentResponse?> EnrollAsync(
        Guid userId, Guid skillTreeId, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);

        var tree = await db.SkillTrees
            .FirstOrDefaultAsync(item => item.Id == skillTreeId &&
                                          item.DeletedAtUtc == null &&
                                          item.CurrentPublishedRevisionId != null, token);
        if (tree is null)
            return null;

        var enrollment = await db.SkillTreeEnrollments
            .FirstOrDefaultAsync(item => item.UserId == userId && item.SkillTreeId == skillTreeId, token);

        if (enrollment is null)
        {
            var hasCurrent = await db.SkillTreeEnrollments
                .AnyAsync(item => item.UserId == userId && item.IsCurrent &&
                                  item.SkillTree.DeletedAtUtc == null, token);
            enrollment = new SkillTreeEnrollment
            {
                UserId = userId,
                SkillTreeId = skillTreeId,
                IsCurrent = !hasCurrent
            };
            db.SkillTreeEnrollments.Add(enrollment);
        }
        else if (!enrollment.IsCurrent)
        {
            var hasCurrent = await db.SkillTreeEnrollments
                .AnyAsync(item => item.UserId == userId && item.IsCurrent &&
                                  item.SkillTree.DeletedAtUtc == null, token);
            if (!hasCurrent)
                enrollment.IsCurrent = true;
        }

        try
        {
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new SkillTreeRevisionConflictException();
        }

        return new SkillTreeEnrollmentResponse(
            enrollment.Id, tree.Id, tree.Name, tree.IconKey, enrollment.IsCurrent, enrollment.EnrolledAtUtc);
    }

    public async Task<bool> LeaveAsync(Guid userId, Guid skillTreeId, CancellationToken token)
    {
        var enrollment = await db.SkillTreeEnrollments
            .FirstOrDefaultAsync(item => item.UserId == userId && item.SkillTreeId == skillTreeId, token);
        if (enrollment is null)
            return false;

        db.SkillTreeEnrollments.Remove(enrollment);
        await db.SaveChangesAsync(token);
        return true;
    }

    public async Task<SkillTreeEnrollmentResponse?> SelectCurrentAsync(
        Guid userId, Guid skillTreeId, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);

        var enrollment = await db.SkillTreeEnrollments
            .Include(item => item.SkillTree)
            .FirstOrDefaultAsync(item => item.UserId == userId && item.SkillTreeId == skillTreeId, token);
        if (enrollment is null)
            return null;

        await db.SkillTreeEnrollments
            .Where(item => item.UserId == userId && item.IsCurrent && item.Id != enrollment.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsCurrent, false), token);
        enrollment.IsCurrent = true;

        try
        {
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new SkillTreeRevisionConflictException();
        }

        return new SkillTreeEnrollmentResponse(
            enrollment.Id, enrollment.SkillTreeId, enrollment.SkillTree.Name,
            enrollment.SkillTree.IconKey, enrollment.IsCurrent, enrollment.EnrolledAtUtc);
    }

    public async Task<MyLearningResponse> GetMyLearningAsync(Guid userId, CancellationToken token)
    {
        var enrollments = await db.SkillTreeEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.SkillTree)
                .ThenInclude(tree => tree.Revisions)
                    .ThenInclude(revision => revision.Categories)
                        .ThenInclude(reference => reference.Category)
            .Where(enrollment => enrollment.UserId == userId)
            .OrderBy(enrollment => enrollment.EnrolledAtUtc)
            .ToListAsync(token);

        var revisions = enrollments
            .Select(enrollment => (Enrollment: enrollment, Revision: ResolveRevision(enrollment.SkillTree)))
            .Where(item => item.Revision is not null)
            .ToList();

        var categoryIds = revisions
            .SelectMany(item => item.Revision!.Categories
                .Where(reference => reference.Category.DeletedAtUtc == null)
                .Select(reference => reference.CategoryId))
            .Distinct()
            .ToList();

        var contents = await db.CategoryContents
            .AsNoTracking()
            .Where(content => categoryIds.Contains(content.CategoryId))
            .ToListAsync(token);

        var challengeIds = contents
            .Where(content => content.ChallengeId is not null)
            .Select(content => content.ChallengeId!.Value).Distinct().ToList();
        var lessonIds = contents
            .Where(content => content.LessonId is not null)
            .Select(content => content.LessonId!.Value).Distinct().ToList();

        var challenges = await db.Challenges
            .AsNoTracking()
            .Where(challenge => challengeIds.Contains(challenge.Id))
            .Select(challenge => new
            {
                challenge.Id,
                challenge.PublicationState,
                challenge.IsEnabled
            })
            .ToDictionaryAsync(item => item.Id, token);
        var lessons = await db.Lessons
            .AsNoTracking()
            .Where(lesson => lessonIds.Contains(lesson.Id))
            .Select(lesson => new { lesson.Id, lesson.PublicationState })
            .ToDictionaryAsync(item => item.Id, token);

        var activeContents = contents.Where(content =>
            content.ChallengeId is { } challengeId &&
            challenges.TryGetValue(challengeId, out var challenge) &&
            challenge.PublicationState == ChallengePublicationState.Published && challenge.IsEnabled ||
            content.LessonId is { } lessonId &&
            lessons.TryGetValue(lessonId, out var lesson) &&
            lesson.PublicationState == LessonPublicationState.Published).ToList();

        var completedChallenges = (await db.ChallengeProgress
            .AsNoTracking()
            .Where(progress => progress.UserId == userId)
            .Select(progress => progress.ChallengeId)
            .ToHashSetAsync(token));
        var completedLessons = (await db.LessonProgress
            .AsNoTracking()
            .Where(progress => progress.UserId == userId)
            .Select(progress => progress.LessonId)
            .ToHashSetAsync(token));

        var records = revisions.Select(item =>
        {
            var tree = item.Enrollment.SkillTree;
            var revision = item.Revision!;
            var activeCategories = revision.Categories
                .Where(reference => reference.Category.DeletedAtUtc == null)
                .ToList();
            var treeContents = activeContents
                .Where(content => activeCategories.Any(reference => reference.CategoryId == content.CategoryId))
                .ToList();
            var treeChallengeIds = treeContents
                .Where(content => content.ChallengeId is not null)
                .Select(content => content.ChallengeId!.Value).Distinct().ToList();
            var treeLessonIds = treeContents
                .Where(content => content.LessonId is not null)
                .Select(content => content.LessonId!.Value).Distinct().ToList();

            var completedCategoryCount = activeCategories.Count(reference =>
            {
                var categoryContents = treeContents
                    .Where(content => content.CategoryId == reference.CategoryId)
                    .ToList();
                if (categoryContents.Count == 0)
                    return false;
                return categoryContents.All(content =>
                    content.ChallengeId is { } challengeId
                        ? completedChallenges.Contains(challengeId)
                        : content.LessonId is { } lessonId && completedLessons.Contains(lessonId));
            });

            return new MySkillTreeRecordResponse(
                tree.Id,
                tree.Name,
                tree.IconKey,
                item.Enrollment.IsCurrent && tree.DeletedAtUtc == null,
                tree.DeletedAtUtc != null,
                activeCategories.Count,
                completedCategoryCount,
                treeChallengeIds.Count,
                treeChallengeIds.Count(completedChallenges.Contains),
                treeLessonIds.Count,
                treeLessonIds.Count(completedLessons.Contains));
        }).ToArray();

        var currentSkillTreeId = records
            .FirstOrDefault(record => record.IsCurrent)?.SkillTreeId;

        var recentActivity = await BuildRecentActivityAsync(userId, token);

        return new MyLearningResponse(currentSkillTreeId, records, recentActivity);
    }

    private async Task<IReadOnlyList<RecentLearningActivityResponse>> BuildRecentActivityAsync(
        Guid userId, CancellationToken token)
    {
        var lessons = await db.LessonProgress
            .AsNoTracking()
            .Include(progress => progress.Lesson)
                .ThenInclude(lesson => lesson.Localizations)
            .Where(progress => progress.UserId == userId)
            .OrderByDescending(progress => progress.CompletedAtUtc)
            .Take(RecentActivityLimit)
            .ToListAsync(token);

        var challenges = await db.ChallengeProgress
            .AsNoTracking()
            .Include(progress => progress.Challenge)
                .ThenInclude(challenge => challenge.Localizations)
            .Where(progress => progress.UserId == userId)
            .OrderByDescending(progress => progress.SolvedAtUtc)
            .Take(RecentActivityLimit)
            .ToListAsync(token);

        return lessons
            .Select(progress => new RecentLearningActivityResponse(
                "lesson", progress.LessonId,
                PickTitle(progress.Lesson?.Localizations.Select(item => (item.Locale, item.Title))),
                progress.CompletedAtUtc, null))
            .Concat(challenges.Select(progress => new RecentLearningActivityResponse(
                "challenge", progress.ChallengeId,
                PickTitle(progress.Challenge?.Localizations.Select(item => (item.Locale, item.Title))),
                progress.SolvedAtUtc, progress.SolveMode.ToString())))
            .OrderByDescending(item => item.CompletedAtUtc)
            .Take(RecentActivityLimit)
            .ToArray();
    }

    private static SkillTreeRevision? ResolveRevision(SkillTree tree)
    {
        if (tree.CurrentPublishedRevisionId is { } currentId)
        {
            var current = tree.Revisions.FirstOrDefault(revision => revision.Id == currentId);
            if (current is not null)
                return current;
        }

        return tree.Revisions
            .Where(revision => revision.Status == SkillTreeRevisionStatus.Published)
            .OrderByDescending(revision => revision.PublishedAtUtc ?? revision.CreatedAtUtc)
            .FirstOrDefault();
    }

    private static string PickTitle(IEnumerable<(string Locale, string Title)>? values)
    {
        if (values is null)
            return string.Empty;
        var list = values.ToList();
        return (list.FirstOrDefault(item => item.Locale.Equals("en", StringComparison.OrdinalIgnoreCase)).Title
                ?? list.FirstOrDefault(item => item.Locale.Equals("zh-CN", StringComparison.OrdinalIgnoreCase)).Title
                ?? list.FirstOrDefault().Title) ?? string.Empty;
    }
}
