using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test;

/// <summary>
/// Collection to ensure tests don't run in parallel (share the same factory instance)
/// </summary>
[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<GZCTFApplicationFactory>
{
}

/// <summary>
/// Basic API integration tests to verify server is running and responding
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class BasicApiTests
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
    public async Task Server_IsRunning_AndResponding()
    {
        // Verify the server is running by checking an actual API endpoint
        var response = await _client.GetAsync("/api/Config");
        _output.WriteLine($"Status: {response.StatusCode}");
        
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Api_Config_ReturnsServerConfig()
    {
        var response = await _client.GetAsync("/api/Config");
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
        var response = await _client.PostAsJsonAsync("/api/Account/Register", new { });
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
        var response = await _client.GetAsync("/api/Account/Profile");
        _output.WriteLine($"Status: {response.StatusCode}");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
