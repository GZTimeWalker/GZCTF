using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class UserMetadataFieldRepository(
    CacheHelper cacheHelper,
    AppDbContext context,
    ILogger<UserMetadataFieldRepository> logger) : RepositoryBase(context), IUserMetadataFieldRepository
{
    public Task<UserMetadataField?> GetByKeyAsync(string key, CancellationToken token = default)
        => Context.UserMetadataFields.FirstOrDefaultAsync(f => f.Key == key, token);

    public Task<UserMetadataField[]> GetAllAsync(CancellationToken token = default) =>
        cacheHelper.GetOrCreateAsync(logger, CacheKey.UserMetadataFields,
            entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(1);
                return Context.UserMetadataFields.OrderBy(f => f.Order).ToArrayAsync(token);
            },
            token: token);

    public async Task CreateAsync(UserMetadataField field, CancellationToken token = default)
    {
        await Context.UserMetadataFields.AddAsync(field, token);
        await SaveAsync(token);
        await ClearCacheAsync(token);
    }

    public async Task UpdateAsync(UserMetadataField field, CancellationToken token = default)
    {
        Context.UserMetadataFields.Update(field);
        await SaveAsync(token);
        await ClearCacheAsync(token);
    }

    public async Task DeleteAsync(string key, CancellationToken token = default)
    {
        var field = await Context.UserMetadataFields.SingleOrDefaultAsync(f => f.Key == key, token);
        if (field == null)
            return;

        Context.UserMetadataFields.Remove(field);
        await SaveAsync(token);
        await ClearCacheAsync(token);
    }

    private Task ClearCacheAsync(CancellationToken token = default) =>
        cacheHelper.RemoveAsync(CacheKey.UserMetadataFields, token);
}
