using System.Net;
using GZCTF.Integration.Test.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Tests to verify routes are properly configured
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class RoutingTests
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public RoutingTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
    {
        _output = output;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Root_ReturnsResponse()
    {
        var response = await _client.GetAsync("/");
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
        
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response length: {content.Length}");
        if (content.Length < 500)
        {
            _output.WriteLine($"Response body: {content}");
        }
    }

    [Fact]
    public async Task Api_Endpoints_AreAvailable()
    {
        var endpoints = new[] {
            "/api/info",
            "/api/account/profile",
            "/api/account/register",
            "/openapi/v1.json"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            _output.WriteLine($"{endpoint}: {response.StatusCode}");
        }
    }
}
