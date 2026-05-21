using GZCTF.Utils;

namespace GZCTF.Models.Response.Admin;

/// <summary>
/// Body returned by <c>GET /api/Edit/Games/{id}/Challenges/{cId}/AuditMeta</c>.
/// Carries enough source-of-truth detail for an admin to verify a pending
/// challenge before approving: the raw YAML, the file tree, and previews
/// of common reviewer-targeted files (READMEs / writeups / solver
/// scripts).
/// </summary>
public sealed class ChallengeAuditModel
{
    /// <summary>
    /// Verbatim contents of <c>challenge.yaml</c> / <c>challenge.yml</c>
    /// as the submitter packaged it. Null when no yaml could be found.
    /// </summary>
    public string? YamlText { get; set; }

    /// <summary>
    /// Every regular file under the package directory.
    /// </summary>
    public ChallengeAuditFile[] Files { get; set; } = [];

    /// <summary>
    /// UTF-8 previews (first ~8 KiB) of files whose name matches the
    /// common writeup / solver conventions. Keyed by relative path.
    /// </summary>
    public Dictionary<string, string> Previews { get; set; } = [];

    /// <summary>
    /// True when an archive blob is on file and downloadable via
    /// <c>AuditArchive</c>.
    /// </summary>
    public bool ArchiveAvailable { get; set; }

    /// <summary>Most recent build outcome.
    /// <see cref="ChallengeBuildStatus.None"/> for challenges that ship
    /// a registry-published image; Success / Failed for auto-built
    /// challenges.</summary>
    public ChallengeBuildStatus BuildStatus { get; set; }

    /// <summary>Tail of the last build log — surfaced so admins can
    /// diagnose Failed builds without re-running the build.</summary>
    public string? LastBuildLog { get; set; }
}

public sealed class ChallengeAuditFile
{
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
}
