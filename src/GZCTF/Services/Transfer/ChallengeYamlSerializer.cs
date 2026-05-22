using GZCTF.Models.Data;
using GZCTF.Models.Request.Edit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GZCTF.Services.Transfer;

/// <summary>
/// Round-trip the in-memory <see cref="GameChallenge"/> row back to a
/// <c>challenge.yml</c> file. Output uses the camelCase aliases the
/// parser already expects (see
/// <see cref="ChallengeYamlModel"/> — same property metadata), so a
/// re-import of a serialized yaml yields the same DB state.
///
/// <para><b>Lossy:</b> comments and unrecognized keys in the original
/// file are NOT preserved — only the fields we model survive. Operators
/// that opt into <c>PushOnEdit</c> accept this trade-off.</para>
///
/// <para><b>Excluded:</b> platform-managed state never lands in yaml —
/// <c>BuildStatus</c>, <c>BuildImageDigest</c>, <c>LastBuildLog</c>,
/// <c>OriginalScore</c>, <c>MinScore</c>, <c>OriginalArchiveBlobPath</c>.
/// Those drift from the repo as the platform manages them and would
/// cause noisy diff churn if we pushed them.</para>
/// </summary>
public static class ChallengeYamlSerializer
{
    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        // YamlDotNet writes nulls as "key:" lines, which both clutters
        // the file and re-parses to empty strings on the next read.
        // Skip them entirely.
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull
                                        | DefaultValuesHandling.OmitEmptyCollections)
        .Build();

    /// <summary>
    /// Serialize a challenge + its flags into a yaml string suitable
    /// for writing back to <see cref="GameChallenge.SourceYamlPath"/>.
    /// </summary>
    /// <param name="ch">The challenge entity with its
    /// <c>Flags</c> navigation loaded.</param>
    /// <param name="flagTexts">Flag literal strings (extracted by the
    /// caller from the FlagContext entities; we don't take the nav
    /// directly so the caller can decide how to render dynamic flag
    /// templates).</param>
    public static string Serialize(GameChallenge ch, IReadOnlyList<string> flagTexts)
    {
        var model = new ChallengeYamlModel
        {
            Name = ch.Title,
            // Author is split out of Content at import time by
            // ApplyYamlToChallenge ("Author: **X**\n\n..."), but we
            // don't reverse that here — round-tripping author back
            // through Content would require fragile string parsing.
            // Leave Author null and keep the existing Content;
            // operators editing the description still get a clean
            // round trip on everything else.
            Description = StripAuthorPrefix(ch.Content, out var extractedAuthor),
            Author = extractedAuthor,
            Type = ch.Type.ToString(),
            Category = ch.Category.ToString(),
            FlagTemplate = string.IsNullOrEmpty(ch.FlagTemplate) ? null : ch.FlagTemplate,
            Hints = ch.Hints is { Count: > 0 } ? new List<string>(ch.Hints) : null,
            Flags = flagTexts.Count > 0 ? new List<string>(flagTexts) : null,
            MinScoreRate = ch.MinScoreRate == 0.25 ? null : ch.MinScoreRate,
            Difficulty = ch.Difficulty == 3 ? null : ch.Difficulty,
            SubmissionLimit = ch.SubmissionLimit == 0 ? null : ch.SubmissionLimit,
            DisableBloodBonus = ch.DisableBloodBonus ? true : null,
            // FileName isn't in ChallengeYamlModel — the `provide:` field
            // points at the attachment file path, and we don't track the
            // attachment's relative path round-trip, so leave it as the
            // existing yaml's value (gets stripped by re-serialization).
            // Operators wanting attachment changes via push-back is a
            // separate feature.
        };

        if (ch.Type.IsContainer())
        {
            model.Container = new ChallengeYamlModel.ContainerSection
            {
                ContainerImage = ch.ContainerImage,
                MemoryLimit = ch.MemoryLimit,
                CpuCount = ch.CPUCount,
                StorageLimit = ch.StorageLimit,
                ExposePort = ch.ExposePort,
                NetworkMode = ch.NetworkMode == GZCTF.Utils.NetworkMode.Open
                    ? null
                    : ch.NetworkMode.ToString(),
                EnableTrafficCapture = ch.EnableTrafficCapture ? true : null,
                FlagTemplate = string.IsNullOrEmpty(ch.FlagTemplate) ? null : ch.FlagTemplate,
            };
        }

        return YamlSerializer.Serialize(model);
    }

    /// <summary>
    /// The importer prepends <c>"Author: **X**\n\n"</c> to the
    /// challenge description when an <c>author:</c> field is present in
    /// the yaml. Reverse that for the round trip so we don't double the
    /// prefix on every push.
    /// </summary>
    private static string StripAuthorPrefix(string content, out string? author)
    {
        author = null;
        if (string.IsNullOrEmpty(content)) return content;
        // Conservative: only match the exact shape the importer writes.
        const string prefix = "Author: **";
        if (!content.StartsWith(prefix, StringComparison.Ordinal)) return content;
        var endQuote = content.IndexOf("**\n\n", prefix.Length, StringComparison.Ordinal);
        if (endQuote < 0) return content;
        author = content[prefix.Length..endQuote];
        return content[(endQuote + 4)..];
    }
}
