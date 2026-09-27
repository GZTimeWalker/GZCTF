using System;
using GZCTF.Extensions.Startup;
using Xunit;

namespace GZCTF.Test.UnitTests.Features.Shared;

public class DatabaseConnectionDiagnosticTests
{
    [Fact]
    public void Parse_ValidConnectionString_KeepsOnlyHostAndDatabase()
    {
        const string connectionString =
            "Host=postgres.internal;Port=5432;Database=gzctf;" +
            "Username=admin;Password=secret;Application Name=learning-platform";

        var diagnostic = DatabaseConnectionDiagnostic.Parse(connectionString);

        Assert.Equal("postgres.internal", diagnostic.Host);
        Assert.Equal("gzctf", diagnostic.Database);
        Assert.DoesNotContain("admin", diagnostic.LogValue, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", diagnostic.LogValue, StringComparison.Ordinal);
        Assert.DoesNotContain("learning-platform", diagnostic.LogValue, StringComparison.Ordinal);
        Assert.DoesNotContain("5432", diagnostic.LogValue, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Password=secret;this is not valid")]
    public void Parse_MissingOrMalformedConnectionString_DoesNotEchoInput(string? connectionString)
    {
        var diagnostic = DatabaseConnectionDiagnostic.Parse(connectionString);

        Assert.DoesNotContain("secret", diagnostic.LogValue, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("this is not valid", diagnostic.LogValue,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ControlCharacters_RemovesLogInjectionCharacters()
    {
        const string connectionString =
            "Host=postgres\nforged;Database=gzctf\rfake;Username=user;Password=secret";

        var diagnostic = DatabaseConnectionDiagnostic.Parse(connectionString);

        Assert.DoesNotContain('\n', diagnostic.LogValue);
        Assert.DoesNotContain('\r', diagnostic.LogValue);
    }
}
