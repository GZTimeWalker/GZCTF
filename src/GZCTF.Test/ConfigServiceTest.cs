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
    public void TestGetConfigs()
    {
        HashSet<Config>? configs = ConfigService.GetConfigs(new TestConfig());
        Assert.True(configs is not null);
        Assert.True(configs.Count > 0);

        foreach (Config config in configs)
            output.WriteLine($"{config.ConfigKey,-32}={config.Value}");
    }
}

public class TestConfig
{
    public AccountPolicy AccountPolicy { get; set; } = new();
    public DockerConfig DockerConfig { get; set; } = new();
    public EmailConfig EmailConfig { get; set; } = new();
}
