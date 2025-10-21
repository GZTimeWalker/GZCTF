using System.Collections.Generic;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Services.Config;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Test;

public class ConfigServiceTest(ITestOutputHelper output)
{
    [Fact]
    public void TestGetConfigs_ReturnsValidConfigs()
    {
        // Arrange
        var testConfig = new TestConfig();

        // Act
        HashSet<Config>? configs = ConfigService.GetConfigs(testConfig);

        // Assert
        Assert.NotNull(configs);
        Assert.NotEmpty(configs);

        foreach (Config config in configs)
        {
            output.WriteLine($"{config.ConfigKey,-32}={config.Value}");
            Assert.NotNull(config.ConfigKey);
            Assert.NotEmpty(config.ConfigKey);
        }
    }

    [Fact]
    public void TestGetConfigs_AccountPolicy_HasExpectedKeys()
    {
        // Arrange
        var config = new TestConfig();

        // Act
        HashSet<Config>? configs = ConfigService.GetConfigs(config);

        // Assert
        Assert.Contains(configs, c => c.ConfigKey.Contains("AccountPolicy"));
    }

    [Fact]
    public void TestGetConfigs_DockerConfig_HasExpectedKeys()
    {
        // Arrange
        var config = new TestConfig();

        // Act
        HashSet<Config>? configs = ConfigService.GetConfigs(config);

        // Assert
        Assert.Contains(configs, c => c.ConfigKey.Contains("Docker"));
    }

    [Fact]
    public void TestGetConfigs_EmailConfig_HasExpectedKeys()
    {
        // Arrange
        var config = new TestConfig();

        // Act
        HashSet<Config>? configs = ConfigService.GetConfigs(config);

        // Assert
        Assert.Contains(configs, c => c.ConfigKey.Contains("Email"));
    }

    [Fact]
    public void TestGetConfigs_WithNullProperties_HandlesGracefully()
    {
        // Arrange - This tests that the service handles edge cases
        var emptyConfig = new object();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => ConfigService.GetConfigs(emptyConfig));
        Assert.Null(exception);
    }
}

public class TestConfig
{
    public AccountPolicy AccountPolicy { get; set; } = new();
    public DockerConfig DockerConfig { get; set; } = new();
    public EmailConfig EmailConfig { get; set; } = new();
}
