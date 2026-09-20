using System.Data;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.LearningPaths.Application;

public sealed class LearningPathService(AppDbContext db)
{
    public async Task<LearningPathDraftResponse> CreatePathAsync(
        LearningPathCommand command, CancellationToken token)
    {
        var path = await LoadPathAsync(command.Slug, token);
        if (path is null)
        {
            path = new LearningPath { Slug = command.Slug };
            ApplyPathLocalizations(path, command.Localizations);
            var revision = BuildRevision(path, command.Modules);
            path.Revisions.Add(revision);
            db.LearningPaths.Add(path);
            await db.SaveChangesAsync(token);
            return ToDraftResponse(path, revision, command.Locale);
        }

        var draft = path.Revisions.SingleOrDefault(item => item.Status == LearningPathRevisionStatus.Draft);
        if (draft is null)
        {
            draft = CopyRevision(path, path.CurrentPublishedRevision
                ?? path.Revisions.Single(item => item.Status == LearningPathRevisionStatus.Published));
            // Path-localized metadata is shared by all revisions in the foundation schema;
            // keep it unchanged once a published graph exists.
            if (path.CurrentPublishedRevisionId is null)
                ApplyPathLocalizations(path, command.Localizations);
            ReplaceRevisionGraph(draft, command.Modules);
            await db.SaveChangesAsync(token);
        }

        return ToDraftResponse(path, draft, command.Locale);
    }

    public async Task<LearningPathDraftResponse?> GetDraftAsync(
        Guid pathId, string? locale, CancellationToken token)
    {
        var path = await LoadPathAsync(pathId, token);
        if (path is null)
            return null;

        var draft = path.Revisions.SingleOrDefault(item => item.Status == LearningPathRevisionStatus.Draft);
        if (draft is null && path.CurrentPublishedRevision is not null)
        {
            draft = CopyRevision(path, path.CurrentPublishedRevision);
            await db.SaveChangesAsync(token);
        }

        return draft is null ? null : ToDraftResponse(path, draft, locale);
    }

    public async Task<LearningPathDraftResponse?> UpdateDraftAsync(
        Guid pathId, LearningPathCommand command, CancellationToken token)
    {
        var path = await LoadPathAsync(pathId, token);
        if (path is null)
            return null;

        var draft = path.Revisions.SingleOrDefault(item => item.Status == LearningPathRevisionStatus.Draft);
        if (draft is null && path.CurrentPublishedRevision is not null)
        {
            draft = CopyRevision(path, path.CurrentPublishedRevision);
            await db.SaveChangesAsync(token);
        }

        if (draft is null)
            return null;
        EnsureRowVersion(draft, command.RowVersion);

        if (path.CurrentPublishedRevisionId is null && !string.IsNullOrWhiteSpace(command.Slug))
            path.Slug = command.Slug;
        if (path.CurrentPublishedRevisionId is null)
            ApplyPathLocalizations(path, command.Localizations);
        ReplaceRevisionGraph(draft, command.Modules);

        // Touch the revision row so its PostgreSQL xmin changes when the graph changes.
        db.Entry(draft).Property(item => item.Status).IsModified = true;
        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LearningRevisionConflictException();
        }

