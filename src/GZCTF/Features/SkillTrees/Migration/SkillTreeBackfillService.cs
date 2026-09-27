using System.Data;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GZCTF.Features.SkillTrees.Migration;

/// <summary>
/// Idempotently copies the currently implemented LearningPath graph into the new SkillTree
/// schema using stable source IDs. Current Learning APIs and tables stay untouched, so this
/// runs beside them until a later wave performs the cutover.
/// </summary>
public sealed class SkillTreeBackfillService(
    AppDbContext db,
    ILogger<SkillTreeBackfillService> logger)
{
    public async Task<SkillTreeBackfillResult> RunAsync(CancellationToken token = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, token);

        var paths = await db.LearningPaths
            .AsSplitQuery()
            .Include(path => path.Localizations)
            .Include(path => path.Revisions)
                .ThenInclude(revision => revision.Modules)
                    .ThenInclude(module => module.Localizations)
            .Include(path => path.Revisions)
                .ThenInclude(revision => revision.Modules)
                    .ThenInclude(module => module.Items)
            .Include(path => path.Enrollments)
            .ToListAsync(token);

        var trees = 0;
        var revisions = 0;
        var categories = 0;
        var contents = 0;
        var enrollments = 0;
        var redirects = 0;

        foreach (var path in paths)
        {
            // A redirect marks an already backfilled path; skip it so reruns stay idempotent.
            if (await db.LearningPathRedirects.AnyAsync(item => item.LearningPathId == path.Id, token))
                continue;

            var pathText = PickText(path.Localizations, item => item.Locale);

            if (!await db.SkillTrees.AnyAsync(item => item.Id == path.Id, token))
            {
                db.SkillTrees.Add(new SkillTree
                {
                    Id = path.Id,
                    Name = pathText?.Title ?? path.Slug,
                    Summary = pathText?.Summary ?? string.Empty,
                    IconKey = SkillTreeIconCatalog.Default,
                    CreatedAtUtc = path.CreatedAtUtc,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                    CurrentPublishedRevisionId = null
                });
                trees++;
            }

            foreach (var revision in path.Revisions)
            {
                if (await db.SkillTreeRevisions.AnyAsync(item => item.Id == revision.Id, token))
                    continue;

                db.SkillTreeRevisions.Add(new SkillTreeRevision
                {
                    Id = revision.Id,
                    SkillTreeId = path.Id,
                    Status = (SkillTreeRevisionStatus)revision.Status,
                    CreatedAtUtc = revision.CreatedAtUtc,
                    PublishedAtUtc = revision.PublishedAtUtc
                });
                revisions++;

                foreach (var module in revision.Modules)
                {
                    var moduleText = PickText(module.Localizations, item => item.Locale);

                    if (!await db.SkillCategories.AnyAsync(item => item.Id == module.Id, token))
                    {
                        db.SkillCategories.Add(new SkillCategory
                        {
                            Id = module.Id,
                            Name = moduleText?.Title ?? string.Empty,
                            Summary = moduleText?.Summary ?? string.Empty,
                            IconKey = SkillTreeIconCatalog.Default,
                            CreatedAtUtc = DateTimeOffset.UtcNow,
                            UpdatedAtUtc = DateTimeOffset.UtcNow
                        });
                        categories++;
                    }

                    if (!await db.SkillTreeCategoryRefs.AnyAsync(item => item.Id == module.Id, token))
                    {
                        db.SkillTreeCategoryRefs.Add(new SkillTreeCategoryRef
                        {
                            Id = module.Id,
                            RevisionId = revision.Id,
                            CategoryId = module.Id,
                            SortOrder = module.SortOrder
                        });
                    }

                    foreach (var item in module.Items)
                    {
                        if (await db.CategoryContents.AnyAsync(content => content.Id == item.Id, token))
                            continue;

                        db.CategoryContents.Add(new CategoryContent
                        {
                            Id = item.Id,
                            CategoryId = module.Id,
                            SortOrder = item.SortOrder,
                            LessonId = item.LessonId,
                            ChallengeId = item.ChallengeId,
                            CreatedAtUtc = DateTimeOffset.UtcNow
                        });
                        contents++;
                    }
                }
            }

            foreach (var enrollment in path.Enrollments)
            {
                if (await db.SkillTreeEnrollments.AnyAsync(item => item.Id == enrollment.Id, token))
                    continue;

                db.SkillTreeEnrollments.Add(new SkillTreeEnrollment
                {
                    Id = enrollment.Id,
                    UserId = enrollment.UserId,
                    SkillTreeId = path.Id,
                    IsCurrent = enrollment.IsCurrent,
                    EnrolledAtUtc = enrollment.EnrolledAtUtc
                });
                enrollments++;
            }

            if (!await db.LearningPathRedirects.AnyAsync(item => item.Id == path.Id, token))
            {
                db.LearningPathRedirects.Add(new LearningPathRedirect
                {
                    Id = path.Id,
                    LearningPathId = path.Id,
                    OldSlug = path.Slug,
                    SkillTreeId = path.Id
                });
                redirects++;
            }
        }

        await db.SaveChangesAsync(token);

        // Now that the copied graph exists, point each tree at its copied published revision.
        foreach (var path in paths)
        {
            if (path.CurrentPublishedRevisionId is not { } publishedRevisionId)
                continue;

            var tree = await db.SkillTrees.FindAsync(new object[] { path.Id }, token);
            if (tree is null || tree.CurrentPublishedRevisionId == publishedRevisionId)
                continue;

            tree.CurrentPublishedRevisionId = publishedRevisionId;
            tree.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);

        var result = new SkillTreeBackfillResult(trees, revisions, categories, contents, enrollments,
            redirects);
        logger.LogInformation(
            "Skill tree backfill inserted {Trees} trees, {Revisions} revisions, {Categories} categories, " +
            "{Contents} contents, {Enrollments} enrollments and {Redirects} redirects",
            result.Trees, result.Revisions, result.Categories, result.Contents, result.Enrollments,
            result.Redirects);

        return result;
    }

    private static T? PickText<T>(IEnumerable<T> values, Func<T, string> locale) =>
        values.FirstOrDefault(x => locale(x).Equals("zh-CN", StringComparison.OrdinalIgnoreCase))
        ?? values.FirstOrDefault(x => locale(x).Equals("en", StringComparison.OrdinalIgnoreCase))
        ?? values.FirstOrDefault();
}

public sealed record SkillTreeBackfillResult(
    int Trees,
    int Revisions,
    int Categories,
    int Contents,
    int Enrollments,
    int Redirects);
