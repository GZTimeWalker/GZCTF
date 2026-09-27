using GZCTF.Features.SkillTrees.Domain;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.SkillTrees.Application;

public sealed class LearningRedirectService(AppDbContext db)
{
    public async Task<string?> ResolveAsync(string oldSlug, string? moduleId, string? itemId, CancellationToken token)
    {
        var redirect = await db.LearningPathRedirects
            .AsNoTracking()
            .Include(r => r.SkillTree)
            .FirstOrDefaultAsync(r => r.OldSlug == oldSlug, token);

        if (redirect is null || redirect.SkillTree.DeletedAtUtc != null)
            return null;

        var treeId = redirect.SkillTreeId;

        if (string.IsNullOrWhiteSpace(moduleId) && string.IsNullOrWhiteSpace(itemId))
            return $"/skill-trees/{treeId}";

        if (string.IsNullOrWhiteSpace(moduleId) || string.IsNullOrWhiteSpace(itemId))
            return null;

        if (!Guid.TryParse(moduleId, out var categoryGuid) || !Guid.TryParse(itemId, out var contentGuid))
            return null;

        var belongsToTree = await db.SkillTreeCategoryRefs
            .AsNoTracking()
            .AnyAsync(cr => cr.Revision.SkillTreeId == treeId && cr.CategoryId == categoryGuid, token);

        if (!belongsToTree)
            return null;

        // Legacy deep links address content by its own entity id, matching the workspace route.
        var content = await db.CategoryContents
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == categoryGuid &&
                                       (c.ChallengeId == contentGuid || c.LessonId == contentGuid), token);

        if (content is null)
            return null;

        var kind = content.ChallengeId is not null ? "challenge"
            : content.LessonId is not null ? "lesson"
            : null;

        if (kind is null)
            return null;

        // The workspace route addresses content by its own entity id, not by the
        // CategoryContent row, so the deep link must carry the challenge or lesson id.
        var contentId = content.ChallengeId ?? content.LessonId;
        if (contentId is null)
            return null;

        return $"/skill-trees/{treeId}/{categoryGuid}/{kind}/{contentId}";
    }
}
