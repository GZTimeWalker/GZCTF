using System.Data;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Models;
using GZCTF.Storage.Interface;
using Microsoft.EntityFrameworkCore;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Features.SkillTrees.Application;

public sealed class ContentPublicationService(
    AppDbContext db, ISkillTreeCacheInvalidator cacheInvalidator, IBlobStorage storage)
{
    public async Task PublishChallengeAsync(
        Guid challengeId, PublishContentCommand command, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);

        var challenge = await db.Challenges
            .Include(item => item.Localizations)
            .Include(item => item.Flags)
            .FirstOrDefaultAsync(item => item.Id == challengeId, token)
            ?? throw new ContentPublicationValidationException("The challenge was not found.");

        if (challenge.PublicationState is ChallengePublicationState.Retired or ChallengePublicationState.Merged)
            throw new ContentPublicationValidationException("A retired challenge cannot be published.");
        if (command.RowVersion != challenge.RowVersion)
            throw new SkillTreeRevisionConflictException();
        if (!HasPublishableText(challenge.Localizations.Select(item => item.Title)))
            throw new ContentPublicationValidationException("The challenge needs a localized title before publication.");
        await ChallengePublicationValidator.ValidateAsync(challenge, storage, token);

        var categoryIds = await ResolveCategoriesAsync(command, token);

        await db.CategoryContents
            .Where(item => item.ChallengeId == challengeId)
            .ExecuteDeleteAsync(token);

        var nextOrders = await LoadNextOrdersAsync(categoryIds, token);
        foreach (var categoryId in categoryIds)
        {
            db.CategoryContents.Add(new CategoryContent
            {
                CategoryId = categoryId,
                SortOrder = nextOrders[categoryId]++,
                ChallengeId = challengeId
            });
        }

        challenge.PublicationState = ChallengePublicationState.Published;

        try
        {
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new SkillTreeRevisionConflictException();
        }

        foreach (var categoryId in categoryIds)
            await cacheInvalidator.InvalidateByCategoryAsync(categoryId, token);
    }

    public async Task PublishLessonAsync(
        Guid lessonId, PublishContentCommand command, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);

        var lesson = await db.Lessons
            .Include(item => item.Localizations)
            .FirstOrDefaultAsync(item => item.Id == lessonId, token)
            ?? throw new ContentPublicationValidationException("The lesson was not found.");

        if (lesson.PublicationState == LessonPublicationState.Retired)
            throw new ContentPublicationValidationException("A retired lesson cannot be published.");
        if (command.RowVersion != lesson.RowVersion)
            throw new SkillTreeRevisionConflictException();
        if (!HasPublishableText(lesson.Localizations.Select(item => item.Title)))
            throw new ContentPublicationValidationException("The lesson needs a localized title before publication.");

        var categoryIds = await ResolveCategoriesAsync(command, token);

        await db.CategoryContents
            .Where(item => item.LessonId == lessonId)
            .ExecuteDeleteAsync(token);

        var nextOrders = await LoadNextOrdersAsync(categoryIds, token);
        foreach (var categoryId in categoryIds)
        {
            db.CategoryContents.Add(new CategoryContent
            {
                CategoryId = categoryId,
                SortOrder = nextOrders[categoryId]++,
                LessonId = lessonId
            });
        }

        lesson.PublicationState = LessonPublicationState.Published;

        try
        {
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new SkillTreeRevisionConflictException();
        }

        foreach (var categoryId in categoryIds)
            await cacheInvalidator.InvalidateByCategoryAsync(categoryId, token);
    }

    private async Task<IReadOnlyList<Guid>> ResolveCategoriesAsync(
        PublishContentCommand command, CancellationToken token)
    {
        var orderedCategoryIds = new List<Guid>();
        foreach (var categoryId in command.CategoryIds ?? [])
            if (categoryId != Guid.Empty && !orderedCategoryIds.Contains(categoryId))
                orderedCategoryIds.Add(categoryId);

        var inlineCategories = command.InlineCategories ?? [];
        if (inlineCategories.Count > 0)
        {
            var treeIds = inlineCategories.Select(item => item.SkillTreeId).Distinct().ToList();
            var trees = await db.SkillTrees
                .Include(tree => tree.Revisions)
                    .ThenInclude(revision => revision.Categories)
                .Where(tree => treeIds.Contains(tree.Id) && tree.DeletedAtUtc == null)
                .ToListAsync(token);

            if (trees.Count != treeIds.Count)
                throw new ContentPublicationValidationException("An inline category targets an unknown skill tree.");

            foreach (var inline in inlineCategories)
            {
                var tree = trees.Single(item => item.Id == inline.SkillTreeId);
                var draft = tree.Revisions.SingleOrDefault(revision =>
                    revision.Status == SkillTreeRevisionStatus.Draft);

                if (draft is null)
                {
                    draft = ClonePublishedRevision(tree);
                    tree.Revisions.Add(draft);
                    // Entry(...) below would force-attach the fresh clone as an existing row
                    // and turn the insert into a zero-row xmin update, so track it as added.
                    db.Add(draft);
                }

                var category = new SkillCategory
                {
                    Name = ValidateInlineName(inline.Name),
                    Summary = (inline.Summary ?? string.Empty).Trim(),
                    IconKey = SkillTreeIconCatalog.IsSupported(inline.IconKey)
                        ? inline.IconKey
                        : SkillTreeIconCatalog.Default
                };
                db.SkillCategories.Add(category);

                draft.Categories.Add(new SkillTreeCategoryRef
                {
                    Revision = draft,
                    Category = category,
                    CategoryId = category.Id,
                    SortOrder = draft.Categories.Count
                });

                if (tree.CurrentPublishedRevisionId is null)
                {
                    draft.Status = SkillTreeRevisionStatus.Published;
                    draft.PublishedAtUtc = DateTimeOffset.UtcNow;
                    tree.CurrentPublishedRevisionId = draft.Id;
                    tree.CurrentPublishedRevision = draft;
                }
                else
                {
                    db.Entry(draft).Property(revision => revision.Status).IsModified = true;
                }

                if (!orderedCategoryIds.Contains(category.Id))
                    orderedCategoryIds.Add(category.Id);
            }

            // Persist inline categories and draft references so the validation queries below can see them.
            await db.SaveChangesAsync(token);
        }

        if (orderedCategoryIds.Count == 0)
            throw new ContentCategoryRequiredException();

        var activeCategories = await db.SkillCategories
            .Where(category => orderedCategoryIds.Contains(category.Id) && category.DeletedAtUtc == null)
            .Select(category => category.Id)
            .ToListAsync(token);

        if (activeCategories.Count != orderedCategoryIds.Count)
            throw new ContentPublicationValidationException("One or more categories are not active.");

        var categoriesWithTree = await db.SkillTreeCategoryRefs
            .Where(reference => orderedCategoryIds.Contains(reference.CategoryId) &&
                                reference.Revision.SkillTree.DeletedAtUtc == null &&
                                (reference.Revision.Status == SkillTreeRevisionStatus.Draft ||
                                 reference.Revision.SkillTree.CurrentPublishedRevisionId == reference.RevisionId))
            .Select(reference => reference.CategoryId)
            .Distinct()
            .ToListAsync(token);

        if (orderedCategoryIds.Any(categoryId => !categoriesWithTree.Contains(categoryId)))
            throw new ContentCategoryHasNoTreeException();

        return orderedCategoryIds;
    }

    private async Task<Dictionary<Guid, int>> LoadNextOrdersAsync(
        IReadOnlyList<Guid> categoryIds, CancellationToken token)
    {
        var existing = await db.CategoryContents
            .Where(item => categoryIds.Contains(item.CategoryId))
            .GroupBy(item => item.CategoryId)
            .Select(group => new { CategoryId = group.Key, Max = group.Max(item => item.SortOrder) })
            .ToListAsync(token);

        return categoryIds.ToDictionary(
            categoryId => categoryId,
            categoryId => existing.FirstOrDefault(item => item.CategoryId == categoryId)?.Max + 1 ?? 0);
    }

    private static SkillTreeRevision ClonePublishedRevision(SkillTree tree)
    {
        var published = tree.Revisions
            .OrderByDescending(revision => revision.PublishedAtUtc ?? revision.CreatedAtUtc)
            .FirstOrDefault(revision => revision.Status == SkillTreeRevisionStatus.Published);

        var draft = new SkillTreeRevision
        {
            SkillTreeId = tree.Id,
            SkillTree = tree,
            Status = SkillTreeRevisionStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (published is not null)
            foreach (var reference in published.Categories.OrderBy(item => item.SortOrder))
                draft.Categories.Add(new SkillTreeCategoryRef
                {
                    Revision = draft,
                    CategoryId = reference.CategoryId,
                    SortOrder = reference.SortOrder
                });

        return draft;
    }

    private static bool HasPublishableText(IEnumerable<string> titles) =>
        titles.Any(title => !string.IsNullOrWhiteSpace(title));

    private static string ValidateInlineName(string? value)
    {
        var name = (value ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ContentPublicationValidationException("An inline category needs a name.");
        if (name.Length > 128)
            throw new ContentPublicationValidationException("An inline category name is too long.");
        return name;
    }

    public static async Task<IReadOnlyList<Guid>> LoadContentCategoryIdsAsync(
        AppDbContext db, Guid? challengeId, Guid? lessonId, CancellationToken token) =>
        await db.CategoryContents
            .AsNoTracking()
            .Where(item => (challengeId != null && item.ChallengeId == challengeId) ||
                           (lessonId != null && item.LessonId == lessonId))
            .OrderBy(item => item.SortOrder)
            .Select(item => item.CategoryId)
            .Distinct()
            .ToListAsync(token);
}
