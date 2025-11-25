using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class OAuthProviderRepository(AppDbContext context) : RepositoryBase(context), IOAuthProviderRepository
{
    public Task<OAuthProvider?> FindByKeyAsync(string key, CancellationToken token = default) =>
        Context.OAuthProviders.AsNoTracking().FirstOrDefaultAsync(p => p.Key == key, token);

    public Task<OAuthProvider?> FindByIdAsync(int id, CancellationToken token = default) =>
        Context.OAuthProviders.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, token);

    public Task<List<OAuthProvider>> ListAsync(CancellationToken token = default) =>
        Context.OAuthProviders.AsNoTracking().ToListAsync(token);
}
