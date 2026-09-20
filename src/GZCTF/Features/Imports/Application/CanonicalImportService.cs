using System.Text.Json;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.Imports.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Features.Imports.Application;

public sealed record CanonicalImportResult(
    Guid BatchId,
    MigrationBatchState State,
    int ChallengeCount,
    int PathCount,
    int WarningCount);

public sealed class CanonicalImportService(AppDbContext db, ImportParityService parity)
{
    public async Task<CanonicalImportResult> ImportAsync(
        CanonicalChallengeImportBatch source, CancellationToken token = default)
    {
        CanonicalChallengeImportValidator.ThrowIfInvalid(source);
        var existing = await db.MigrationBatches.SingleOrDefaultAsync(item =>
            item.PackageFingerprintSha256 == source.FingerprintSha256, token);
        if (existing?.State == MigrationBatchState.Completed)
            return ToResult(existing);

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var batch = existing ?? new MigrationBatch
        {
            SourceType = source.SourceType,
            PackageFingerprintSha256 = source.FingerprintSha256,
            State = MigrationBatchState.Running,
            StartedAtUtc = DateTimeOffset.UtcNow
        };
        if (existing is null)
            db.MigrationBatches.Add(batch);
        else
            batch.State = MigrationBatchState.Running;
        await db.SaveChangesAsync(token);

        var warnings = new List<string>();
        foreach (var pathSource in source.Paths)
            await ImportPathAsync(pathSource, batch, warnings, token);
        if (source.Exercises.Count > 0)
        {
            var exercisePath = new CanonicalPathImport(
                source.SourceType,
                "legacy-exercises",
                "legacy-exercises",
                "Legacy Exercises",
                "Imported legacy exercise dependencies",
                [new CanonicalModuleImport(
                    "legacy-exercises-module",
                    "Exercises",
                    "Legacy exercise challenges",
                    0,
                    source.Exercises.OrderBy(item => item.SourceOrder).ToArray())]);
            await ImportPathAsync(exercisePath, batch, warnings, token);
        }

        await parity.CompareAndEnforceAsync(source, batch, token);
        batch.State = MigrationBatchState.Completed;
        batch.CompletedAtUtc = DateTimeOffset.UtcNow;
        batch.ChallengeCount = source.Paths.SelectMany(path => path.Modules)
            .SelectMany(module => module.Challenges).Count() + source.Exercises.Count;
        batch.PathCount = source.Paths.Count + (source.Exercises.Count > 0 ? 1 : 0);
        batch.WarningCount = warnings.Count;
        batch.WarningsJson = JsonSerializer.Serialize(warnings);
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return ToResult(batch);
    }

    private async Task ImportPathAsync(
        CanonicalPathImport source, MigrationBatch batch, ICollection<string> warnings, CancellationToken token)
    {
        var pathMap = await db.LegacyPathMaps.SingleOrDefaultAsync(item =>
            item.SourceType == source.SourceType && item.SourceId == source.SourceId, token);
        LearningPath path;
        if (pathMap is not null)
        {
            path = await db.LearningPaths.Include(item => item.Revisions)
                .ThenInclude(item => item.Modules).ThenInclude(item => item.Items)
                .SingleAsync(item => item.Id == pathMap.PathId, token);
        }
        else
        {
            path = new LearningPath
            {
                Slug = source.Slug,
                Localizations = [new LearningPathLocalization
                {
                    Locale = "en", Title = source.Title, Summary = source.Summary
                }]
            };
            var revision = new LearningPathRevision { Status = LearningPathRevisionStatus.Draft };
            path.Revisions.Add(revision);
            db.LearningPaths.Add(path);
            pathMap = new LegacyPathMap
            {
                SourceType = source.SourceType,
                SourceId = source.SourceId,
                Path = path,
                MigrationBatch = batch,
                MigrationBatchId = batch.Id
            };
            db.LegacyPathMaps.Add(pathMap);
            batch.PathMappings.Add(pathMap);
            await db.SaveChangesAsync(token);
        }

        var revisionTarget = path.Revisions.SingleOrDefault(item => item.Status == LearningPathRevisionStatus.Draft)
                             ?? new LearningPathRevision { Path = path, Status = LearningPathRevisionStatus.Draft };
        if (!path.Revisions.Contains(revisionTarget))
            path.Revisions.Add(revisionTarget);
        foreach (var moduleSource in source.Modules.OrderBy(item => item.SortOrder))
        {
            var module = revisionTarget.Modules.SingleOrDefault(item => item.SortOrder == moduleSource.SortOrder)
                         ?? new LearningModule
                         {
                             SortOrder = moduleSource.SortOrder,
                             ExpectedMinutes = 60,
                             Localizations = [new LearningModuleLocalization
                             {
                                 Locale = "en", Title = moduleSource.Title, Summary = moduleSource.Summary
                             }]
                         };
            if (!revisionTarget.Modules.Contains(module))
                revisionTarget.Modules.Add(module);
            foreach (var challengeSource in moduleSource.Challenges.OrderBy(item => item.SourceOrder))
            {
                var challenge = await ImportChallengeAsync(challengeSource, batch, warnings, token);
                if (module.Items.All(item => item.ChallengeId != challenge.Id))
                    module.Items.Add(new ModuleItem { SortOrder = module.Items.Count, Challenge = challenge });
            }
        }
        await db.SaveChangesAsync(token);
    }

