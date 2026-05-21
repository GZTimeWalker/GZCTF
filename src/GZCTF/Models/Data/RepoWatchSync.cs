using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// Append-only audit row written by the <see cref="GZCTF.Services.Transfer.RepoWatchService"/>
/// hosted service on every tick. Used by the admin UI to render a sync
/// history per watch (success counts, errors, commit shas).
/// </summary>
[Index(nameof(RepoWatchId), nameof(RanAtUtc))]
public sealed class RepoWatchSync
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RepoWatchId { get; set; }

    [JsonIgnore]
    public RepoWatch RepoWatch { get; set; } = null!;

    [Required]
    public DateTimeOffset RanAtUtc { get; set; }

    /// <summary>
    /// HEAD commit observed at the time of the sync. Null when the GitHub
    /// API call itself failed.
    /// </summary>
    [MaxLength(64)]
    public string? CommitSha { get; set; }

    public int Imported { get; set; }

    public int Updated { get; set; }

    public int Skipped { get; set; }

    public int Failed { get; set; }

    [MaxLength(2048)]
    public string? ErrorMessage { get; set; }
}
