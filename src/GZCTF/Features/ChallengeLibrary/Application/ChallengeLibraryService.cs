using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.Dashboard.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Features.ChallengeLibrary.Application;

public sealed class ChallengeLibraryService(AppDbContext db, IChallengeMergeConflictChecker conflictChecker)
{
    public async Task<ChallengeEditResponse> CreateChallengeAsync(
        ChallengeCommand command, CancellationToken token)
    {
        var challenge = new CanonicalChallenge();
        ApplyChallengeCommand(challenge, command, isCreate: true);
        await db.Challenges.AddAsync(challenge, token);
        await db.SaveChangesAsync(token);
        return ToEditResponse(challenge, command.Locale);
    }

    public async Task<IReadOnlyList<ChallengeSummaryResponse>> ListChallengesAsync(
        string? locale, CancellationToken token)
    {
        var challenges = await db.Challenges
            .AsNoTracking()
            .Include(challenge => challenge.Localizations)
            .OrderBy(challenge => challenge.Id)
            .ToListAsync(token);

        return challenges.Select(challenge => ToSummaryResponse(challenge, locale)).ToArray();
    }

    public async Task<ChallengeSummaryResponse?> GetChallengeAsync(
        Guid id, string? locale, CancellationToken token)
    {
        var challenge = await db.Challenges
            .AsNoTracking()
            .Include(item => item.Localizations)
            .SingleOrDefaultAsync(item => item.Id == id, token);
        return challenge is null ? null : ToSummaryResponse(challenge, locale);
    }

    public async Task<ChallengeEditResponse?> GetChallengeForEditAsync(
        Guid id, string? locale, CancellationToken token)
    {
        var challenge = await db.Challenges
            .Include(item => item.Localizations)
            .Include(item => item.Flags)
            .Include(item => item.Hints)
            .Include(item => item.Writeups)
            .SingleOrDefaultAsync(item => item.Id == id, token);
        return challenge is null ? null : ToEditResponse(challenge, locale);
    }

    public async Task<ChallengeEditResponse?> UpdateChallengeAsync(
        Guid id, ChallengeCommand command, CancellationToken token)
    {
        var challenge = await db.Challenges
            .Include(item => item.Localizations)
            .Include(item => item.Flags)
            .Include(item => item.Hints)
            .Include(item => item.Writeups)
            .SingleOrDefaultAsync(item => item.Id == id, token);
        if (challenge is null)
            return null;

        if (command.Type is { } type && type != challenge.Type &&
            challenge.PublicationState != ChallengePublicationState.Draft)
            throw new ChallengeTypeImmutableException();

        ApplyChallengeCommand(challenge, command, isCreate: false);
        await db.SaveChangesAsync(token);
        return ToEditResponse(challenge, command.Locale);
    }

    public async Task<bool> RetireChallengeAsync(Guid id, CancellationToken token)
    {
        var challenge = await db.Challenges.SingleOrDefaultAsync(item => item.Id == id, token);
        if (challenge is null)
            return false;

        challenge.PublicationState = ChallengePublicationState.Retired;
        challenge.IsEnabled = false;
        await db.SaveChangesAsync(token);
        return true;
    }

    public async Task<LessonResponse> CreateLessonAsync(LessonCommand command, CancellationToken token)
    {
        var lesson = new Lesson();
        ApplyLessonCommand(lesson, command, isCreate: true);
        await db.Lessons.AddAsync(lesson, token);
        await db.SaveChangesAsync(token);
        return ToLessonResponse(lesson, command.Locale);
    }

    public async Task<IReadOnlyList<LessonResponse>> ListLessonsAsync(
        string? locale, CancellationToken token)
    {
        var lessons = await db.Lessons
            .AsNoTracking()
            .Include(lesson => lesson.Localizations)
            .OrderBy(lesson => lesson.Id)
            .ToListAsync(token);
        return lessons.Select(lesson => ToLessonResponse(lesson, locale)).ToArray();
    }

    public async Task<LessonResponse?> GetLessonAsync(Guid id, string? locale, CancellationToken token)
    {
        var lesson = await db.Lessons
            .AsNoTracking()
            .Include(item => item.Localizations)
            .SingleOrDefaultAsync(item => item.Id == id, token);
        return lesson is null ? null : ToLessonResponse(lesson, locale);
    }

    public async Task<LessonResponse?> UpdateLessonAsync(
        Guid id, LessonCommand command, CancellationToken token)
    {
        var lesson = await db.Lessons
            .Include(item => item.Localizations)
            .SingleOrDefaultAsync(item => item.Id == id, token);
        if (lesson is null)
            return null;

        ApplyLessonCommand(lesson, command, isCreate: false);
        await db.SaveChangesAsync(token);
        return ToLessonResponse(lesson, command.Locale);
    }

