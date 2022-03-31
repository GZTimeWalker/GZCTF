using Xunit;
using CTFServer.Services;
using CTFServer.Services.Interface;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;
using Xunit.Abstractions;
using System.Collections.Generic;
using Docker.DotNet.Models;
using NLog;
using System;

namespace CTFServer.Test;

public class ContainerServiceTest
{
    public IContainerService service;
    private readonly ITestOutputHelper output;

    public ContainerServiceTest(ITestOutputHelper _output)
    {
        LogManager.GlobalThreshold = LogLevel.Off;

        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        service = new ContainerService(builder.Build());
        output = _output;
    }

    [Fact]
    public async void InfoTest()
    {
        (var ver, var info) = await service.GetHostInfo();

        Assert.NotNull(info);
        Assert.NotNull(ver);

        output.WriteLine("[[ Version ]]");
        foreach (var item in ver.GetType().GetProperties())
        {
            var val = item.GetValue(ver);
            if (val is IEnumerable<object> vals)
                val = string.Join(", ", vals);
            output.WriteLine($"{item.Name,-20}: {val}");
        }

        output.WriteLine("[[ Info ]]");
        foreach (var item in info.GetType().GetProperties())
        {
            var val = item.GetValue(info);
            if (val is IEnumerable<object> vals)
                val = string.Join(", ", vals);
            output.WriteLine($"{item.Name,-20}: {val}");
        }
    }

    [Fact]
    public async void CreateThenDestoryTest()
    {
        var parameters = new CreateContainerParameters()
        {
            Image = "busybox",
            Name = $"test_{Guid.NewGuid().ToString("N")[..6]}",
            ExposedPorts = new Dictionary<string, EmptyStruct> { { "80/tcp", default } },
            Entrypoint = new List<string> { "/bin/busybox", "sleep", "360000" },
            HostConfig = new()
            {
                PublishAllPorts = true,
                Memory = 64 * 1024 * 1024,
                CPUCount = 1
            },
        };

        var container = await service.CreateContainer(parameters);

        Assert.NotNull(container);
        output.WriteLine($"[{DateTime.Now.ToString("u")}] Container Created.");

        output.WriteLine("[[ Container Info ]]");
        foreach (var item in container!.GetType().GetProperties())
        {
            var val = item.GetValue(container);
            if (val is IEnumerable<object> vals)
                val = string.Join(", ", vals);
            output.WriteLine($"{item.Name,-20}: {val}");
        }

        var status = await service.DestoryContainer(container);

        Assert.Equal(TaskStatus.Success, status);
        Assert.Equal(ContainerStatus.Stop, container.Status);
        output.WriteLine($"[{DateTime.Now.ToString("u")}] Container Destoryed.");
    }
}
