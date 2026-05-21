using YamlDotNet.Serialization;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// In-memory shape of a <c>.gzevent</c> manifest file parsed by
/// <see cref="GZCTF.Services.Transfer.RepoBindingDiscoveryService"/>.
/// Mirrors the schema gzcli publishes at
/// <c>https://raw.githubusercontent.com/dimasma0305/gzcli/refs/heads/master/internal/template/templates/others/ctf-template/.gzctf/gzevent.schema.yaml</c>.
///
/// Field names use camelCase to match the published schema; YamlDotNet
/// is configured with <see cref="YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention"/>
/// to bind them.
/// </summary>
public sealed class GzEventModel
{
    public string? Title { get; set; }

    public DateTimeOffset? Start { get; set; }

    public DateTimeOffset? End { get; set; }

    /// <summary>Repo-relative path to a poster image; not applied in v1.</summary>
    public string? Poster { get; set; }

    public bool? Hidden { get; set; }

    public string? Summary { get; set; }

    public string? Content { get; set; }

    public bool? AcceptWithoutReview { get; set; }

    public string? InviteCode { get; set; }

    public List<string>? Organizations { get; set; }

    public int? TeamMemberCountLimit { get; set; }

    public int? ContainerCountLimit { get; set; }

    public bool? PracticeMode { get; set; }

    public bool? WriteupRequired { get; set; }

    public DateTimeOffset? WriteupDeadline { get; set; }

    public string? WriteupNote { get; set; }

    public long? BloodBonus { get; set; }
}
