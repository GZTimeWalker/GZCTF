using System.Diagnostics.CodeAnalysis;

namespace GZCTF.Storage.Interface;

/// <summary>
/// Utility helpers for working with storage object keys in a provider-agnostic fashion.
/// </summary>
public static class StoragePath
{
    /// <summary>
    /// Normalizes the provided path by trimming leading/trailing separators and collapsing duplicate separators.
    /// </summary>
    public static string Normalize(string? path) =>
        string.IsNullOrWhiteSpace(path) ? string.Empty : string.Join('/', Split(path));

    /// <summary>
    /// Combines the provided path segments using forward slashes as separators.
    /// </summary>
    public static string Combine(params string[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        return string.Join('/',
            segments.SelectMany(Split)
                .Where(static segment => segment.Length > 0));
    }

    /// <summary>
    /// Splits the provided path into normalized segments.
    /// </summary>
    internal static IEnumerable<string> Split(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            yield break;

        foreach (var segment in path.Split(['/', '\\'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            yield return segment;
    }

    /// <summary>
    /// Attempts to retrieve the directory portion of the provided path.
    /// </summary>
    public static bool TryGetDirectoryName(string path, [MaybeNullWhen(false)] out string directory)
    {
        var segments = Split(path).ToArray();
        if (segments.Length <= 1)
        {
            directory = null;
            return false;
        }

        directory = string.Join('/', segments[..^1]);
        return true;
    }
}
