using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Request model for CSV-based batch user import (Admin only)
/// </summary>
public class CsvImportRequest
{
    /// <summary>Raw CSV text content (headers on first row)</summary>
    [Required]
    public string CsvText { get; set; } = string.Empty;

    /// <summary>CSV column header that maps to Real Name (required)</summary>
    [Required]
    public string RealNameColumn { get; set; } = string.Empty;

    /// <summary>CSV column header that maps to Email (required)</summary>
    [Required]
    public string EmailColumn { get; set; } = string.Empty;

    /// <summary>CSV column header that maps to Team Name (optional)</summary>
    public string? TeamNameColumn { get; set; }

    /// <summary>CSV column header that maps to Student ID (optional)</summary>
    public string? StdNumberColumn { get; set; }

    /// <summary>CSV column header that maps to Phone (optional)</summary>
    public string? PhoneColumn { get; set; }

    /// <summary>Team assignment mode: "csv" | "single" | "none"</summary>
    public string TeamMode { get; set; } = "csv";

    /// <summary>Team name used when TeamMode is "single"</summary>
    public string? SingleTeamName { get; set; }

    /// <summary>Auto-confirm email so users can log in immediately</summary>
    public bool EmailConfirmed { get; set; } = true;
}
