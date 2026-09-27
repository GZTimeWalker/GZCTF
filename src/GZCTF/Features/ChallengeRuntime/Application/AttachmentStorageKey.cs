namespace GZCTF.Features.ChallengeRuntime.Application;

public static class AttachmentStorageKey
{
    public static string Normalize(string? key, string fileName)
    {
        if (string.IsNullOrWhiteSpace(key)) return fileName;
        var path = Uri.TryCreate(key, UriKind.Absolute, out var uri) ? uri.AbsolutePath : key;
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && parts[0].Equals("assets", StringComparison.OrdinalIgnoreCase) &&
            parts[1].Length == 64 && parts[1].All(Uri.IsHexDigit))
        {
            var hash = parts[1].ToLowerInvariant();
            return $"uploads/{hash[..2]}/{hash[2..4]}/{hash}";
        }
        return key;
    }
}