        return ToDraftResponse(path, draft, command.Locale);
    }

    public async Task PublishAsync(Guid pathId, uint? rowVersion, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, token);

        // Serializable isolation prevents two publishers from committing a stale pointer.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT \"Id\" FROM \"LearningPaths\" WHERE \"Id\" = {pathId} FOR UPDATE", token);

        var path = await LoadPathAsync(pathId, token);
        if (path is null)
            throw new LearningPathNotFoundException();
        var draft = path.Revisions.SingleOrDefault(item => item.Status == LearningPathRevisionStatus.Draft);
        if (draft is null)
            throw new LearningPathValidationException("The path does not have a draft revision.");

        EnsureRowVersion(draft, rowVersion);
        await ValidatePublishableAsync(path, draft, token);

        draft.Status = LearningPathRevisionStatus.Published;
        draft.PublishedAtUtc = DateTimeOffset.UtcNow;
        path.CurrentPublishedRevisionId = draft.Id;
        path.CurrentPublishedRevision = draft;
        try
        {
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LearningRevisionConflictException();
        }
    }

    public async Task<IReadOnlyList<LearningPathSummaryResponse>> ListPublishedAsync(
        string? locale, CancellationToken token)
    {
        var paths = await db.LearningPaths
            .AsNoTracking()
            .Include(item => item.Localizations)
            .Include(item => item.CurrentPublishedRevision!)
                .ThenInclude(item => item.Modules)
                    .ThenInclude(item => item.Items)
            .Where(item => item.CurrentPublishedRevisionId != null && item.CurrentPublishedRevision != null)
            .OrderBy(item => item.Slug)
            .ToListAsync(token);

        return paths.Select(path =>
        {
            var text = Pick(path.Localizations, locale);
            var revision = path.CurrentPublishedRevision!;
            return new LearningPathSummaryResponse(
                path.Id, path.Slug, text?.Title ?? path.Slug, text?.Summary ?? string.Empty,
                revision.Modules.Count, revision.Modules.Sum(item => item.Items.Count),
                revision.Modules.Sum(item => item.ExpectedMinutes));
        }).ToArray();
    }

    public async Task<LearningPathPreviewResponse?> GetPublishedPreviewAsync(
        string slug, string? locale, CancellationToken token)
    {
        var path = await LoadPathAsync(slug, token, publishedOnly: true);
        if (path?.CurrentPublishedRevision is null)
            return null;
        return ToPreviewResponse(path, path.CurrentPublishedRevision, locale);
    }

    public async Task<LearningPathPreviewResponse?> GetDraftPreviewAsync(
        Guid pathId, string? locale, CancellationToken token)
    {
        var path = await LoadPathAsync(pathId, token);
        if (path is null)
            return null;
        var revision = path.Revisions.SingleOrDefault(item => item.Status == LearningPathRevisionStatus.Draft);
        return revision is null ? null : ToPreviewResponse(path, revision, locale);
    }

    private async Task<LearningPath?> LoadPathAsync(string slug, CancellationToken token, bool publishedOnly = false) =>
        await LoadPathQuery(publishedOnly).SingleOrDefaultAsync(item => item.Slug == slug, token);

    private async Task<LearningPath?> LoadPathAsync(Guid id, CancellationToken token) =>
        await LoadPathQuery().SingleOrDefaultAsync(item => item.Id == id, token);

    private IQueryable<LearningPath> LoadPathQuery(bool publishedOnly = false)
    {
        var query = db.LearningPaths
            .Include(item => item.Localizations)
            .Include(item => item.Revisions)
                .ThenInclude(item => item.Modules)
                    .ThenInclude(item => item.Localizations)
            .Include(item => item.Revisions)
                .ThenInclude(item => item.Modules)
                    .ThenInclude(item => item.Items)
                        .ThenInclude(item => item.Challenge)
                            .ThenInclude(item => item!.Localizations)
            .Include(item => item.Revisions)
                .ThenInclude(item => item.Modules)
                    .ThenInclude(item => item.Items)
                        .ThenInclude(item => item.Lesson)
                            .ThenInclude(item => item!.Localizations)
            .Include(item => item.CurrentPublishedRevision)
                .ThenInclude(item => item!.Modules)
                    .ThenInclude(item => item.Localizations)
            .Include(item => item.CurrentPublishedRevision)
                .ThenInclude(item => item!.Modules)
                    .ThenInclude(item => item.Items)
                        .ThenInclude(item => item.Challenge)
                            .ThenInclude(item => item!.Localizations)
            .Include(item => item.CurrentPublishedRevision)
                .ThenInclude(item => item!.Modules)
                    .ThenInclude(item => item.Items)
                        .ThenInclude(item => item.Lesson)
                            .ThenInclude(item => item!.Localizations)
            .AsQueryable();
        return publishedOnly ? query.Where(item => item.CurrentPublishedRevisionId != null) : query;
    }

    private async Task ValidatePublishableAsync(
        LearningPath path, LearningPathRevision revision, CancellationToken token)
    {
        if (English(path.Localizations) is not { Title.Length: > 0 })
            throw new LearningPathValidationException("An English path title is required.");
        if (revision.Modules.Count == 0)
            throw new LearningPathValidationException("A path must contain at least one module.");

        foreach (var module in revision.Modules.OrderBy(item => item.SortOrder))
        {
            if (English(module.Localizations) is not { Title.Length: > 0 })
                throw new LearningPathValidationException("Every module requires an English title.");
            if (module.Items.Count == 0)
                throw new LearningPathValidationException("Every module must contain at least one item.");

            foreach (var item in module.Items)
            {
                if ((item.LessonId is null) == (item.ChallengeId is null))
                    throw new LearningPathValidationException("Every module item must reference one content item.");
                if (item.LessonId is { } lessonId)
                {
                    var lesson = await db.Lessons.Include(content => content.Localizations)
                        .SingleOrDefaultAsync(content => content.Id == lessonId, token);
                    if (lesson is null || English(lesson.Localizations) is not { Title.Length: > 0 })
                        throw new LearningPathValidationException("Every lesson requires an English title.");
                }
                else if (item.ChallengeId is { } challengeId)
                {
                    var challenge = await db.Challenges.Include(content => content.Localizations)
                        .SingleOrDefaultAsync(content => content.Id == challengeId, token);
                    if (challenge is null || challenge.PublicationState != ChallengePublicationState.Published)
                        throw new LearningPathValidationException("Every challenge must be published before the path is published.");
                    if (English(challenge.Localizations) is not { Title.Length: > 0 })
                        throw new LearningPathValidationException("Every challenge requires an English title.");
                }
            }
        }
    }

    private static LearningPathRevision CopyRevision(LearningPath path, LearningPathRevision source)
    {
        var copy = new LearningPathRevision { Path = path, Status = LearningPathRevisionStatus.Draft };
        foreach (var sourceModule in source.Modules.OrderBy(item => item.SortOrder))
        {
            var module = new LearningModule
            {
                Revision = copy,
                SortOrder = sourceModule.SortOrder,
                ExpectedMinutes = sourceModule.ExpectedMinutes
            };
            module.Localizations.AddRange(sourceModule.Localizations.Select(item =>
                new LearningModuleLocalization
                {
                    Locale = item.Locale, Title = item.Title, Summary = item.Summary, Module = module
                }));
            module.Items.AddRange(sourceModule.Items.OrderBy(item => item.SortOrder).Select(item =>
                new ModuleItem
                {
                    SortOrder = item.SortOrder, LessonId = item.LessonId, ChallengeId = item.ChallengeId,
                    Module = module
                }));
            copy.Modules.Add(module);
        }
        path.Revisions.Add(copy);
        return copy;
    }

    private static LearningPathRevision BuildRevision(LearningPath path, IReadOnlyList<LearningModuleCommand> modules)
    {
        var revision = new LearningPathRevision { Path = path };
        ReplaceRevisionGraph(revision, modules);
        return revision;
    }

    private static void ReplaceRevisionGraph(LearningPathRevision revision, IReadOnlyList<LearningModuleCommand> modules)
    {
        revision.Modules.Clear();
        foreach (var sourceModule in modules.OrderBy(item => item.SortOrder))
        {
            var module = new LearningModule
            {
                Revision = revision,
                SortOrder = sourceModule.SortOrder,
                ExpectedMinutes = sourceModule.ExpectedMinutes
            };
            module.Localizations.AddRange(sourceModule.Localizations.Select(item =>
                new LearningModuleLocalization
                {
                    Locale = item.Locale, Title = item.Title, Summary = item.Summary, Module = module
                }));
            module.Items.AddRange(sourceModule.Items.OrderBy(item => item.SortOrder).Select(item =>
                new ModuleItem
                {
                    SortOrder = item.SortOrder, LessonId = item.LessonId, ChallengeId = item.ChallengeId,
                    Module = module
                }));
            revision.Modules.Add(module);
        }
    }

    private static void ApplyPathLocalizations(LearningPath path, IReadOnlyList<LearningPathLocalizationCommand> localizations)
    {
        path.Localizations.Clear();
        path.Localizations.AddRange(localizations.Select(item => new LearningPathLocalization
        {
            Path = path, Locale = item.Locale, Title = item.Title, Summary = item.Summary
        }));
    }

    private static void EnsureRowVersion(LearningPathRevision revision, uint? requested)
    {
        if (requested is null || requested.Value != revision.RowVersion)
            throw new LearningRevisionConflictException();
    }

    private static LearningPathDraftResponse ToDraftResponse(
        LearningPath path, LearningPathRevision revision, string? locale) =>
        new(path.Id, revision.Id, path.Slug, revision.RowVersion,
            path.Localizations.Select(item => new LearningLocalizationResponse(item.Locale, item.Title, item.Summary)).ToArray(),
            revision.Modules.OrderBy(item => item.SortOrder).Select(module => ToModuleResponse(module, locale)).ToArray());

    private static LearningPathPreviewResponse ToPreviewResponse(
        LearningPath path, LearningPathRevision revision, string? locale)
    {
        var pathText = Pick(path.Localizations, locale);
        return new LearningPathPreviewResponse(
            path.Id, revision.Id, path.Slug, pathText?.Title ?? path.Slug, pathText?.Summary ?? string.Empty,
            revision.Modules.OrderBy(item => item.SortOrder).Select(module =>
            {
                var text = Pick(module.Localizations, locale);
                return new LearningModulePreviewResponse(
                    module.Id, module.SortOrder, module.ExpectedMinutes, text?.Title ?? string.Empty,
                    text?.Summary ?? string.Empty,
                    module.Items.OrderBy(item => item.SortOrder).Select(content =>
                    {
                        var challenge = content.Challenge;
                        var lesson = content.Lesson;
                        var contentText = challenge is not null
                            ? Pick(challenge.Localizations, locale)?.Title
                            : lesson is not null ? Pick(lesson.Localizations, locale)?.Title : null;
                        var contentSummary = challenge is not null
                            ? Pick(challenge.Localizations, locale)?.Summary
                            : null;
                        contentSummary ??= string.Empty;
                        return new LearningItemPreviewResponse(
                            content.Id, content.SortOrder,
                            content.ChallengeId is not null ? "challenge" : "lesson",
                            content.ChallengeId ?? content.LessonId ?? Guid.Empty,
                            contentText ?? string.Empty, contentSummary);
                    }).ToArray());
            }).ToArray());
    }

    private static LearningModuleResponse ToModuleResponse(LearningModule module, string? locale)
    {
        var text = Pick(module.Localizations, locale);
        return new LearningModuleResponse(
            module.Id, module.SortOrder, module.ExpectedMinutes, text?.Title ?? string.Empty,
            text?.Summary ?? string.Empty,
            module.Items.OrderBy(item => item.SortOrder).Select(content =>
                new LearningItemResponse(content.Id, content.SortOrder, content.LessonId, content.ChallengeId)).ToArray());
    }

    private static T? Pick<T>(IEnumerable<T> values, string? locale) where T : class
    {
        var items = values.ToArray();
        return items.FirstOrDefault(item => string.Equals(GetLocale(item), locale, StringComparison.OrdinalIgnoreCase))
            ?? items.FirstOrDefault(item => string.Equals(GetLocale(item), "en", StringComparison.OrdinalIgnoreCase))
            ?? items.FirstOrDefault();
    }

    private static T? English<T>(IEnumerable<T> values) where T : class =>
        values.FirstOrDefault(item => string.Equals(GetLocale(item), "en", StringComparison.OrdinalIgnoreCase));

    private static string GetLocale<T>(T item) where T : class => item switch
    {
        LearningPathLocalization value => value.Locale,
        LearningModuleLocalization value => value.Locale,
        LessonLocalization value => value.Locale,
        ChallengeLocalization value => value.Locale,
        _ => string.Empty
    };
}

