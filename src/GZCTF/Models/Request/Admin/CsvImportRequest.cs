using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Request for batch user import. Column mapping and row editing happen client-side;
/// this model carries the final, user-reviewed list of rows ready for creation.
/// </summary>
public class CsvImportRequest
{
    /// <summary>Pre-parsed, user-reviewed rows to import.</summary>
    [Required]
    public List<CsvRowModel> Rows { get; set; } = [];

    /// <summary>Team assignment mode: "fromrow" | "single" | "none"</summary>
    public string TeamMode { get; set; } = "fromrow";

    /// <summary>Fixed team name when TeamMode is "single"</summary>
    public string? SingleTeamName { get; set; }

    /// <summary>Auto-confirm email so users can log in immediately</summary>
    public bool EmailConfirmed { get; set; } = true;
}

/// <summary>A single user row after client-side CSV parsing and user editing.</summary>
public class CsvRowModel
{
    /// <summary>Email address (required, validated server-side)</summary>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>Real/display name — used to auto-generate the username if UserNameOverride is absent</summary>
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// Optional username override. When provided, the server uses this value (deduplicated if needed)
    /// instead of auto-generating from RealName. Leave blank for auto-generate.
    /// </summary>
    public string? UserNameOverride { get; set; }

    /// <summary>Team name from CSV (used when TeamMode = "fromrow")</summary>
    public string? TeamName { get; set; }

    public string? StdNumber { get; set; }

    public string? Phone { get; set; }
}
