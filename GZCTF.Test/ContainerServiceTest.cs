using CTFServer.Models.Internal;
using CTFServer.Services.Interface;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CTFServer.Test;

public class ContainerServiceTest : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory factory;
    private readonly ITestOutputHelper output;

    public ContainerServiceTest(ITestOutputHelper _output, TestWebAppFactory _factory)
    {
        factory = _factory;
        output = _output;
    }

    [Fact]
    public async void BasicInfo()
    {
        var service = factory.Services.GetRequiredService<IContainerService>();
        var info = await service.GetHostInfo();

        output.WriteLine(info);
    }

    [Fact]
    public async void CreateThenDestory()
    {
        var service = factory.Services.GetRequiredService<IContainerService>();
        var config = new ContainerConfig()
        {
            Image = "ghcr.io/gztimewalker/gzctf/test",
            ExposedPort = 70,
            CPUCount = 1,
            Flag = "flag{the_test_flag}",
            MemoryLimit = 64
        };

        var container = await service.CreateContainer(config);

        Assert.NotNull(container);
        output.WriteLine($"[{DateTime.Now:u}] Container created.");

        int times = 0;

        do
        {
            await Task.Delay(1000);
            times += 1;
            output.WriteLine($"[{DateTime.Now:u}] Query: {times} times.");

            container = await service.QueryContainer(container!);

            if (container!.Status == ContainerStatus.Destoryed)
            {
                output.WriteLine($"[{DateTime.Now:u}] Container destroyed unexpected.");
                return;
            }
        } while (container!.Status != ContainerStatus.Running);

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

        output.WriteLine($"[{DateTime.Now:u}] Container destoryed.");
    }
}