using System.Collections.Generic;
using Docker.DotNet.Models;
using GZCTF.Services.Container.Manager;
using Xunit;

namespace GZCTF.Test.UnitTests.Services.Container;

public class DockerManagerTests
{
    [Fact]
    public void GetPublishedPortBindings_ReturnsMatchingProtocolBinding()
    {
        var ports = new Dictionary<string, IList<PortBinding>>
        {
            ["80/tcp"] = [new() { HostPort = "32768" }]
        };

        var bindings = DockerManager.GetPublishedPortBindings(ports, 80);

        Assert.NotNull(bindings);
        Assert.Single(bindings);
        Assert.Equal("32768", bindings[0].HostPort);
    }

    [Fact]
    public void GetPublishedPortBindings_PrefersTcpBindingWhenMultipleProtocolsMatch()
    {
        var ports = new Dictionary<string, IList<PortBinding>>
        {
            ["53/udp"] = [new() { HostPort = "32769" }],
            ["53/tcp"] = [new() { HostPort = "32768" }]
        };

        var bindings = DockerManager.GetPublishedPortBindings(ports, 53);

        Assert.NotNull(bindings);
        Assert.Single(bindings);
        Assert.Equal("32768", bindings[0].HostPort);
    }

    [Fact]
    public void GetPublishedPortBindings_DoesNotMatchPortsWithSamePrefix()
    {
        var ports = new Dictionary<string, IList<PortBinding>>
        {
            ["5300/tcp"] = [new() { HostPort = "32768" }]
        };

        var bindings = DockerManager.GetPublishedPortBindings(ports, 53);

        Assert.Null(bindings);
    }
}