    private async Task<CanonicalChallenge> ImportChallengeAsync(
        CanonicalChallengeImport source, MigrationBatch batch, ICollection<string> warnings,
        CancellationToken token)
    {
        var map = await db.LegacyChallengeMaps.SingleOrDefaultAsync(item =>
            item.SourceType == source.SourceType && item.SourceId == source.SourceId, token);
        if (map is not null)
            return await db.Challenges.Include(item => item.Localizations).SingleAsync(item => item.Id == map.ChallengeId,
                token);

        var challenge = new CanonicalChallenge
        {
            Type = source.Type,
            Difficulty = Difficulty.Normal,
            PublicationState = ChallengePublicationState.Draft,
            SourceType = source.SourceType,
            SourceId = source.SourceId,
            SourceMetadataJson = source.LegacyMetadataJson,
            RuntimeConfigurationJson = source.Container is null ?
                JsonSerializer.Serialize(new { source.Attachments }) : JsonSerializer.Serialize(source.Container),
            Localizations = source.Localizations.Select(item => new ChallengeLocalization
            {
                Locale = item.Locale,
                Title = item.Title,
                Summary = item.Summary,
                Body = item.Body
            }).ToList(),
            Hints = source.Hints.Select(item => new ChallengeHint
            {
                Locale = item.Locale,
                SortOrder = item.SortOrder,
                Content = item.Content
            }).ToList()
        };
        if (source.Type is ChallengeType.StaticAttachment or ChallengeType.StaticContainer)
            challenge.Flags.Add(new ChallengeFlag { Kind = ChallengeFlagKind.Static, Value = source.StaticFlag });
        else if (source.Type == ChallengeType.DynamicAttachment)
            challenge.Flags.Add(new ChallengeFlag
            {
                Kind = ChallengeFlagKind.DynamicAttachment,
                AttachmentPoolKey = source.SourceId,
                MetadataJson = JsonSerializer.Serialize(source.Attachments)
            });
        else
            challenge.Flags.Add(new ChallengeFlag { Kind = ChallengeFlagKind.Template, Template = source.FlagTemplate });

        db.Challenges.Add(challenge);
        var challengeMap = new LegacyChallengeMap
        {
            SourceType = source.SourceType,
            SourceId = source.SourceId,
            Challenge = challenge,
            MigrationBatch = batch,
            MigrationBatchId = batch.Id
        };
        db.LegacyChallengeMaps.Add(challengeMap);
        batch.ChallengeMappings.Add(challengeMap);
        warnings.Add($"{source.SourceType}:{source.SourceId}:official_writeup_not_imported");
        await db.SaveChangesAsync(token);
        return challenge;
    }

    private static CanonicalImportResult ToResult(MigrationBatch batch) =>
        new(batch.Id, batch.State, batch.ChallengeCount, batch.PathCount, batch.WarningCount);
}
