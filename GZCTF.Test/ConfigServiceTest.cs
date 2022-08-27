using Xunit;
using CTFServer.Services;
using CTFServer.Models.Internal;
using Xunit.Abstractions;

namespace CTFServer.Test;

public class ConfigServiceTest
{
    private readonly ITestOutputHelper output;

    public ConfigServiceTest(ITestOutputHelper _output)
    {
        output = _output;
    }

    [Fact]
    public void TestGetConfigs()
    {
        var configs = ConfigService.GetConfigs(new TestConfig());
        Assert.True(configs is not null);
        Assert.True(configs.Count > 0);

        foreach (var config in configs)
        {
            output.WriteLine($"{config.ConfigKey,-32}={config.Value}");
        }
    }
}

public class TestConfig
{
    public AccountPolicy AccoutPolicy { get; set; } = new();
    public DockerConfig DockerConfig { get; set; } = new();
    public EmailConfig EmailConfig { get; set; } = new();
}