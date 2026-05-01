using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Admin;

/// <summary>Summary result of a CSV import operation</summary>
public class CsvImportResultModel
{
    /// <summary>Total data rows in the CSV (excluding header)</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Users successfully created</summary>
    [JsonPropertyName("created")]
    public int Created { get; set; }

    /// <summary>Existing users whose credentials were updated</summary>
    [JsonPropertyName("updated")]
    public int Updated { get; set; }

    /// <summary>Rows skipped due to validation errors or duplicate emails</summary>
    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }

    /// <summary>Per-user results including generated credentials</summary>
    [JsonPropertyName("users")]
    public List<CsvImportUserResult> Users { get; set; } = [];
}

/// <summary>Result for a single imported user</summary>
public class CsvImportUserResult
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("realName")]
    public string RealName { get; set; } = string.Empty;

    /// <summary>Auto-generated or deduplicated username</summary>
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Auto-generated password (only returned once — admin must download it)</summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("teamName")]
    public string? TeamName { get; set; }

    /// <summary>"created" | "updated" | "skipped"</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "created";

    /// <summary>Error description when Status is "skipped"</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
