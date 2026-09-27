using System.IO.Compression;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GZCTF.Features.Imports.Application;
using GZCTF.Features.Imports.Infrastructure;
using Xunit;

namespace GZCTF.Test.UnitTests.Features.Imports;

public sealed class LegacyZipSourceTests
{
    [Fact]
    public async Task Valid_manifest_is_read_and_fingerprinted()
    {
        var bytes = CreateArchive("manifest.json", JsonSerializer.Serialize(
            new CanonicalChallengeImportBatch("zip", "placeholder", [], [])));
        var package = await new LegacyZipSource().ReadAsync(new MemoryStream(bytes));

        Assert.Equal(64, package.FingerprintSha256.Length);
        Assert.Equal(package.FingerprintSha256, package.Graph.FingerprintSha256);
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("/absolute")]
    [InlineData("C:/absolute")]
    [InlineData("folder//file")]
    public void Unsafe_entry_paths_are_rejected(string path) =>
        Assert.Null(LegacyZipSource.NormalizeEntryPath(path));

    [Fact]
    public async Task Duplicate_entries_are_rejected()
    {
        var bytes = CreateArchive("manifest.json", "{}");
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "manifest.json", "{}");
            AddEntry(archive, "manifest.json", "{}");
        }

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new LegacyZipSource().ReadAsync(new MemoryStream(output.ToArray())));
        Assert.NotEmpty(bytes);
    }

    private static byte[] CreateArchive(string name, string content)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            AddEntry(archive, name, content);
        return output.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
