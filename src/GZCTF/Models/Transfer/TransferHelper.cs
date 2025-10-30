using System.Security;
using System.Security.Cryptography;
using System.Text;
using CsToml;

namespace GZCTF.Models.Transfer;

/// <summary>
/// Helper utilities for game transfer operations
/// </summary>
public static class TransferHelper
{
    /// <summary>
    /// Current transfer format version
    /// </summary>
    public const string CurrentVersion = "1.0";

    /// <summary>
    /// Transfer format identifier
    /// </summary>
    public const string FormatIdentifier = "GZCTF-GAME";

    static readonly CsTomlSerializerOptions Options = CsTomlSerializerOptions.Default with
    {
        SerializeOptions = new SerializeOptions
        {
            TableStyle = TomlTableStyle.Header,
            DefaultNullHandling = TomlNullHandling.Ignore
        }
    };

    /// <summary>
    /// Serialize object to TOML string
    /// </summary>
    public static string ToToml<T>(T obj) where T : class
    {
        var result = CsTomlSerializer.Serialize(obj, Options);
        return Encoding.UTF8.GetString(result.ByteSpan);
    }

    /// <summary>
    /// Deserialize TOML string to object
    /// </summary>
    public static T FromToml<T>(string toml) where T : class => FromToml<T>(Encoding.UTF8.GetBytes(toml));

    /// <summary>
    /// Deserialize TOML bytes to object
    /// </summary>
    public static T FromToml<T>(ReadOnlySpan<byte> utf8Toml) where T : class =>
        CsTomlSerializer.Deserialize<T>(utf8Toml);

    /// <summary>
    /// Deserialize TOML file to object
    /// </summary>
    public static async Task<T> FromTomlFileAsync<T>(string filePath, CancellationToken token = default)
        where T : class
    {
        var bytes = await File.ReadAllBytesAsync(filePath, token);
        return FromToml<T>(bytes);
    }

    /// <summary>
    /// Compute SHA256 hash of UTF-8 string content
    /// </summary>
    public static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    /// <summary>
    /// Compute SHA256 hash of byte content
    /// </summary>
    public static string ComputeHash(ReadOnlySpan<byte> content)
    {
        var hash = SHA256.HashData(content);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    /// <summary>
    /// Compute SHA256 hash of stream
    /// </summary>
    public static async Task<string> ComputeHashAsync(Stream stream, CancellationToken token = default)
    {
        var hash = await SHA256.HashDataAsync(stream, token);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    /// <summary>
    /// Compute SHA256 hash of file
    /// </summary>
    public static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken token = default)
    {
        await using var stream = File.OpenRead(filePath);
        return await ComputeHashAsync(stream, token);
    }

    /// <summary>
    /// Create transfer manifest
    /// </summary>
    public static TransferManifest CreateManifest(
        Game game,
        int challengeCount,
        int totalFlags,
        int totalFiles,
        long totalSize,
        int divisionCount,
        string exporterVersion) =>
        new()
        {
            Version = CurrentVersion,
            Format = FormatIdentifier,
            ExportedAt = DateTimeOffset.UtcNow,
            ExporterVersion = exporterVersion,
            Game = new GameInfoSection { Id = game.Id, Title = game.Title },
            Statistics = new StatisticsSection
            {
                ChallengeCount = challengeCount,
                TotalFlags = totalFlags,
                TotalFiles = totalFiles,
                TotalFileSize = totalSize,
                DivisionCount = divisionCount
            },
            Checksum = new ChecksumSection { Algorithm = "SHA256" }
        };

    /// <summary>
    /// Sanitize filename for safe file system operations
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalid));

        // Limit length
        if (sanitized.Length > 50)
            sanitized = sanitized[..50];

        return sanitized.ToLowerInvariant();
    }

    /// <summary>
    /// Validate path to prevent directory traversal
    /// </summary>
    public static void ValidatePath(string path, string baseDir)
    {
        var fullPath = Path.GetFullPath(Path.Combine(baseDir, path));
        var basePath = Path.GetFullPath(baseDir);

        if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException($"Path traversal detected: {path}");
        }
    }

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
