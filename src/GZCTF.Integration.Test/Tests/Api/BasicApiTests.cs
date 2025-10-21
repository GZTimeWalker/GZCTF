using System;
using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Fixtures;
using GZCTF.Models;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

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
    public async Task Api_Account_Register_And_Profile_Succeeds()
    {
        using var client = _factory.CreateClient();

        var userName = $"t{Guid.NewGuid():N}".Substring(0, Limits.MaxUserNameLength);
        var email = $"{userName}@example.com";

        var registerModel = new RegisterModel
        {
            UserName = userName,
            Email = email,
            Password = "P@ssw0rd!123"
        };

        var registerResponse = await client.PostAsJsonAsync("/api/Account/Register", registerModel);
        _output.WriteLine($"Register status: {registerResponse.StatusCode}");
        registerResponse.EnsureSuccessStatusCode();

        var payload = await registerResponse.Content.ReadFromJsonAsync<RequestResponse<RegisterStatus>>();
        Assert.NotNull(payload);
        Assert.Equal(RegisterStatus.LoggedIn, payload!.Data);

        var profileResponse = await client.GetAsync("/api/Account/Profile");
        _output.WriteLine($"Profile status: {profileResponse.StatusCode}");
        profileResponse.EnsureSuccessStatusCode();

        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileUserInfoModel>();
        Assert.NotNull(profile);
        Assert.Equal(registerModel.UserName, profile!.UserName);
        Assert.Equal(registerModel.Email, profile.Email);
    }

    [Fact]
    public async Task Api_Unauthenticated_Profile_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Account/Profile");
        _output.WriteLine($"Status: {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
