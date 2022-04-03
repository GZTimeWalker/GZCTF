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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CTFServer.Test;

public class ContainerServiceTest
{
    private readonly IContainerService service;
    private readonly ITestOutputHelper output;
    private readonly HttpClient httpClient;
                
    public ContainerServiceTest(ITestOutputHelper _output)
    {
        LogManager.GlobalThreshold = LogLevel.Off;
        var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {

            });
        httpClient = app.CreateClient();
        service = app.Services.GetRequiredService<IContainerService>();
        output = _output;
    }

    [Fact]
    public async void BasicInfo()
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
    public async void CreateThenDestory()
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
