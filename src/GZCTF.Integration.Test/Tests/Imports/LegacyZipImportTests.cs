using System.Security.Cryptography;
using System.IO.Compression;
using System.Text;
using GZCTF.Features.Imports.Infrastructure;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Imports;

public sealed class LegacyZipImportTests
{
    private const string MinimalManifest =
        """
        {"sourceType":"legacy-zip","fingerprintSha256":"","paths":[],"exercises":[]}
        """;

    [Fact]
    public void Golden_package_has_a_stable_fingerprint_and_expected_paths()
    {
        var package = File.ReadAllBytes(Fixture("legacy-game.zip"));
        var fingerprint = Convert.ToHexString(SHA256.HashData(package)).ToLowerInvariant();
        var expected = File.ReadAllText(Fixture("expected-parity.json"));

        Assert.Equal(64, fingerprint.Length);
        Assert.Contains("附件/静态.bin", expected);
        Assert.Contains("legacy-dynamic-b", expected);
        Assert.NotEmpty(package);
    }

    [Fact]
    public async Task Directory_entries_are_ignored()
    {
        await using var package = CreatePackage(archive =>
        {
            archive.CreateEntry("attachments/");
            WriteEntry(archive, "manifest.json", MinimalManifest);
            WriteEntry(archive, "attachments/readme.txt", "content");
        });

        var result = await new LegacyZipSource().ReadAsync(package);

        Assert.DoesNotContain("attachments/", result.Files.Keys);
        Assert.Equal("content", Encoding.UTF8.GetString(result.Files["attachments/readme.txt"]));
    }

    [Fact]
    public async Task Actual_expanded_bytes_are_limited_while_streaming()
    {
        await using var package = CreatePackage(archive =>
        {
            WriteEntry(archive, "manifest.json", MinimalManifest);
            WriteEntry(archive, "payload.bin", new string('x', 300));
        });

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            new LegacyZipSource(maxExpandedBytes: 256).ReadAsync(package));

        Assert.Equal("import.expanded_size_limit", exception.Message);
    }

    private static MemoryStream CreatePackage(Action<ZipArchive> write)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            write(archive);
        stream.Position = 0;
        return stream;
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(path).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string Fixture(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Legacy", name);
}
