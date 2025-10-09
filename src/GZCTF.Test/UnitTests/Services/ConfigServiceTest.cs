using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using GZCTF.Models.Internal;
using GZCTF.Services.Config;
using GZCTF.Test.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Test.UnitTests.Services;

public class ConfigServiceTest : TestBase
{
    private readonly IConfigService _configService;
    private readonly Mock<IOptionsSnapshot<GlobalConfig>> _mockGlobalConfig;
    private readonly Mock<IOptionsSnapshot<ManagedConfig>> _mockManagedConfig;

    public ConfigServiceTest(ITestOutputHelper output) : base(output)
    {
        // Setup additional mocks
        _mockGlobalConfig = new Mock<IOptionsSnapshot<GlobalConfig>>();
        _mockManagedConfig = new Mock<IOptionsSnapshot<ManagedConfig>>();

        _mockGlobalConfig.Setup(x => x.Value).Returns(new GlobalConfig 
        { 
            Title = "Test GZCTF",
            Slogan = "Test slogan"
        });
        
        _mockManagedConfig.Setup(x => x.Value).Returns(new ManagedConfig());

        // Create the service with mocked dependencies
        _configService = new ConfigService(
            DbContext,
            MockCache.Object,
            Mock.Of<ILogger<ConfigService>>(),
            _mockGlobalConfig.Object,
            _mockManagedConfig.Object,
            Configuration
        );
    }

    [Fact]
    public void XorKey_ShouldReturnConfiguredKey()
    {
        // Act
        var xorKey = _configService.GetXorKey();

        // Assert
        xorKey.Should().NotBeEmpty();
        System.Text.Encoding.UTF8.GetString(xorKey).Should().Be("test-xor-key");
    }

    [Fact]
    public async Task SaveConfig_ShouldUpdateExistingConfig()
    {
        // Arrange
        var existingConfig = new GZCTF.Models.Data.Config
        {
            ConfigKey = "TestKey",
            Value = "OldValue"
        };
        DbContext.Configs.Add(existingConfig);
        await DbContext.SaveChangesAsync();

        var newConfig = new GZCTF.Models.Data.Config
        {
            ConfigKey = "TestKey",
            Value = "NewValue"
        };

        // Act
        await _configService.SaveConfigSet(new HashSet<GZCTF.Models.Data.Config> { newConfig });

        // Assert
        var updatedConfig = await DbContext.Configs.FindAsync("TestKey");
        updatedConfig.Should().NotBeNull();
        updatedConfig!.Value.Should().Be("NewValue");
    }

    [Fact]
    public async Task SaveConfig_ShouldAddNewConfig()
    {
        // Arrange
        var newConfig = new GZCTF.Models.Data.Config
        {
            ConfigKey = "NewTestKey",
            Value = "TestValue"
        };

        // Act
        await _configService.SaveConfigSet(new HashSet<GZCTF.Models.Data.Config> { newConfig });

        // Assert
        var savedConfig = await DbContext.Configs.FindAsync("NewTestKey");
        savedConfig.Should().NotBeNull();
        savedConfig!.Value.Should().Be("TestValue");
    }

    [Theory]
    [GzctfAutoData]
    public async Task SaveConfig_WithGenericType_ShouldSerializeCorrectly(string title, string slogan)
    {
        // Arrange
        var globalConfig = new GlobalConfig
        {
            Title = title,
            Slogan = slogan
        };

        // Act
        await _configService.SaveConfig(globalConfig);

        // Assert
        var titleConfig = await DbContext.Configs.FindAsync("GlobalConfig:Title");
        var sloganConfig = await DbContext.Configs.FindAsync("GlobalConfig:Slogan");
        
        titleConfig.Should().NotBeNull();
        titleConfig!.Value.Should().Be(title);
        sloganConfig.Should().NotBeNull();
        sloganConfig!.Value.Should().Be(slogan);
    }

    [Fact]
    public void ReloadConfig_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _configService.ReloadConfig();
        act.Should().NotThrow();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        
        // Add Config-related services
        services.AddSingleton(_mockGlobalConfig.Object);
        services.AddSingleton(_mockManagedConfig.Object);
    }
}