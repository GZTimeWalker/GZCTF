using YamlDotNet.Serialization;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// In-memory shape of one <c>challenge.yaml</c> / <c>challenge.yml</c>
/// file parsed by <see cref="GZCTF.Services.Transfer.ChallengeImportService"/>.
/// Mirrors the subset of the gzcli schema that maps onto a
/// <see cref="GameChallenge"/>.
///
/// Property names are kebab/camel-cased in YAML (gzcli convention); the
/// <see cref="YamlMemberAttribute"/> alias makes the binder accept both
/// gzcli's snake_case and the camelCase used in some community templates.
/// Unrecognized keys are silently ignored.
/// </summary>
public sealed class ChallengeYamlModel
{
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "author")]
    public string? Author { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Maps to <see cref="GZCTF.Utils.ChallengeType"/>. Accepted values:
    /// <c>StaticAttachment</c>, <c>StaticContainer</c>,
    /// <c>DynamicAttachment</c>, <c>DynamicContainer</c>.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string? Type { get; set; }

    [YamlMember(Alias = "category")]
    public string? Category { get; set; }

    [YamlMember(Alias = "min_score_rate")]
    public double? MinScoreRate { get; set; }

    [YamlMember(Alias = "difficulty")]
    public double? Difficulty { get; set; }

    [YamlMember(Alias = "hints")]
    public List<string>? Hints { get; set; }

    [YamlMember(Alias = "flags")]
    public List<string>? Flags { get; set; }

    [YamlMember(Alias = "flag_template")]
    public string? FlagTemplate { get; set; }

    /// <summary>
    /// Relative path inside the package root pointing at a single attachment
    /// file (e.g. <c>./attachments/binary.zip</c>). gzcli also accepts
    /// directories — we only support a single file in v1.
    /// </summary>
    [YamlMember(Alias = "provide")]
    public string? Provide { get; set; }

    [YamlMember(Alias = "disable_blood_bonus")]
    public bool? DisableBloodBonus { get; set; }

    [YamlMember(Alias = "submission_limit")]
    public int? SubmissionLimit { get; set; }

    [YamlMember(Alias = "container")]
    public ContainerSection? Container { get; set; }

    public sealed class ContainerSection
    {
        /// <summary>
        /// Either a published image reference (e.g. <c>nginx:alpine</c>,
        /// <c>ghcr.io/foo/bar:tag</c>) or a relative path to a Dockerfile.
        /// The latter is rejected by the importer in v1 — server-side
        /// docker build is not yet wired.
        /// </summary>
        [YamlMember(Alias = "container_image")]
        public string? ContainerImage { get; set; }

        [YamlMember(Alias = "flag_template")]
        public string? FlagTemplate { get; set; }

        [YamlMember(Alias = "memory_limit")]
        public int? MemoryLimit { get; set; }

        [YamlMember(Alias = "cpu_count")]
        public int? CpuCount { get; set; }

        [YamlMember(Alias = "storage_limit")]
        public int? StorageLimit { get; set; }

        [YamlMember(Alias = "expose_port")]
        public int? ExposePort { get; set; }

        [YamlMember(Alias = "network_mode")]
        public string? NetworkMode { get; set; }

        [YamlMember(Alias = "enable_traffic_capture")]
        public bool? EnableTrafficCapture { get; set; }
    }
}
