using GZCTF.Features.Imports.Application;
using GZCTF.Utils;
using System.Linq;
using Xunit;

namespace GZCTF.Test.UnitTests.Features.Imports;

public sealed class CanonicalChallengeImportTests
{
    [Fact]
    public void Valid_four_mode_graph_is_accepted()
    {
        var batch = Batch(Challenge(ChallengeType.StaticAttachment, "static"));
        Assert.True(CanonicalChallengeImportValidator.Validate(batch).IsValid);
    }

    [Fact]
    public void Mixed_attachment_and_container_configuration_is_rejected()
    {
        var challenge = Challenge(ChallengeType.StaticContainer, "mixed") with
        {
            Attachments = [new ImportAttachment("a.bin", "sha", "a.bin", "flag")]
        };
        Assert.Contains("mixed_attachment_container",
            CanonicalChallengeImportValidator.Validate(Batch(challenge)).Errors.Single());
    }

    [Fact]
    public void Unsafe_paths_and_duplicate_source_ids_are_rejected()
    {
        var first = Challenge(ChallengeType.DynamicAttachment, "same") with
        {
            Attachments = [new ImportAttachment("../escape", "sha", "../escape", "flag")]
        };
        var second = first with { SourceOrder = 2 };
        var result = CanonicalChallengeImportValidator.Validate(Batch(first, second));
        Assert.Contains(result.Errors, error => error.StartsWith("unsafe_attachment_path"));
        Assert.Contains(result.Errors, error => error.StartsWith("duplicate_source_id"));
    }

    [Fact]
    public void Unsupported_locale_falls_back_to_english_without_losing_source_value()
    {
        Assert.Equal("en", CanonicalChallengeImportValidator.NormalizeLocale("fr-FR"));
        var source = new ImportLocalizedText("fr-FR", "Titre", "Résumé", "Corps");
        Assert.Equal("fr-FR", source.Locale);
    }

    private static CanonicalChallengeImportBatch Batch(params CanonicalChallengeImport[] challenges) =>
        new("test", "fingerprint", [new CanonicalPathImport(
            "game", "game-1", "game-1", "Game", "Summary",
            [new CanonicalModuleImport("module-1", "Module", "Summary", 0, challenges)])], []);

    private static CanonicalChallengeImport Challenge(ChallengeType type, string sourceId) =>
        new("legacy", sourceId, type,
            [new ImportLocalizedText("en", sourceId, "Summary", "Body")],
            [],
            type is ChallengeType.StaticAttachment or ChallengeType.StaticContainer ? "flag" : null,
            type == ChallengeType.DynamicContainer ? "flag-{userId}" : null,
            type == ChallengeType.DynamicAttachment
                ? [new ImportAttachment("a.bin", "sha", "a.bin", "flag")]
                : [],
            type.IsContainer() ? new ImportContainerConfiguration("image", 8080, 1, 128, 256, "open") : null,
            "{\"legacy\":true}", null, 0);
}
