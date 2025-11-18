using GZCTF.Models.Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Extensions.Startup;

static class OAuthExtension
{
    public static void ConfigureOAuth(this WebApplicationBuilder builder)
    {
        // OAuth will be dynamically configured from database at runtime
        builder.Services.AddScoped<IOAuthProviderManager, OAuthProviderManager>();
    }
}

public interface IOAuthProviderManager
{
    Task<List<UserMetadataField>> GetUserMetadataFieldsAsync(CancellationToken token = default);
    Task UpdateUserMetadataFieldsAsync(List<UserMetadataField> fields, CancellationToken token = default);
    Task<Dictionary<string, OAuthProviderConfig>> GetOAuthProvidersAsync(CancellationToken token = default);
    Task<OAuthProviderConfig?> GetOAuthProviderAsync(string key, CancellationToken token = default);
    Task UpdateOAuthProviderAsync(string key, OAuthProviderConfig config, CancellationToken token = default);
    Task DeleteOAuthProviderAsync(string key, CancellationToken token = default);
    Task<Dictionary<string, AuthenticationScheme>> GetAvailableProvidersAsync(CancellationToken token = default);
}

public class OAuthProviderManager(
    AppDbContext context,
    IAuthenticationSchemeProvider schemeProvider,
    ILogger<OAuthProviderManager> logger) : IOAuthProviderManager
{
    public async Task<List<UserMetadataField>> GetUserMetadataFieldsAsync(CancellationToken token = default)
    {
        var fields = await context.UserMetadataFields
            .OrderBy(f => f.Order)
            .ToListAsync(token);
        
        return fields.Select(f => f.ToField()).ToList();
    }

    public async Task UpdateUserMetadataFieldsAsync(List<UserMetadataField> fields, CancellationToken token = default)
    {
        // Remove all existing fields
        var existingFields = await context.UserMetadataFields.ToListAsync(token);
        context.UserMetadataFields.RemoveRange(existingFields);

        // Add new fields
        for (int i = 0; i < fields.Count; i++)
        {
            var field = new UserMetadataFieldConfig { Order = i };
            field.UpdateFromField(fields[i]);
            await context.UserMetadataFields.AddAsync(field, token);
        }

        await context.SaveChangesAsync(token);
        logger.SystemLog("User metadata fields configuration updated", TaskStatus.Success, LogLevel.Information);
    }

    public async Task<Dictionary<string, OAuthProviderConfig>> GetOAuthProvidersAsync(CancellationToken token = default)
    {
        var providers = await context.OAuthProviders.ToListAsync(token);
        return providers.ToDictionary(p => p.Key, p => p.ToConfig());
    }

    public async Task<OAuthProviderConfig?> GetOAuthProviderAsync(string key, CancellationToken token = default)
    {
        var provider = await context.OAuthProviders
            .FirstOrDefaultAsync(p => p.Key == key, token);
        return provider?.ToConfig();
    }

    public async Task UpdateOAuthProviderAsync(string key, OAuthProviderConfig config, CancellationToken token = default)
    {
        var provider = await context.OAuthProviders
            .FirstOrDefaultAsync(p => p.Key == key, token);
        
        if (provider is null)
        {
            provider = new OAuthProvider { Key = key };
            provider.UpdateFromConfig(config);
            await context.OAuthProviders.AddAsync(provider, token);
        }
        else
        {
            provider.UpdateFromConfig(config);
        }

        await context.SaveChangesAsync(token);
        logger.SystemLog($"OAuth provider '{key}' configuration updated", TaskStatus.Success, LogLevel.Information);
    }

    public async Task DeleteOAuthProviderAsync(string key, CancellationToken token = default)
    {
        var provider = await context.OAuthProviders
            .FirstOrDefaultAsync(p => p.Key == key, token);
        
        if (provider is not null)
        {
            context.OAuthProviders.Remove(provider);
            await context.SaveChangesAsync(token);
            logger.SystemLog($"OAuth provider '{key}' deleted", TaskStatus.Success, LogLevel.Information);
        }
    }

    public async Task<Dictionary<string, AuthenticationScheme>> GetAvailableProvidersAsync(CancellationToken token = default)
    {
        var providers = await context.OAuthProviders
            .Where(p => p.Enabled)
            .ToListAsync(token);

        var schemes = await schemeProvider.GetAllSchemesAsync();
        var available = new Dictionary<string, AuthenticationScheme>();

        foreach (var provider in providers)
        {
            var scheme = schemes.FirstOrDefault(s => 
                s.Name.Equals(provider.Key, StringComparison.OrdinalIgnoreCase) ||
                s.DisplayName?.Equals(provider.Key, StringComparison.OrdinalIgnoreCase) == true);

            if (scheme is not null)
                available[provider.Key] = scheme;
        }

        return available;
    }
}
