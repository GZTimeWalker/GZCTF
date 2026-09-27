using System.Text.Json;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Imports;

public sealed class LegacyDatabaseMigrationTests
{
    [Fact]
    public void Golden_database_fixture_contains_roles_and_four_challenge_modes()
    {
        var sql = File.ReadAllText(Fixture("legacy-database.sql"));
        var parity = JsonDocument.Parse(File.ReadAllText(Fixture("expected-parity.json")));

        Assert.True(sql.Split("legacy-", StringSplitOptions.None).Length - 1 >= 4);
        Assert.Equal(4, parity.RootElement.GetProperty("challenges").GetArrayLength());
        Assert.Contains("legacy-admin", sql);
        Assert.Contains("legacy-monitor", sql);
        Assert.Contains("legacy-banned", sql);
    }

    private static string Fixture(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Legacy", name);
}
