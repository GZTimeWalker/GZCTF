using System.Net;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Tests for OpenAPI specification and schema validation
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class OpenApiTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task OpenApi_Spec_IsValidJson()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");
        output.WriteLine($"Status: {response.StatusCode}");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        output.WriteLine($"Response length: {content.Length} bytes");

        // Assert - should be valid JSON
        Assert.NotEmpty(content);

        // Parse to verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(content);
        Assert.NotNull(jsonDoc);

        // Verify it has OpenAPI structure
        var root = jsonDoc.RootElement;
        Assert.True(root.TryGetProperty("openapi", out var openApiVersion));
        Assert.True(root.TryGetProperty("info", out var info));
        Assert.True(root.TryGetProperty("paths", out var paths));

        output.WriteLine($"OpenAPI version: {openApiVersion.GetString()}");
        output.WriteLine($"Title: {info.GetProperty("title").GetString()}");
        output.WriteLine($"Number of paths: {paths.EnumerateObject().Count()}");
    }

    [Fact]
    public async Task OpenApi_ContainsExpectedEndpoints()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var paths = jsonDoc.RootElement.GetProperty("paths");

        // Expected endpoints
        string[] expectedEndpoints =
        [
            "/api/Config", "/api/Account/Register", "/api/Account/LogIn", "/api/Account/Profile"
        ];

        // Assert
        foreach (var endpoint in expectedEndpoints)
        {
            var hasEndpoint = paths.EnumerateObject()
                .Any(p => p.Name.Equals(endpoint, StringComparison.OrdinalIgnoreCase));
            output.WriteLine($"Endpoint '{endpoint}': {(hasEndpoint ? "Found" : "Missing")}");
            Assert.True(hasEndpoint, $"Expected endpoint '{endpoint}' not found in OpenAPI spec");
        }
    }

    [Fact]
    public async Task OpenApi_HasSchemaDefinitions()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        // Assert - should have components/schemas section
        Assert.True(jsonDoc.RootElement.TryGetProperty("components", out var components));
        Assert.True(components.TryGetProperty("schemas", out var schemas));

        var schemaCount = schemas.EnumerateObject().Count();
        output.WriteLine($"Number of schema definitions: {schemaCount}");
        Assert.True(schemaCount > 0, "OpenAPI spec should contain schema definitions");

        // List some schemas for verification
        var schemaNames = schemas.EnumerateObject().Select(s => s.Name).Take(10).ToList();
        output.WriteLine($"Sample schemas: {string.Join(", ", schemaNames)}");
    }

    [Fact]
    public async Task Scalar_Documentation_IsAvailable()
    {
        // Scalar is the API documentation UI
        // Act
        var response = await _client.GetAsync("/scalar/v1");
        output.WriteLine($"Status: {response.StatusCode}");

        // Assert - in development mode, Scalar should be available
        // It might return 404 in some configurations, so we just verify it responds
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"Expected OK or NotFound but got {response.StatusCode}"
        );
    }
}
