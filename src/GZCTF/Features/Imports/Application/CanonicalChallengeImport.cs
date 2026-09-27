using GZCTF.Utils;

namespace GZCTF.Features.Imports.Application;

public sealed record ImportLocalizedText(string Locale, string Title, string Summary, string Body);

public sealed record ImportHint(string Locale, int SortOrder, string Content);

public sealed record ImportAttachment(string FileName, string Sha256, string StorageKey, string Flag);

public sealed record ImportContainerConfiguration(
    string Image,
    int ExposedPort,
    int Cpu,
    int MemoryMb,
    int StorageMb,
    string NetworkMode);

public sealed record CanonicalChallengeImport(
    string SourceType,
    string SourceId,
    ChallengeType Type,
    IReadOnlyList<ImportLocalizedText> Localizations,
    IReadOnlyList<ImportHint> Hints,
    string? StaticFlag,
    string? FlagTemplate,
    IReadOnlyList<ImportAttachment> Attachments,
    ImportContainerConfiguration? Container,
    string? LegacyMetadataJson,
    int? LegacyDifficulty,
    int SourceOrder);

public sealed record CanonicalModuleImport(
    string SourceId,
    string Title,
    string Summary,
    int SortOrder,
    IReadOnlyList<CanonicalChallengeImport> Challenges);

public sealed record CanonicalPathImport(
    string SourceType,
    string SourceId,
    string Slug,
    string Title,
    string Summary,
    IReadOnlyList<CanonicalModuleImport> Modules);

public sealed record CanonicalChallengeImportBatch(
    string SourceType,
    string FingerprintSha256,
    IReadOnlyList<CanonicalPathImport> Paths,
    IReadOnlyList<CanonicalChallengeImport> Exercises);

public sealed record ImportValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public static class CanonicalChallengeImportValidator
{
    public static ImportValidationResult Validate(CanonicalChallengeImportBatch batch)
    {
        var errors = new List<string>();
        var sourceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var challenge in batch.Paths.SelectMany(path => path.Modules).SelectMany(module => module.Challenges)
                     .Concat(batch.Exercises))
        {
            if (!sourceIds.Add($"{challenge.SourceType}:{challenge.SourceId}"))
                errors.Add($"duplicate_source_id:{challenge.SourceType}:{challenge.SourceId}");
            ValidateChallenge(challenge, errors);
        }

        foreach (var path in batch.Paths)
        {
            if (!sourceIds.Add($"path:{path.SourceType}:{path.SourceId}"))
                errors.Add($"duplicate_path_source_id:{path.SourceType}:{path.SourceId}");
            if (string.IsNullOrWhiteSpace(path.Slug))
                errors.Add($"path_slug_missing:{path.SourceId}");
        }

        return new ImportValidationResult(errors);
    }

    public static void ThrowIfInvalid(CanonicalChallengeImportBatch batch)
    {
        var result = Validate(batch);
        if (!result.IsValid)
            throw new InvalidOperationException(string.Join(';', result.Errors));
    }

    public static string NormalizeLocale(string locale) =>
        locale.Equals("en", StringComparison.OrdinalIgnoreCase) ||
        locale.Equals("en-US", StringComparison.OrdinalIgnoreCase) ||
        locale.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ||
        locale.Equals("zh-TW", StringComparison.OrdinalIgnoreCase)
            ? locale
            : "en";

    private static void ValidateChallenge(CanonicalChallengeImport challenge, ICollection<string> errors)
    {
        var attachmentMode = challenge.Type.IsAttachment();
        var containerMode = challenge.Type.IsContainer();
        if (attachmentMode == containerMode)
            errors.Add($"type_invalid:{challenge.SourceId}");
        if (attachmentMode && challenge.Container is not null)
            errors.Add($"mixed_container_attachment:{challenge.SourceId}");
        if (containerMode && challenge.Attachments.Count > 0)
            errors.Add($"mixed_attachment_container:{challenge.SourceId}");
        if (challenge.Type is ChallengeType.StaticAttachment or ChallengeType.StaticContainer &&
            string.IsNullOrWhiteSpace(challenge.StaticFlag))
            errors.Add($"static_flag_missing:{challenge.SourceId}");
        if (challenge.Type == ChallengeType.DynamicContainer && string.IsNullOrWhiteSpace(challenge.FlagTemplate))
            errors.Add($"dynamic_template_missing:{challenge.SourceId}");
        if (challenge.Type == ChallengeType.DynamicAttachment && challenge.Attachments.Count == 0)
            errors.Add($"dynamic_attachment_missing:{challenge.SourceId}");
        if (containerMode && challenge.Container is { } container &&
            (string.IsNullOrWhiteSpace(container.Image) || container.ExposedPort is < 1 or > 65535 ||
             container.Cpu <= 0 || container.MemoryMb <= 0 || container.StorageMb <= 0))
            errors.Add($"container_configuration_invalid:{challenge.SourceId}");
        foreach (var attachment in challenge.Attachments)
            if (Path.IsPathRooted(attachment.FileName) || attachment.FileName.Split('/', '\\')
                    .Any(segment => segment is ".." or ""))
                errors.Add($"unsafe_attachment_path:{challenge.SourceId}:{attachment.FileName}");
    }
}
