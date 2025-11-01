using System.Text.Encodings.Web;
using System.Text.Json;

namespace GZCTF.Models.Transfer;

/// <summary>
/// Helper utilities for game transfer operations
/// </summary>
public static class TransferHelper
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ToJson<T>(T obj) where T : class => JsonSerializer.Serialize(obj, JsonOptions);

    public static T? FromJson<T>(string json) where T : class => JsonSerializer.Deserialize<T>(json, JsonOptions);

    /// <summary>
    /// Get GZCTF version for export metadata
    /// </summary>
    public static string GetExporterVersion()
    {
        var assembly = typeof(TransferHelper).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Compute SHA256 hash of file using streaming for memory efficiency
    /// </summary>
    public static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct = default)
    {
        await using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, FileOptions.SequentialScan | FileOptions.Asynchronous);
        using var hasher = System.Security.Cryptography.SHA256.Create();
        var hash = await hasher.ComputeHashAsync(file, ct);
        return Convert.ToHexStringLower(hash);
    }
}
