using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Extensions.Startup;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Services;
using Microsoft.AspNetCore.Identity;

namespace GZCTF.Services.OAuth;

public interface IOAuthService
{
    Task<OAuthUserInfo?> ExchangeCodeForUserInfoAsync(OAuthProvider provider, string code, string redirectUri, CancellationToken token = default);
    Task<(UserInfo user, bool isNewUser)> GetOrCreateUserFromOAuthAsync(OAuthProvider provider, OAuthUserInfo oauthUser, CancellationToken token = default);
}

public class OAuthService(
    IOAuthProviderManager providerManager,
    IUserMetadataService metadataService,
    UserManager<UserInfo> userManager,
    IHttpClientFactory httpClientFactory,
    ILogger<OAuthService> logger) : IOAuthService
{
    public async Task<OAuthUserInfo?> ExchangeCodeForUserInfoAsync(
        OAuthProvider provider,
        string code,
        string redirectUri,
        CancellationToken token = default)
    {
        var providerConfig = await providerManager.GetOAuthProviderAsync(provider.Key, token);
        if (providerConfig is null || !providerConfig.Enabled)
        {
            logger.LogWarning("OAuth provider {Provider} not found or not enabled", provider.Key);
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
                ProviderId = provider.Key,
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
            oauthUser.MappedFields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
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
            logger.LogError(ex, "Error during OAuth code exchange for provider {Provider}", provider.Key);
            return null;
        }
    }

    public async Task<(UserInfo user, bool isNewUser)> GetOrCreateUserFromOAuthAsync(
        OAuthProvider provider,
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
            if (existingUser.OAuthProviderId is null)
                throw new OAuthLoginException(OAuthLoginError.EmailInUse,
                    "Email already registered by another method");

            if (existingUser.OAuthProviderId != provider.Id)
                throw new OAuthLoginException(OAuthLoginError.ProviderMismatch,
                    "Email already linked to another OAuth provider");

            var validation = await metadataService.ValidateAsync(
                oauthUser.MappedFields,
                existingUser.UserMetadata,
                allowLockedWrites: true,
                enforceLockedRequirements: true,
                token);

            if (!validation.IsValid)
                throw new OAuthLoginException(OAuthLoginError.MetadataInvalid, validation.Errors.First());

            existingUser.UserMetadata = validation.Values;
            await userManager.UpdateAsync(existingUser);

            logger.LogInformation("User {Email} logged in via OAuth provider {Provider}", oauthUser.Email, provider);
            return (existingUser, false);
        }

        // Create new user
        var userName = oauthUser.UserName ?? oauthUser.Email.Split('@')[0];

        // Truncate username if too long (max 16 characters, leave room for counter)
        const int maxUsernameLength = 16;
        if (userName.Length > maxUsernameLength - 3) // Reserve 3 chars for potential counter (e.g., "123")
        {
            userName = userName[..(maxUsernameLength - 3)];
        }

        // Ensure username is unique
        var baseUserName = userName;
        var counter = 1;
        while (await userManager.FindByNameAsync(userName) is not null)
        {
            var suffix = counter.ToString();
            var maxBaseLength = maxUsernameLength - suffix.Length;
            userName = baseUserName.Length > maxBaseLength
                ? $"{baseUserName[..maxBaseLength]}{suffix}"
                : $"{baseUserName}{suffix}";
            counter++;
        }

        var newMetadata = await metadataService.ValidateAsync(
            oauthUser.MappedFields,
            null,
            allowLockedWrites: true,
            enforceLockedRequirements: true,
            token);

        if (!newMetadata.IsValid)
            throw new OAuthLoginException(OAuthLoginError.MetadataInvalid, newMetadata.Errors.First());

        var newUser = new UserInfo
        {
            UserName = userName,
            Email = oauthUser.Email,
            EmailConfirmed = true, // OAuth providers verify emails
            RegisterTimeUtc = DateTimeOffset.UtcNow,
            OAuthProviderId = provider.Id,
            UserMetadata = newMetadata.Values
        };

        var result = await userManager.CreateAsync(newUser);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user from OAuth: {errors}");
        }

        logger.LogInformation("Created new user {Email} from OAuth provider {Provider}", oauthUser.Email, provider.Key);
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
    /// <summary>
    /// Provider key (matching <see cref="OAuthProvider.Key"/>) the user originates from.
    /// </summary>
    public required string ProviderId { get; set; }

    /// <summary>
    /// Identifier issued by the upstream provider for this user, if provided.
    /// </summary>
    public string? ProviderUserId { get; set; }

    /// <summary>
    /// Email reported by the provider; required for linking/creating accounts.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Preferred username or handle supplied by the provider.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Normalized field mapping results keyed by target metadata field names.
    /// </summary>
    public Dictionary<string, string?> MappedFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Raw JSON response from the provider for downstream auditing or debugging.
    /// </summary>
    public JsonElement RawData { get; set; }
}
