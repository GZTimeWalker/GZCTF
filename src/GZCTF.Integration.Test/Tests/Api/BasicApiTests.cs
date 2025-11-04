using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Basic API integration tests to verify server is running and responding
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class BasicApiTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Server_IsRunning_AndResponding()
    {
        // Verify the server is running by checking an actual API endpoint
        var response = await _client.GetAsync("/api/Config");
        output.WriteLine($"Status: {response.StatusCode}");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Api_Config_ReturnsServerConfig()
    {
        var response = await _client.GetAsync("/api/Config");
        output.WriteLine($"Status: {response.StatusCode}");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        output.WriteLine($"Response: {content}");

        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task Api_OpenApi_ReturnsSpec()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        output.WriteLine($"Status: {response.StatusCode}");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        output.WriteLine($"Response length: {content.Length}");

        Assert.NotEmpty(content);
        Assert.Contains("openapi", content.ToLower());
    }

    [Fact]
    public async Task Api_Account_Register_And_Profile_Succeeds()
    {
        using var client = factory.CreateClient();

        var userName = TestDataSeeder.RandomName();
        var email = $"{userName}@example.com";

        var registerModel = new RegisterModel { UserName = userName, Email = email, Password = "P@ssw0rd!123" };

        var registerResponse = await client.PostAsJsonAsync("/api/Account/Register", registerModel);
        output.WriteLine($"Register status: {registerResponse.StatusCode}");
        registerResponse.EnsureSuccessStatusCode();

        var payload = await registerResponse.Content.ReadFromJsonAsync<RequestResponse<RegisterStatus>>();
        Assert.NotNull(payload);
        Assert.Equal(RegisterStatus.LoggedIn, payload.Data);

        var profileResponse = await client.GetAsync("/api/Account/Profile");
        output.WriteLine($"Profile status: {profileResponse.StatusCode}");
        profileResponse.EnsureSuccessStatusCode();

        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileUserInfoModel>();
        Assert.NotNull(profile);
        Assert.Equal(registerModel.UserName, profile.UserName);
        Assert.Equal(registerModel.Email, profile.Email);
    }

    [Fact]
    public async Task Api_Unauthenticated_Profile_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/Account/Profile");
        output.WriteLine($"Status: {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Fallback_CONNECT_Method_Returns405()
    {
        var request = new HttpRequestMessage(new HttpMethod("CONNECT"), "/nonexistent");
        var response = await _client.SendAsync(request);
        output.WriteLine($"CONNECT Status: {response.StatusCode}");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Fallback_GET_Method_ReturnsIndex()
    {
        var response = await _client.GetAsync("/nonexistent");
        output.WriteLine($"GET Status: {response.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}
