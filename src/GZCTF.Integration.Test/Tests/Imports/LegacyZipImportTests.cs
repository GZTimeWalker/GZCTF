using System.Security.Cryptography;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Imports;

public sealed class LegacyZipImportTests
{
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

    private static string Fixture(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Legacy", name);
}
