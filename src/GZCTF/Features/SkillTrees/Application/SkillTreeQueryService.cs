using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.SkillTrees.Application;

public sealed class SkillTreeQueryService(AppDbContext db)
{
    public async Task<IReadOnlyList<SkillTreeSummaryResponse>> ListPublishedAsync(CancellationToken token)
    {
        return await db.SkillTrees
            .AsNoTracking()
            .Where(tree => tree.DeletedAtUtc == null
                           && tree.CurrentPublishedRevisionId != null
                           && tree.CurrentPublishedRevision != null)
            .Select(tree => new SkillTreeSummaryResponse(
                tree.Id,
                tree.Name,
                tree.Summary,
                tree.IconKey,
                tree.CurrentPublishedRevision.Categories.Count(cat => cat.Category.DeletedAtUtc == null),
                tree.CurrentPublishedRevision.Categories
                    .Where(cat => cat.Category.DeletedAtUtc == null)
                    .SelectMany(cat => cat.Category.Contents)
                    .Where(cc => cc.ChallengeId != null
                                 && cc.Challenge!.PublicationState == ChallengePublicationState.Published
                                 && cc.Challenge.IsEnabled)
                    .Select(cc => cc.ChallengeId.Value)
                    .Distinct()
                    .Count(),
                tree.CurrentPublishedRevision.Categories
                    .Where(cat => cat.Category.DeletedAtUtc == null)
                    .SelectMany(cat => cat.Category.Contents)
                    .Where(cc => cc.LessonId != null
                                 && cc.Lesson!.PublicationState == LessonPublicationState.Published)
                    .Select(cc => cc.LessonId.Value)
                    .Distinct()
                    .Count()))
            .ToListAsync(token);
    }

    public async Task<SkillTreeDetailResponse?> GetPublishedAsync(Guid id, CancellationToken token)
    {
        var tree = await db.SkillTrees
            .AsNoTracking()
            .Include(t => t.CurrentPublishedRevision!)
                .ThenInclude(r => r.Categories)
                    .ThenInclude(cr => cr.Category)
            .FirstOrDefaultAsync(t => t.Id == id
                                      && t.DeletedAtUtc == null
                                      && t.CurrentPublishedRevisionId != null
                                      && t.CurrentPublishedRevision != null, token);

        if (tree is null)
            return null;

        var published = tree.CurrentPublishedRevision;
        var activeRefs = published.Categories
            .Where(cr => cr.Category.DeletedAtUtc == null)
            .OrderBy(cr => cr.SortOrder)
            .ToList();

        var categoryIds = activeRefs.Select(cr => cr.CategoryId).ToList();

        var contents = await db.CategoryContents
            .AsNoTracking()
            .Where(cc => categoryIds.Contains(cc.CategoryId))
            .OrderBy(cc => cc.CategoryId)
            .ThenBy(cc => cc.SortOrder)
            .ToListAsync(token);

        var challengeIds = contents.Where(cc => cc.ChallengeId is not null).Select(cc => cc.ChallengeId!.Value).Distinct().ToList();
        var lessonIds = contents.Where(cc => cc.LessonId is not null).Select(cc => cc.LessonId!.Value).Distinct().ToList();

        var challenges = await db.Challenges
            .AsNoTracking()
            .Include(c => c.Localizations)
            .Where(c => challengeIds.Contains(c.Id))
            .ToListAsync(token);

        var lessons = await db.Lessons
            .AsNoTracking()
            .Include(l => l.Localizations)
            .Where(l => lessonIds.Contains(l.Id))
            .ToListAsync(token);

        var challengeMap = challenges.ToDictionary(c => c.Id);
        var lessonMap = lessons.ToDictionary(l => l.Id);

        var publishedContents = contents
            .Where(cc => (cc.ChallengeId is not null
                           && challengeMap.TryGetValue(cc.ChallengeId!.Value, out var ch)
                           && ch.PublicationState == ChallengePublicationState.Published
                           && ch.IsEnabled)
                         || (cc.LessonId is not null
                             && lessonMap.TryGetValue(cc.LessonId!.Value, out var ls)
                             && ls.PublicationState == LessonPublicationState.Published))
            .ToList();

        var contentsByCategory = publishedContents
            .GroupBy(cc => cc.CategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var categories = activeRefs.Select(cr =>
        {
            var list = contentsByCategory.TryGetValue(cr.CategoryId, out var items) ? items : [];
            var summaries = list.Select(cc =>
            {
                if (cc.ChallengeId is { } cid && challengeMap.TryGetValue(cid, out var challenge))
                {
                    return new SkillTreeContentSummaryResponse(
                        cid, "challenge", cc.SortOrder,
                        PickTitle(challenge.Localizations) ?? string.Empty,
                        PickSummary(challenge.Localizations) ?? string.Empty,
                        challenge.ExpectedMinutes,
                        challenge.Difficulty.ToString());
                }

                if (cc.LessonId is { } lid && lessonMap.TryGetValue(lid, out var lesson))
                {
                    return new SkillTreeContentSummaryResponse(
                        lid, "lesson", cc.SortOrder,
                        PickTitle(lesson.Localizations) ?? string.Empty,
                        string.Empty, 0, string.Empty);
                }

                return new SkillTreeContentSummaryResponse(cc.Id, "unknown", cc.SortOrder, string.Empty, string.Empty, 0, string.Empty);
            }).ToArray();
            return new SkillCategoryPublicResponse(
                cr.CategoryId, cr.Category.Name, cr.Category.Summary, cr.Category.IconKey, cr.SortOrder, summaries);
        }).ToArray();

        return new SkillTreeDetailResponse(tree.Id, tree.Name, tree.Summary, tree.IconKey, categories);
    }

    private static T? PickText<T>(IEnumerable<T>? values, Func<T, string> locale) where T : class
    {
        if (values is null) return null;
        var list = values.ToList();
        return list.FirstOrDefault(x => locale(x).Equals("en", StringComparison.OrdinalIgnoreCase))
            ?? list.FirstOrDefault(x => locale(x).Equals("zh-CN", StringComparison.OrdinalIgnoreCase))
            ?? list.FirstOrDefault();
    }

    private static string PickTitle(IEnumerable<ChallengeLocalization>? localizations) =>
        PickText(localizations, x => x.Locale)?.Title ?? string.Empty;

    private static string PickTitle(IEnumerable<LessonLocalization>? localizations) =>
        PickText(localizations, x => x.Locale)?.Title ?? string.Empty;

    private static string PickSummary(IEnumerable<ChallengeLocalization>? localizations) =>
        PickText(localizations, x => x.Locale)?.Summary ?? string.Empty;
}
