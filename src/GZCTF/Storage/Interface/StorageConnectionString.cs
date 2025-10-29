namespace GZCTF.Storage.Interface;

static class StorageConnectionString
{
    public static (string Scheme, Dictionary<string, string> Parameters) Parse(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var trimmed = connectionString.Trim();
        var parts = trimmed.Split(["://"], 2, StringSplitOptions.TrimEntries);

        var scheme = parts[0].ToLowerInvariant();
        Dictionary<string, string> parameters = new(StringComparer.OrdinalIgnoreCase);

        if (parts.Length == 1)
            return (scheme, parameters);

        foreach (var segment in parts[1].Split(';',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var index = segment.IndexOf('=');
            if (index <= 0)
                continue;

            var key = segment[..index];
            var value = segment[(index + 1)..];
            parameters[key] = value;
        }

        return (scheme, parameters);
    }
}