public sealed record LearningPathCommand(
    string Slug,
    IReadOnlyList<LearningPathLocalizationCommand> Localizations,
    IReadOnlyList<LearningModuleCommand> Modules,
    uint? RowVersion = null,
    string? Locale = null);

public sealed record LearningPathLocalizationCommand(string Locale, string Title, string Summary);

public sealed record LearningModuleCommand(
    int SortOrder,
    int ExpectedMinutes,
    IReadOnlyList<LearningModuleLocalizationCommand> Localizations,
    IReadOnlyList<LearningModuleItemCommand> Items);

public sealed record LearningModuleLocalizationCommand(string Locale, string Title, string Summary);

public sealed record LearningModuleItemCommand(int SortOrder, Guid? LessonId, Guid? ChallengeId);

public sealed record LearningPathDraftResponse(
    Guid PathId,
    Guid RevisionId,
    string Slug,
    uint RowVersion,
    IReadOnlyList<LearningLocalizationResponse> Localizations,
    IReadOnlyList<LearningModuleResponse> Modules);

public sealed record LearningLocalizationResponse(string Locale, string Title, string Summary);

public sealed record LearningModuleResponse(
    Guid Id,
    int SortOrder,
    int ExpectedMinutes,
    string Title,
    string Summary,
    IReadOnlyList<LearningItemResponse> Items);

public sealed record LearningItemResponse(Guid Id, int SortOrder, Guid? LessonId, Guid? ChallengeId);

public sealed record LearningPathSummaryResponse(
    Guid PathId,
    string Slug,
    string Title,
    string Summary,
    int ModuleCount,
    int ItemCount,
    int ExpectedMinutes);

public sealed record LearningPathPreviewResponse(
    Guid PathId,
    Guid RevisionId,
    string Slug,
    string Title,
    string Summary,
    IReadOnlyList<LearningModulePreviewResponse> Modules);

public sealed record LearningModulePreviewResponse(
    Guid Id,
    int SortOrder,
    int ExpectedMinutes,
    string Title,
    string Summary,
    IReadOnlyList<LearningItemPreviewResponse> Items);

public sealed record LearningItemPreviewResponse(
    Guid Id,
    int SortOrder,
    string Kind,
    Guid ContentId,
    string Title,
    string Summary);

public sealed class LearningRevisionConflictException : Exception;
public sealed class LearningPathNotFoundException : Exception;
public sealed class LearningPathValidationException(string message) : Exception(message);