    public async Task<bool> DeleteLessonAsync(Guid id, CancellationToken token)
    {
        var lesson = await db.Lessons.SingleOrDefaultAsync(item => item.Id == id, token);
        if (lesson is null)
            return false;

        db.Lessons.Remove(lesson);
        await db.SaveChangesAsync(token);
        return true;
    }

    public async Task<ChallengeMergeResult> MergeChallengesAsync(
        Guid duplicateId, Guid survivorId, CancellationToken token)
    {
        if (duplicateId == survivorId)
            throw new ChallengeMergeException("A challenge cannot be merged into itself.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        var duplicate = await db.Challenges.SingleOrDefaultAsync(item => item.Id == duplicateId, token)
                        ?? throw new ChallengeMergeException("Duplicate challenge was not found.");
        var survivor = await db.Challenges.SingleOrDefaultAsync(item => item.Id == survivorId, token)
                       ?? throw new ChallengeMergeException("Survivor challenge was not found.");

        if (duplicate.PublicationState == ChallengePublicationState.Merged)
            throw new ChallengeMergeException("The duplicate challenge was already merged.");

        if (await conflictChecker.HasConflictAsync(duplicateId, token) ||
            await conflictChecker.HasConflictAsync(survivorId, token))
            throw new ChallengeMergeConflictException();

        var affectedUserIds = await db.ChallengeProgress
            .Where(progress => progress.ChallengeId == duplicateId || progress.ChallengeId == survivorId)
            .Select(progress => progress.UserId)
            .Distinct()
            .ToArrayAsync(token);

        await db.ModuleItems
            .Where(item => item.ChallengeId == duplicateId)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.ChallengeId, survivorId), token);

        var progress = await db.ChallengeProgress
            .Where(item => item.ChallengeId == duplicateId || item.ChallengeId == survivorId)
            .ToListAsync(token);
        db.ChallengeProgress.RemoveRange(progress);
        await db.SaveChangesAsync(token);

        foreach (var userProgress in progress.GroupBy(item => item.UserId))
        {
            var earliest = userProgress.OrderBy(item => item.SolvedAtUtc).First();
            db.ChallengeProgress.Add(new ChallengeProgress
            {
                UserId = earliest.UserId,
                ChallengeId = survivorId,
                SolvedAtUtc = earliest.SolvedAtUtc,
                SolveMode = earliest.SolveMode,
                AttributionMetadataJson = earliest.AttributionMetadataJson
            });
        }

