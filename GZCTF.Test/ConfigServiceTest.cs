using System;
using Xunit;
using CTFServer.Services;
using CTFServer.Models.Internal;
using Xunit.Abstractions;
using CTFServer.Services.Interface;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;

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
        EmailOptions policy = new();
        var configs = ConfigService.GetConfigs(policy);

        foreach(var config in configs)
            output.WriteLine($"{config.ConfigKey,16}:{config.Value}");
    }
}

