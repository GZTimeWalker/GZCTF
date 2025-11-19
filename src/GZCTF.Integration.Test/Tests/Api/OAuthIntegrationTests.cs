using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Services.OAuth;
using GZCTF.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Tests for OAuth authentication flow
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class OAuthIntegrationTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task Admin_CreateOAuthProvider_Succeeds()
    {
        // Arrange
        var (admin, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.Admin);
        using var client = factory.CreateAuthenticatedClient(admin);

        var providers = new Dictionary<string, OAuthProviderConfig>
        {
            ["testprovider"] = new()
            {
                Enabled = true,
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret",
                AuthorizationEndpoint = "https://test.example.com/oauth/authorize",
                TokenEndpoint = "https://test.example.com/oauth/token",
                UserInformationEndpoint = "https://test.example.com/oauth/userinfo",
                DisplayName = "Test Provider",
                Scopes = ["openid", "profile", "email"],
                FieldMapping = new Dictionary<string, string>
                {
                    { "sub", "userId" },
                    { "email", "email" },
                    { "name", "displayName" }
                }
            }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/Admin/OAuth", providers);
        output.WriteLine($"Status: {response.StatusCode}");

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify provider was created
        var getResponse = await client.GetAsync("/api/Admin/OAuth");
        getResponse.EnsureSuccessStatusCode();
        var retrievedProviders = await getResponse.Content.ReadFromJsonAsync<Dictionary<string, OAuthProviderConfig>>();
        
        Assert.NotNull(retrievedProviders);
        Assert.True(retrievedProviders.ContainsKey("testprovider"));
        Assert.Equal("Test Provider", retrievedProviders["testprovider"].DisplayName);
        Assert.Equal(3, retrievedProviders["testprovider"].FieldMapping.Count);
    }

    [Fact]
    public async Task User_GetOAuthProviders_ReturnsEnabledProviders()
    {
        // Arrange
        var (admin, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.Admin);
        using var adminClient = factory.CreateAuthenticatedClient(admin);

        // Create OAuth providers
        var providers = new Dictionary<string, OAuthProviderConfig>
        {
            ["enabled"] = new()
            {
                Enabled = true,
                ClientId = "test",
                ClientSecret = "secret",
                AuthorizationEndpoint = "https://test.com/auth",
                TokenEndpoint = "https://test.com/token",
                UserInformationEndpoint = "https://test.com/user",
                DisplayName = "Enabled Provider",
                Scopes = ["email"]
            },
            ["disabled"] = new()
            {
                Enabled = false,
                ClientId = "test2",
                ClientSecret = "secret2",
                AuthorizationEndpoint = "https://test2.com/auth",
                TokenEndpoint = "https://test2.com/token",
                UserInformationEndpoint = "https://test2.com/user",
                DisplayName = "Disabled Provider",
                Scopes = ["email"]
            }
        };
        await adminClient.PutAsJsonAsync("/api/Admin/OAuth", providers);

        // Act - get as unauthenticated user
        using var publicClient = factory.CreateClient();
        var response = await publicClient.GetAsync("/api/Account/OAuth/Providers");

        // Debug output
        output.WriteLine($"Response status: {response.StatusCode}");
        output.WriteLine($"Response content type: {response.Content.Headers.ContentType}");
        var content = await response.Content.ReadAsStringAsync();
        output.WriteLine($"Response content (first 500 chars): {content[..Math.Min(500, content.Length)]}");

        // Assert
        response.EnsureSuccessStatusCode();
        var availableProviders = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        
        Assert.NotNull(availableProviders);
        Assert.Contains("enabled", availableProviders.Keys);
        Assert.DoesNotContain("disabled", availableProviders.Keys);
    }

    [Fact]
    public async Task OAuth_LoginInitiation_ReturnsAuthorizationUrl()
    {
        // Arrange
        var (admin, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.Admin);
        using var adminClient = factory.CreateAuthenticatedClient(admin);

        // Create OAuth provider
        var providers = new Dictionary<string, OAuthProviderConfig>
        {
            ["github"] = new()
            {
                Enabled = true,
                ClientId = "github-client-id",
                ClientSecret = "github-client-secret",
                AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
                TokenEndpoint = "https://github.com/login/oauth/access_token",
                UserInformationEndpoint = "https://api.github.com/user",
                DisplayName = "GitHub",
                Scopes = ["user:email"],
                FieldMapping = new Dictionary<string, string>()
            }
        };
        await adminClient.PutAsJsonAsync("/api/Admin/OAuth", providers);

        // Act
        using var publicClient = factory.CreateClient();
        var response = await publicClient.GetAsync("/api/Account/OAuth/Login/github");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RequestResponse<string>>();
        
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Contains("github.com/login/oauth/authorize", result.Data);
        Assert.Contains("client_id=github-client-id", result.Data);
        Assert.Contains("state=", result.Data);
        output.WriteLine($"Authorization URL: {result.Data}");
    }

    [Fact]
    public async Task OAuth_LoginWithDisabledProvider_ReturnsBadRequest()
    {
        // Arrange
        var (admin, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.Admin);
        using var adminClient = factory.CreateAuthenticatedClient(admin);

        // Create disabled OAuth provider
        var providers = new Dictionary<string, OAuthProviderConfig>
        {
            ["disabled"] = new()
            {
                Enabled = false,
                ClientId = "test",
                ClientSecret = "secret",
                AuthorizationEndpoint = "https://test.com/auth",
                TokenEndpoint = "https://test.com/token",
                UserInformationEndpoint = "https://test.com/user",
                Scopes = []
            }
        };
        await adminClient.PutAsJsonAsync("/api/Admin/OAuth", providers);

        // Act
        using var publicClient = factory.CreateClient();
        var response = await publicClient.GetAsync("/api/Account/OAuth/Login/disabled");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OAuthService_GetOrCreateUser_CreatesNewUser()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var oauthService = scope.ServiceProvider.GetRequiredService<IOAuthService>();

        var oauthUser = new OAuthUserInfo
        {
            ProviderId = "testprovider",
            ProviderUserId = "12345",
            Email = $"oauth-{Guid.NewGuid():N}@example.com",
            UserName = "oauthuser",
            MappedFields = new Dictionary<string, string>
            {
                { "department", "Engineering" },
                { "role", "Developer" }
            }
        };

        // Act
        var (user, isNewUser) = await oauthService.GetOrCreateUserFromOAuthAsync("testprovider", oauthUser);

        // Assert
        Assert.True(isNewUser);
        Assert.Equal(oauthUser.Email, user.Email);
        Assert.True(user.EmailConfirmed); // OAuth users have confirmed emails
        Assert.Equal("Engineering", user.UserMetadata["department"]);
        Assert.Equal("Developer", user.UserMetadata["role"]);
        output.WriteLine($"Created user: {user.UserName} ({user.Email})");
    }

    [Fact]
    public async Task OAuthService_GetOrCreateUser_UpdatesExistingUser()
    {
        // Arrange
        var email = $"existing-{Guid.NewGuid():N}@example.com";
        var (existingUser, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.User);
        
        // Update user email to match OAuth email
        using var scope1 = factory.Services.CreateScope();
        var userManager = scope1.ServiceProvider.GetRequiredService<UserManager<GZCTF.Models.Data.UserInfo>>();
        var user = await userManager.FindByIdAsync(existingUser.Id.ToString());
        Assert.NotNull(user);
        user.Email = email;
        await userManager.UpdateAsync(user);

        // Create OAuth user with same email
        using var scope2 = factory.Services.CreateScope();
        var oauthService = scope2.ServiceProvider.GetRequiredService<IOAuthService>();
        
        var oauthUser = new OAuthUserInfo
        {
            ProviderId = "testprovider",
            ProviderUserId = "67890",
            Email = email,
            UserName = "oauthuser2",
            MappedFields = new Dictionary<string, string>
            {
                { "company", "TestCorp" },
                { "location", "Remote" }
            }
        };

        // Act
        var (updatedUser, isNewUser) = await oauthService.GetOrCreateUserFromOAuthAsync("testprovider", oauthUser);

        // Assert
        Assert.False(isNewUser);
        Assert.Equal(existingUser.Id, updatedUser.Id);
        Assert.Equal(email, updatedUser.Email);
        Assert.Equal("TestCorp", updatedUser.UserMetadata["company"]);
        Assert.Equal("Remote", updatedUser.UserMetadata["location"]);
        output.WriteLine($"Updated existing user: {updatedUser.UserName}");
    }

    [Fact]
    public async Task OAuthService_HandlesUsernameConflicts()
    {
        // Arrange
        // Create user with specific username
        var userName = $"testuser_{Guid.NewGuid():N}";
        await TestDataSeeder.CreateUserAsync(factory.Services, userName, "Password123!", $"{userName}@test.com");

        // Try to create OAuth user with same username
        using var scope = factory.Services.CreateScope();
        var oauthService = scope.ServiceProvider.GetRequiredService<IOAuthService>();
        
        var oauthUser = new OAuthUserInfo
        {
            ProviderId = "testprovider",
            ProviderUserId = "99999",
            Email = $"different-{Guid.NewGuid():N}@example.com",
            UserName = userName, // Same username as existing user
            MappedFields = new Dictionary<string, string>()
        };

        // Act
        var (user, isNewUser) = await oauthService.GetOrCreateUserFromOAuthAsync("testprovider", oauthUser);

        // Assert
        Assert.True(isNewUser);
        Assert.NotEqual(userName, user.UserName); // Should have different username
        Assert.StartsWith(userName, user.UserName); // Should start with original username
        output.WriteLine($"Resolved username conflict: {userName} -> {user.UserName}");
    }

    [Fact]
    public async Task OAuth_FieldMapping_AppliesCorrectly()
    {
        // Arrange
        var (admin, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.Admin);
        using var adminClient = factory.CreateAuthenticatedClient(admin);

        // Create metadata fields
        var fields = new List<UserMetadataField>
        {
            new()
            {
                Key = "githubUsername",
                DisplayName = "GitHub Username",
                Type = UserMetadataFieldType.Text,
                Required = false,
                Visible = true
            },
            new()
            {
                Key = "fullName",
                DisplayName = "Full Name",
                Type = UserMetadataFieldType.Text,
                Required = false,
                Visible = true
            }
        };
        await adminClient.PutAsJsonAsync("/api/Admin/UserMetadata", fields);

        // Create OAuth provider with field mapping
        var providers = new Dictionary<string, OAuthProviderConfig>
        {
            ["github"] = new()
            {
                Enabled = true,
                ClientId = "test",
                ClientSecret = "secret",
                AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
                TokenEndpoint = "https://github.com/login/oauth/access_token",
                UserInformationEndpoint = "https://api.github.com/user",
                Scopes = ["user:email"],
                FieldMapping = new Dictionary<string, string>
                {
                    { "login", "githubUsername" },
                    { "name", "fullName" }
                }
            }
        };
        await adminClient.PutAsJsonAsync("/api/Admin/OAuth", providers);

        // Create OAuth user
        using var scope = factory.Services.CreateScope();
        var oauthService = scope.ServiceProvider.GetRequiredService<IOAuthService>();
        
        var oauthUser = new OAuthUserInfo
        {
            ProviderId = "github",
            ProviderUserId = "111",
            Email = $"gh-{Guid.NewGuid():N}@example.com",
            UserName = "octocat",
            MappedFields = new Dictionary<string, string>
            {
                { "githubUsername", "octocat" },
                { "fullName", "The Octocat" }
            }
        };

        // Act
        var (user, _) = await oauthService.GetOrCreateUserFromOAuthAsync("github", oauthUser);

        // Assert
        Assert.Equal("octocat", user.UserMetadata["githubUsername"]);
        Assert.Equal("The Octocat", user.UserMetadata["fullName"]);
        output.WriteLine($"User metadata: {JsonSerializer.Serialize(user.UserMetadata)}");
    }
}
