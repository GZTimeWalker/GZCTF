using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test;

/// <summary>
/// Basic API integration tests to verify server is running and responding
/// </summary>
public class BasicApiTests : IClassFixture<GZCTFApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;
    private readonly GZCTFApplicationFactory _factory;

    public BasicApiTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Health endpoint is on metrics port (3000), but we disabled it in tests
        // Instead we test the root endpoint to verify server is running
        var response = await _client.GetAsync("/");
        _output.WriteLine($"Status: {response.StatusCode}");
        
        // Should return OK or redirect, either is fine for basic health check
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.MovedPermanently,
            $"Expected OK, Redirect, or MovedPermanently but got {response.StatusCode}"
        );
    }

    [Fact]
    public async Task Api_Info_ReturnsServerInfo()
    {
        var response = await _client.GetAsync("/api/info");
        _output.WriteLine($"Status: {response.StatusCode}");
        
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {content}");
        
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task Api_OpenApi_ReturnsSpec()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        _output.WriteLine($"Status: {response.StatusCode}");
        
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response length: {content.Length}");
        
        Assert.NotEmpty(content);
        Assert.Contains("openapi", content.ToLower());
    }

    [Fact]
    public async Task Api_Account_Register_WithoutData_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/account/register", new { });
        _output.WriteLine($"Status: {response.StatusCode}");
        
        // Should fail validation
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected BadRequest or UnprocessableEntity but got {response.StatusCode}"
        );
    }

    [Fact]
    public async Task Api_Unauthenticated_Profile_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/account/profile");
        _output.WriteLine($"Status: {response.StatusCode}");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
