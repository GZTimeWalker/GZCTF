using System.ComponentModel.DataAnnotations;
using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using DataUserMetadataField = GZCTF.Models.Data.UserMetadataField;
using InternalUserMetadataField = GZCTF.Models.Internal.UserMetadataField;

namespace GZCTF.Repositories;

public class OAuthProviderRepository(AppDbContext context) : RepositoryBase(context), IOAuthProviderRepository
{
    public Task<OAuthProvider?> FindByKeyAsync(string key, CancellationToken token = default) =>
        Context.OAuthProviders.AsNoTracking().FirstOrDefaultAsync(p => p.Key == key, token);

    public Task<List<OAuthProvider>> ListAsync(CancellationToken token = default) =>
        Context.OAuthProviders.AsNoTracking().ToListAsync(token);

    public async Task<Dictionary<string, OAuthProviderConfig>> GetConfigMapAsync(CancellationToken token = default)
    {
        var providers = await Context.OAuthProviders.AsNoTracking().ToListAsync(token);
        return providers.ToDictionary(p => p.Key, p => p.ToConfig());
    }

    public async Task<OAuthProviderConfig?> GetConfigAsync(string key, CancellationToken token = default) =>
        await Context.OAuthProviders.AsNoTracking()
            .Where(p => p.Key == key)
            .Select(p => p.ToConfig())
            .FirstOrDefaultAsync(token);

    public async Task UpsertAsync(string key, OAuthProviderConfig config, CancellationToken token = default)
    {
        OAuthProvider.ValidateKey(key);

        var provider = await Context.OAuthProviders.FirstOrDefaultAsync(p => p.Key == key, token);

        if (provider is null)
        {
            provider = new OAuthProvider { Key = key };
            await Context.OAuthProviders.AddAsync(provider, token);
        }

        provider.UpdateFromConfig(config);

        Validator.ValidateObject(provider, new ValidationContext(provider), validateAllProperties: true);

        await Context.SaveChangesAsync(token);
    }

    public async Task DeleteAsync(string key, CancellationToken token = default)
    {
        var provider = await Context.OAuthProviders.FirstOrDefaultAsync(p => p.Key == key, token);

        if (provider is null)
            return;

        Context.OAuthProviders.Remove(provider);
        await Context.SaveChangesAsync(token);
    }

    public async Task<List<InternalUserMetadataField>> GetMetadataFieldsAsync(CancellationToken token = default)
    {
        var fields = await Context.UserMetadataFields
            .AsNoTracking()
            .OrderBy(f => f.Order)
            .ToListAsync(token);

        return fields.Select(f => f.ToField()).ToList();
    }

    public async Task UpdateMetadataFieldsAsync(List<InternalUserMetadataField> fields,
        CancellationToken token = default)
    {
        var existingFields = await Context.UserMetadataFields.ToListAsync(token);
        Context.UserMetadataFields.RemoveRange(existingFields);

        for (var i = 0; i < fields.Count; i++)
        {
            var entity = new DataUserMetadataField { Order = i };
            entity.UpdateFromField(fields[i]);
            await Context.UserMetadataFields.AddAsync(entity, token);
        }

        await Context.SaveChangesAsync(token);
    }
}
