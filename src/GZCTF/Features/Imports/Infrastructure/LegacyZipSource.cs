using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using GZCTF.Features.Imports.Application;

namespace GZCTF.Features.Imports.Infrastructure;

public sealed record LegacyZipPackage(
    CanonicalChallengeImportBatch Graph,
    string FingerprintSha256,
    IReadOnlyDictionary<string, byte[]> Files);

public sealed class LegacyZipSource
{
    public const long MaxExpandedBytes = 128 * 1024 * 1024;
    private readonly long _maxExpandedBytes;

    public LegacyZipSource(long maxExpandedBytes = MaxExpandedBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExpandedBytes);
        _maxExpandedBytes = maxExpandedBytes;
    }

    public async Task<LegacyZipPackage> ReadAsync(Stream source, CancellationToken token = default)
    {
        await using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, token);
        var bytes = buffer.ToArray();
        var fingerprint = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read, leaveOpen: false);
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        long expanded = 0;
        ZipArchiveEntry? manifest = null;
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
                continue;

            var path = NormalizeEntryPath(entry.FullName);
            if (path is null)
                throw new InvalidDataException("import.unsafe_archive_path");
            if (!files.TryAdd(path, []))
                throw new InvalidDataException("import.duplicate_archive_entry");
            if (entry.Length > _maxExpandedBytes)
                throw new InvalidDataException("import.expanded_size_limit");
            await using var entryStream = entry.Open();
            await using var file = new MemoryStream();
            var copyBuffer = new byte[81920];
            long entryBytes = 0;
            while (true)
            {
                var read = await entryStream.ReadAsync(copyBuffer, token);
                if (read == 0)
                    break;
                entryBytes += read;
                expanded += read;
                if (entryBytes > _maxExpandedBytes || expanded > _maxExpandedBytes)
                    throw new InvalidDataException("import.expanded_size_limit");
                await file.WriteAsync(copyBuffer.AsMemory(0, read), token);
            }
            files[path] = file.ToArray();
            if (path.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
                manifest = entry;
        }

        if (manifest is null)
            throw new InvalidDataException("import.manifest_missing");
        var manifestJson = System.Text.Encoding.UTF8.GetString(files[NormalizeEntryPath(manifest.FullName)!])
            .TrimStart('\uFEFF');
        var graph = JsonSerializer.Deserialize<CanonicalChallengeImportBatch>(manifestJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidDataException("import.manifest_invalid");
        return new LegacyZipPackage(graph with { FingerprintSha256 = fingerprint }, fingerprint, files);
    }

    public static string? NormalizeEntryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith('/') || path.Contains(':'))
            return null;
        var normalized = path.Replace('\\', '/');
        var segments = normalized.Split('/');
        return segments.Any(segment => segment is "" or "." or "..") ? null : normalized;
    }
}
