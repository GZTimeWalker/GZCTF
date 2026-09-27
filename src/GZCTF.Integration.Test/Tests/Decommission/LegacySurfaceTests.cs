using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Decommission;

[Collection(nameof(IntegrationTestCollection))]
public sealed class LegacySurfaceTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public void Competition_routes_and_monitor_hub_are_not_registered()
    {
        _ = factory.CreateClient();
        var routes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(routes, route => HasPrefix(route, "api/game"));
        Assert.DoesNotContain(routes, route => HasPrefix(route, "api/team"));
        Assert.DoesNotContain(routes, route => HasPrefix(route, "api/exercise"));
        Assert.DoesNotContain(routes, route => HasPrefix(route, "api/admin/teams"));
        Assert.DoesNotContain(routes, route => HasPrefix(route, "api/admin/participation"));
        Assert.DoesNotContain(routes, route => HasPrefix(route, "api/admin/writeups"));
        Assert.DoesNotContain(routes, route => HasPrefix(route, "hub/monitor"));
        Assert.DoesNotContain(routes, route => HasPrefix(route, "hub/user"));
    }

    [Theory]
    [InlineData("api/admin/imports")]
    [InlineData("hub/dashboard")]
    [InlineData("api/skill-tree-redirects")]
    public void Replacement_routes_are_registered(string expected)
    {
        _ = factory.CreateClient();
        var routes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty);

        Assert.Contains(routes, route => HasPrefix(route, expected));
    }

    [Theory]
    [InlineData("/api/learning-paths")]
    [InlineData("/api/admin/learning-paths")]
    public async Task Retired_learning_apis_are_not_routable(string route)
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static bool HasPrefix(string route, string prefix) =>
        route.TrimStart('/').StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
}
