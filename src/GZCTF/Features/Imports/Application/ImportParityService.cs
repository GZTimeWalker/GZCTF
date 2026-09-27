using System.Text.Json;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.Imports.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Features.Imports.Application;

public sealed record ImportParityMismatch(string SourceId, string Field, string Expected, string? Actual);

public sealed record ImportParityReport(
    int GameCount,
    int PathCount,
    int ModuleCount,
    int ChallengeCount,
    IReadOnlyDictionary<string, int> ChallengeTypes,
    int FlagCount,
    int AttachmentCount,
    int ContainerCount,
    int WarningCount,
    int UnsupportedFieldCount,
    IReadOnlyList<ImportParityMismatch> Mismatches)
{
    public bool IsComplete => Mismatches.Count == 0;
}

public sealed class ImportParityException(ImportParityReport report) : Exception(
    $"import.parity_failed:{report.Mismatches.Count}:{report.Mismatches.FirstOrDefault()?.Field}")
{
    public ImportParityReport Report { get; } = report;
}

public sealed class ImportParityService(AppDbContext db)
{
    public async Task<ImportParityReport> CompareAsync(
        CanonicalChallengeImportBatch source, CancellationToken token = default)
    {
        var challenges = source.Paths.SelectMany(path => path.Modules).SelectMany(module => module.Challenges)
            .Concat(source.Exercises).ToArray();
        var mismatches = new List<ImportParityMismatch>();
        var typeCounts = challenges.GroupBy(item => item.Type.ToString())
            .ToDictionary(group => group.Key, group => group.Count());
        foreach (var expected in challenges)
        {
            var mapping = await db.LegacyChallengeMaps.AsNoTracking().SingleOrDefaultAsync(item =>
                item.SourceType == expected.SourceType && item.SourceId == expected.SourceId, token);
            if (mapping is null)
            {
                mismatches.Add(new(expected.SourceId, "mapping", "present", null));
                continue;
            }
            var actual = await db.Challenges.AsNoTracking()
                .Include(item => item.Localizations).Include(item => item.Flags).Include(item => item.Hints)
                .SingleAsync(item => item.Id == mapping.ChallengeId, token);
            Compare(mismatches, expected, actual);
        }

        return new ImportParityReport(
            source.Paths.Count, source.Paths.Count,
            source.Paths.Sum(path => path.Modules.Count) + (source.Exercises.Count > 0 ? 1 : 0),
            challenges.Length, typeCounts,
            challenges.Count(item => item.StaticFlag is not null || item.FlagTemplate is not null ||
                                     item.Attachments.Count > 0),
            challenges.Sum(item => item.Attachments.Count),
            challenges.Count(item => item.Container is not null), 0, 0, mismatches);
    }

    public async Task<ImportParityReport> CompareAndEnforceAsync(
        CanonicalChallengeImportBatch source, MigrationBatch batch, CancellationToken token = default)
    {
        var report = await CompareAsync(source, token);
        batch.ParityReportJson = JsonSerializer.Serialize(report);
        if (!report.IsComplete)
            throw new ImportParityException(report);
        return report;
    }

    private static void Compare(ICollection<ImportParityMismatch> mismatches,
        CanonicalChallengeImport expected, CanonicalChallenge actual)
    {
        if (expected.Type != actual.Type)
            mismatches.Add(new(expected.SourceId, "type", expected.Type.ToString(), actual.Type.ToString()));
        var expectedTitle = expected.Localizations.FirstOrDefault(item => item.Locale == "en")?.Title;
        var actualTitle = actual.Localizations.FirstOrDefault(item => item.Locale == "en")?.Title;
        if (!string.Equals(expectedTitle, actualTitle, StringComparison.Ordinal))
            mismatches.Add(new(expected.SourceId, "localized.title.en", expectedTitle ?? string.Empty, actualTitle));
        var expectedHints = expected.Hints.OrderBy(item => item.SortOrder).Select(item => item.Content).ToArray();
        var actualHints = actual.Hints.OrderBy(item => item.SortOrder).Select(item => item.Content).ToArray();
        if (!expectedHints.SequenceEqual(actualHints, StringComparer.Ordinal))
            mismatches.Add(new(expected.SourceId, "hints.order_or_content", string.Join('|', expectedHints),
                string.Join('|', actualHints)));
        var actualFlag = actual.Flags.FirstOrDefault(item => item.Kind == ChallengeFlagKind.Static)?.Value;
        if (expected.StaticFlag is not null && !string.Equals(expected.StaticFlag, actualFlag, StringComparison.Ordinal))
            mismatches.Add(new(expected.SourceId, "flag", expected.StaticFlag, actualFlag));
        var actualTemplate = actual.Flags.FirstOrDefault(item => item.Kind == ChallengeFlagKind.Template)?.Template;
        if (expected.FlagTemplate is not null && !string.Equals(expected.FlagTemplate, actualTemplate,
                StringComparison.Ordinal))
            mismatches.Add(new(expected.SourceId, "flag_template", expected.FlagTemplate, actualTemplate));
        if (expected.Container is { } container)
        {
            var actualContainer = JsonSerializer.Deserialize<ImportContainerConfiguration>(
                actual.RuntimeConfigurationJson ?? "{}",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (actualContainer is null || actualContainer != container)
                mismatches.Add(new(expected.SourceId, "container_configuration", JsonSerializer.Serialize(container),
                    actual.RuntimeConfigurationJson));
        }
    }
}