        await RebuildDailyStatsAsync(affectedUserIds, token);
        duplicate.PublicationState = ChallengePublicationState.Merged;
        duplicate.IsEnabled = false;
        duplicate.SourceMetadataJson = MergeMetadata(duplicate.SourceMetadataJson, survivorId);
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);

        return new ChallengeMergeResult(duplicateId, survivorId, progress.Count);
    }

    private async Task RebuildDailyStatsAsync(Guid[] userIds, CancellationToken token)
    {
        if (userIds.Length == 0)
            return;

        var existing = await db.LearnerDailySolveStats
            .Where(stat => userIds.Contains(stat.UserId))
            .ToListAsync(token);
        db.LearnerDailySolveStats.RemoveRange(existing);
        await db.SaveChangesAsync(token);

        var rebuilt = await db.ChallengeProgress
            .Where(progress => userIds.Contains(progress.UserId))
            .GroupBy(progress => new { progress.UserId, Date = DateOnly.FromDateTime(progress.SolvedAtUtc.UtcDateTime) })
            .Select(group => new LearnerDailySolveStat
            {
                UserId = group.Key.UserId,
                Date = group.Key.Date,
                SolveCount = group.Count()
            })
            .ToListAsync(token);
        await db.LearnerDailySolveStats.AddRangeAsync(rebuilt, token);
    }

    private static void ApplyChallengeCommand(CanonicalChallenge challenge, ChallengeCommand command, bool isCreate)
    {
        if (command.Type is { } type)
            challenge.Type = type;
        if (command.Difficulty is { } difficulty)
            challenge.Difficulty = difficulty;
        if (command.SourceType is not null)
            challenge.SourceType = command.SourceType.Trim();
        if (command.SourceId is not null)
            challenge.SourceId = command.SourceId.Trim();
        if (command.SourceName is not null)
            challenge.SourceName = command.SourceName;
        if (command.SourceMetadataJson is not null)
            challenge.SourceMetadataJson = command.SourceMetadataJson;
        if (command.RuntimeConfigurationJson is not null)
            challenge.RuntimeConfigurationJson = command.RuntimeConfigurationJson;
        if (command.IsEnabled is { } isEnabled)
            challenge.IsEnabled = isEnabled;
        if (command.ExpectedMinutes is { } expectedMinutes)
            challenge.ExpectedMinutes = expectedMinutes;
        if (command.Localizations is not null)
        {
            challenge.Localizations.Clear();
            challenge.Localizations.AddRange(command.Localizations.Select(localization => new ChallengeLocalization
            {
                Locale = localization.Locale,
                Title = localization.Title,
                Summary = localization.Summary,
                Body = localization.Body
            }));
        }
        if (command.Flags is not null)
        {
            challenge.Flags.Clear();
            challenge.Flags.AddRange(command.Flags.Select(flag => new ChallengeFlag
            {
                Kind = flag.Kind,
                Value = flag.Value,
                Template = flag.Template,
                AttachmentPoolKey = flag.AttachmentPoolKey,
                MetadataJson = flag.MetadataJson
            }));
        }
        if (command.Hints is not null)
        {
            challenge.Hints.Clear();
            challenge.Hints.AddRange(command.Hints.Select(hint => new ChallengeHint
            {
                Locale = hint.Locale,
                SortOrder = hint.SortOrder,
                Content = hint.Content
            }));
        }
        if (command.Writeups is not null)
        {
            challenge.Writeups.Clear();
            challenge.Writeups.AddRange(command.Writeups.Select(writeup => new ChallengeWriteup
            {
                Locale = writeup.Locale,
                Content = writeup.Content
            }));
        }

        if (isCreate)
        {
            challenge.SourceType = string.IsNullOrWhiteSpace(challenge.SourceType) ? "native" : challenge.SourceType;
            challenge.SourceId = string.IsNullOrWhiteSpace(challenge.SourceId)
                ? challenge.Id.ToString("N")
                : challenge.SourceId;
        }
    }

    private static void ApplyLessonCommand(Lesson lesson, LessonCommand command, bool isCreate)
    {
        if (command.Localizations is not null)
        {
            lesson.Localizations.Clear();
            lesson.Localizations.AddRange(command.Localizations.Select(localization => new LessonLocalization
            {
                Locale = localization.Locale,
                Title = localization.Title,
                Body = localization.Body
            }));
        }
    }

    private static ChallengeSummaryResponse ToSummaryResponse(CanonicalChallenge challenge, string? locale)
    {
        var localization = SelectLocalization(challenge.Localizations, locale, item => item.Locale);
        return new ChallengeSummaryResponse(
            challenge.Id,
            challenge.Type,
            challenge.Difficulty,
            challenge.PublicationState,
            challenge.IsEnabled,
            localization?.Title ?? string.Empty,
            localization?.Summary ?? string.Empty,
            challenge.SourceType,
            challenge.SourceId,
            challenge.SourceName);
    }

    private static ChallengeEditResponse ToEditResponse(CanonicalChallenge challenge, string? locale)
    {
        var summary = ToSummaryResponse(challenge, locale);
        return new ChallengeEditResponse(
            summary,
            challenge.RuntimeConfigurationJson,
            challenge.Localizations.Select(localization => new ChallengeLocalizationResponse(
                localization.Locale, localization.Title, localization.Summary, localization.Body)).ToArray(),
            challenge.Flags.Select(flag => new ChallengeFlagResponse(
                flag.Id, flag.Kind, flag.Value, flag.Template, flag.AttachmentPoolKey, flag.MetadataJson)).ToArray(),
            challenge.Hints.OrderBy(hint => hint.SortOrder).Select(hint => new ChallengeHintResponse(
                hint.Id, hint.Locale, hint.SortOrder, hint.Content)).ToArray(),
            challenge.Writeups.Select(writeup => new ChallengeWriteupResponse(
                writeup.Id, writeup.Locale, writeup.Content)).ToArray());
    }

    private static LessonResponse ToLessonResponse(Lesson lesson, string? locale)
    {
        var localization = SelectLocalization(lesson.Localizations, locale, item => item.Locale);
        return new LessonResponse(
            lesson.Id,
            localization?.Locale ?? "en",
            localization?.Title ?? string.Empty,
            localization?.Body ?? string.Empty,
            lesson.Localizations.Select(item => new LessonLocalizationResponse(
                item.Locale, item.Title, item.Body)).ToArray());
    }

    private static T? SelectLocalization<T>(
        IEnumerable<T> localizations, string? locale, Func<T, string> localeSelector)
        where T : class
    {
        var items = localizations.ToArray();
        if (items.Length == 0)
            return null;

        return items.FirstOrDefault(item => string.Equals(
                   localeSelector(item), locale, StringComparison.OrdinalIgnoreCase))
               ?? items.FirstOrDefault(item => string.Equals(
                   localeSelector(item), "en", StringComparison.OrdinalIgnoreCase))
               ?? items[0];
    }

    private static string MergeMetadata(string? existing, Guid survivorId)
    {
        var metadata = JsonNode.Parse(existing ?? "{}") as JsonObject ?? [];
        metadata["mergedIntoChallengeId"] = survivorId.ToString();
        metadata["mergedAtUtc"] = DateTimeOffset.UtcNow;
        return metadata.ToJsonString();
    }
}

