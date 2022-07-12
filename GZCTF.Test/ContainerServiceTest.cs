using CTFServer.Models.Internal;
using CTFServer.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;
using Xunit.Abstractions;

namespace CTFServer.Test;

public class ContainerServiceTest
{
    private readonly IContainerService service;
    private readonly ITestOutputHelper output;
    private readonly HttpClient httpClient;

    public ContainerServiceTest(ITestOutputHelper _output)
    {
        var app = new TestWebAppFactory<Program>();
        httpClient = app.CreateClient();
        service = app.Services.GetRequiredService<IContainerService>();
        output = _output;
    }

    [Fact]
    public async void BasicInfo()
    {
        var info = await service.GetHostInfo();

        output.WriteLine(info);
    }

    [Fact]
    public async void CreateThenDestory()
    {
        var config = new ContainerConfig()
        {
            Image = "busybox",
            ExposedPort = 80,
            CPUCount = 1,
            Flag = "flag{the_test_flag}",
            MemoryLimit = 64
        };

        var container = await service.CreateContainer(config);

        Assert.NotNull(container);
        output.WriteLine($"[{DateTime.Now:u}] Container Created.");

        output.WriteLine("[[ Container Info ]]");
        foreach (var item in container!.GetType().GetProperties())
        {
            var val = item.GetValue(container);
            if (val is IEnumerable<object> vals)
                val = string.Join(", ", vals);
            output.WriteLine($"{item.Name,-20}: {val}");
        }

        await service.DestoryContainer(container);

        Assert.Equal(ContainerStatus.Destoryed, container.Status);

        output.WriteLine($"[{DateTime.Now:u}] Container Destoryed.");
    }
}