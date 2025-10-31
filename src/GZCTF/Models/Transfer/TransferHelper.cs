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
}
