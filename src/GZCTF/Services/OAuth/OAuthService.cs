using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Extensions.Startup;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Services.OAuth;

public interface IOAuthService
{
    Task<OAuthUserInfo?> ExchangeCodeForUserInfoAsync(string provider, string code, string redirectUri, CancellationToken token = default);
    Task<(UserInfo user, bool isNewUser)> GetOrCreateUserFromOAuthAsync(string provider, OAuthUserInfo oauthUser, CancellationToken token = default);
}

public class OAuthService(
    IOAuthProviderManager providerManager,
    UserManager<UserInfo> userManager,
    IHttpClientFactory httpClientFactory,
    ILogger<OAuthService> logger) : IOAuthService
{
    public async Task<OAuthUserInfo?> ExchangeCodeForUserInfoAsync(
        string provider,
        string code,
        string redirectUri,
        CancellationToken token = default)
    {
        var providerConfig = await providerManager.GetOAuthProviderAsync(provider, token);
        if (providerConfig is null || !providerConfig.Enabled)
        {
            logger.LogWarning("OAuth provider {Provider} not found or not enabled", provider);
            return null;
        }

        try
        {
            // Exchange code for access token
            using var httpClient = httpClientFactory.CreateClient();
            
            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", providerConfig.ClientId },
                { "client_secret", providerConfig.ClientSecret }
            };

            var tokenResponse = await httpClient.PostAsync(
                providerConfig.TokenEndpoint,
                new FormUrlEncodedContent(tokenRequest),
                token);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync(token);
                logger.LogWarning("Failed to exchange OAuth code for token: {StatusCode} - {Content}",
                    tokenResponse.StatusCode, errorContent);
                return null;
            }

            var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
            var accessToken = tokenData.GetProperty("access_token").GetString();

            if (string.IsNullOrEmpty(accessToken))
            {
                logger.LogWarning("No access token received from OAuth provider {Provider}", provider);
                return null;
            }

            // Get user info from provider
            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, providerConfig.UserInformationEndpoint);
            userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userInfoResponse = await httpClient.SendAsync(userInfoRequest, token);
            
            if (!userInfoResponse.IsSuccessStatusCode)
            {
                var errorContent = await userInfoResponse.Content.ReadAsStringAsync(token);
                logger.LogWarning("Failed to get user info from OAuth provider: {StatusCode} - {Content}",
                    userInfoResponse.StatusCode, errorContent);
                return null;
            }

            var userInfoData = await userInfoResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);

            // Map fields based on provider configuration
            var oauthUser = new OAuthUserInfo
            {
                ProviderId = provider,
                ProviderUserId = userInfoData.TryGetProperty("id", out var id) 
                    ? id.ToString() 
                    : userInfoData.TryGetProperty("sub", out var sub) 
                        ? sub.ToString() 
                        : null,
                Email = GetFieldValue(userInfoData, "email"),
                UserName = GetFieldValue(userInfoData, "login") 
                          ?? GetFieldValue(userInfoData, "username") 
                          ?? GetFieldValue(userInfoData, "preferred_username"),
                RawData = userInfoData
            };

            // Apply field mapping
            oauthUser.MappedFields = new Dictionary<string, string>();
            foreach (var (sourceField, targetField) in providerConfig.FieldMapping)
            {
                var value = GetFieldValue(userInfoData, sourceField);
                if (!string.IsNullOrEmpty(value))
                {
                    oauthUser.MappedFields[targetField] = value;
                }
            }

            return oauthUser;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during OAuth code exchange for provider {Provider}", provider);
            return null;
        }
    }

    public async Task<(UserInfo user, bool isNewUser)> GetOrCreateUserFromOAuthAsync(
        string provider,
        OAuthUserInfo oauthUser,
        CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(oauthUser.Email))
        {
            throw new InvalidOperationException("OAuth user must have an email address");
        }

        // Try to find existing user by email
        var existingUser = await userManager.FindByEmailAsync(oauthUser.Email);
        
        if (existingUser is not null)
        {
            // Update user metadata from OAuth if configured
            if (oauthUser.MappedFields.Count > 0)
            {
                foreach (var (key, value) in oauthUser.MappedFields)
                {
                    existingUser.UserMetadata[key] = value;
                }
                await userManager.UpdateAsync(existingUser);
            }

            logger.LogInformation("User {Email} logged in via OAuth provider {Provider}", oauthUser.Email, provider);
            return (existingUser, false);
        }

        // Create new user
        var userName = oauthUser.UserName ?? oauthUser.Email.Split('@')[0];
        
        // Ensure username is unique
        var baseUserName = userName;
        var counter = 1;
        while (await userManager.FindByNameAsync(userName) is not null)
        {
            userName = $"{baseUserName}{counter}";
            counter++;
        }

        var newUser = new UserInfo
        {
            UserName = userName,
            Email = oauthUser.Email,
            EmailConfirmed = true, // OAuth providers verify emails
            RegisterTimeUtc = DateTimeOffset.UtcNow,
            UserMetadata = new Dictionary<string, string>()
        };

        // Apply mapped fields
        if (oauthUser.MappedFields.Count > 0)
        {
            foreach (var (key, value) in oauthUser.MappedFields)
            {
                newUser.UserMetadata[key] = value;
            }
        }

        var result = await userManager.CreateAsync(newUser);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user from OAuth: {errors}");
        }

        logger.LogInformation("Created new user {Email} from OAuth provider {Provider}", oauthUser.Email, provider);
        return (newUser, true);
    }

    private static string? GetFieldValue(JsonElement data, string fieldName)
    {
        if (data.TryGetProperty(fieldName, out var value))
        {
            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }
        return null;
    }
}

public class OAuthUserInfo
{
    public required string ProviderId { get; set; }
    public string? ProviderUserId { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public Dictionary<string, string> MappedFields { get; set; } = new();
    public JsonElement RawData { get; set; }
}
