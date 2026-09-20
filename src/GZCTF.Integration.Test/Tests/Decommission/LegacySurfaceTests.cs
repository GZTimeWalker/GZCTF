using System.Net;
using GZCTF.Integration.Test.Base;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Decommission;

[Collection(nameof(IntegrationTestCollection))]
public sealed class LegacySurfaceTests(GZCTFApplicationFactory factory)
{
    [Theory]
    [InlineData("/api/game")]
    [InlineData("/api/team")]
    [InlineData("/api/exercise")]
    public async Task Competition_api_surfaces_are_removed(string path)
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync(path);
        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
            $"{path} returned {(int)response.StatusCode}");
    }

    [Fact]
    public async Task Competition_monitor_hub_is_removed()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsync("/hub/monitor/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/learning-paths")]
    [InlineData("/api/admin/imports")]
    public async Task Replacement_routes_remain_discoverable(string path)
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync(path);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
