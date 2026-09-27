using System.Text.Json;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.ChallengeRuntime.Application;
using GZCTF.Storage.Interface;
using GZCTF.Utils;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Features.SkillTrees.Application;

internal static class ChallengePublicationValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    internal static async Task ValidateAsync(
        CanonicalChallenge challenge, IBlobStorage storage, CancellationToken token)
    {
        var hasStaticFlag = challenge.Flags.Any(flag =>
            flag.Kind == ChallengeFlagKind.Static && !string.IsNullOrWhiteSpace(flag.Value));

        if (challenge.Type is ChallengeType.StaticAttachment or ChallengeType.StaticContainer && !hasStaticFlag)
            throw new ContentPublicationValidationException(
                "At least one static Flag is required.", "content_static_flag_required");

        if (challenge.Type.IsAttachment())
        {
            var pool = challenge.Flags.FirstOrDefault(flag => flag.Kind == ChallengeFlagKind.DynamicAttachment);
            var attachments = ReadAttachments(pool?.MetadataJson ?? challenge.RuntimeConfigurationJson);
            if (challenge.Type == ChallengeType.DynamicAttachment && attachments.Count == 0)
                throw new ContentPublicationValidationException(
                    "A dynamic attachment needs file and Flag pairs.", "content_attachment_pool_required");

            foreach (var attachment in attachments)
            {
                if (string.IsNullOrWhiteSpace(attachment.FileName) ||
                    string.IsNullOrWhiteSpace(attachment.Sha256) ||
                    string.IsNullOrWhiteSpace(attachment.Flag))
                    throw new ContentPublicationValidationException(
                        "Each attachment needs a file, SHA-256 and Flag.", "content_attachment_pool_required");
                var key = AttachmentStorageKey.Normalize(attachment.StorageKey, attachment.FileName);
                bool exists;
                try
                {
                    exists = await storage.ExistsAsync(key, token);
                }
                catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException)
                {
                    exists = false;
                }
                if (!exists)
                    throw new ContentPublicationValidationException(
                        $"Attachment '{attachment.FileName}' is missing from storage.",
                        "content_attachment_missing");
            }
        }

        if (!challenge.Type.IsContainer()) return;

        ContainerSettings? settings;
        try
        {
            settings = JsonSerializer.Deserialize<ContainerSettings>(
                challenge.RuntimeConfigurationJson ?? "", JsonOptions);
        }
        catch (JsonException)
        {
            settings = null;
        }

        if (settings is null ||
            string.IsNullOrWhiteSpace(settings.ContainerImage ?? settings.Image) ||
            settings.ExposedPort is < 1 or > 65535 || settings.Cpu <= 0 ||
            settings.MemoryMb <= 0 || settings.StorageMb < 0 ||
            string.IsNullOrWhiteSpace(settings.NetworkMode))
            throw new ContentPublicationValidationException(
                "Complete the container image, port and resource settings.",
                "content_container_configuration_required");

        if (challenge.Type == ChallengeType.DynamicContainer &&
            string.IsNullOrWhiteSpace(settings.FlagTemplate) &&
            !challenge.Flags.Any(flag => flag.Kind == ChallengeFlagKind.Template &&
                                         !string.IsNullOrWhiteSpace(flag.Template)))
            throw new ContentPublicationValidationException(
                "A dynamic container needs a Flag template.", "content_flag_template_required");
    }

    private static IReadOnlyList<AttachmentCandidate> ReadAttachments(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata)) return [];
        try
        {
            using var document = JsonDocument.Parse(metadata);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("Attachments", out var attachments))
                root = attachments;
            return root.ValueKind == JsonValueKind.Array
                ? JsonSerializer.Deserialize<List<AttachmentCandidate>>(root.GetRawText(), JsonOptions) ?? []
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record AttachmentCandidate(
        string FileName, string Sha256, string Flag, string? StorageKey);

    private sealed record ContainerSettings(
        string? ContainerImage,
        string? Image,
        int ExposedPort,
        int Cpu,
        int MemoryMb,
        int StorageMb,
        string? NetworkMode,
        string? FlagTemplate);
}