public interface IChallengeMergeConflictChecker
{
    Task<bool> HasConflictAsync(Guid challengeId, CancellationToken token);
}

public sealed class NoopChallengeMergeConflictChecker : IChallengeMergeConflictChecker
{
    public Task<bool> HasConflictAsync(Guid challengeId, CancellationToken token) => Task.FromResult(false);
}

public sealed class ChallengeTypeImmutableException : Exception;

public class ChallengeMergeException(string message) : Exception(message);

public sealed class ChallengeMergeConflictException()
    : ChallengeMergeException("The challenge has an active runtime instance and cannot be merged.");

public sealed class ChallengeCommand
{
    public ChallengeType? Type { get; set; }
    public Difficulty? Difficulty { get; set; }
    public string? SourceType { get; set; }
    public string? SourceId { get; set; }
    public string? SourceName { get; set; }
    public string? SourceMetadataJson { get; set; }
    public string? RuntimeConfigurationJson { get; set; }
    public bool? IsEnabled { get; set; }
    public int? ExpectedMinutes { get; set; }
    public string? Locale { get; set; }
    public List<ChallengeLocalizationCommand>? Localizations { get; set; }
    public List<ChallengeFlagCommand>? Flags { get; set; }
    public List<ChallengeHintCommand>? Hints { get; set; }
    public List<ChallengeWriteupCommand>? Writeups { get; set; }
}

public sealed class ChallengeLocalizationCommand
{
    public string Locale { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed class ChallengeFlagCommand
{
    public ChallengeFlagKind Kind { get; set; }
    public string? Value { get; set; }
    public string? Template { get; set; }
    public string? AttachmentPoolKey { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class ChallengeHintCommand
{
    public string Locale { get; set; } = "en";
    public int SortOrder { get; set; }
    public string Content { get; set; } = string.Empty;
}

public sealed class ChallengeWriteupCommand
{
    public string Locale { get; set; } = "en";
    public string Content { get; set; } = string.Empty;
}

public sealed class LessonCommand
{
    public string? Locale { get; set; }
    public List<LessonLocalizationCommand>? Localizations { get; set; }
}

public sealed class LessonLocalizationCommand
{
    public string Locale { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed record ChallengeSummaryResponse(
    Guid Id,
    ChallengeType Type,
    Difficulty Difficulty,
    ChallengePublicationState PublicationState,
    bool IsEnabled,
    string Title,
    string Summary,
    string SourceType,
    string SourceId,
    string? SourceName);

public sealed record ChallengeEditResponse(
    ChallengeSummaryResponse Challenge,
    string? RuntimeConfigurationJson,
    IReadOnlyList<ChallengeLocalizationResponse> Localizations,
    IReadOnlyList<ChallengeFlagResponse> Flags,
    IReadOnlyList<ChallengeHintResponse> Hints,
    IReadOnlyList<ChallengeWriteupResponse> Writeups);

public sealed record ChallengeLocalizationResponse(string Locale, string Title, string Summary, string Body);

public sealed record ChallengeFlagResponse(
    Guid Id,
    ChallengeFlagKind Kind,
    string? Value,
    string? Template,
    string? AttachmentPoolKey,
    string? MetadataJson);

public sealed record ChallengeHintResponse(Guid Id, string Locale, int SortOrder, string Content);

public sealed record ChallengeWriteupResponse(Guid Id, string Locale, string Content);

public sealed record LessonResponse(
    Guid Id,
    string Locale,
    string Title,
    string Body,
    IReadOnlyList<LessonLocalizationResponse> Localizations);

public sealed record LessonLocalizationResponse(string Locale, string Title, string Body);

public sealed record ChallengeMergeResult(Guid DuplicateId, Guid SurvivorId, int ProgressRowsReconciled);
