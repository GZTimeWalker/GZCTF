using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

/// <summary>
/// Repository for managing API tokens.
/// </summary>
public class ApiTokenRepository(AppDbContext context) : RepositoryBase(context), IApiTokenRepository
{
    public async Task<ApiToken> AddTokenAsync(ApiToken token, CancellationToken cancellationToken = default)
    {
        await Context.ApiTokens.AddAsync(token, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public Task<List<ApiToken>> GetTokensAsync(CancellationToken cancellationToken = default) =>
        Context.ApiTokens.AsNoTracking()
            .Include(t => t.Creator)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> RecordTokenUsageAsync(Guid id, CancellationToken cancellationToken = default) =>
        Context.ApiTokens
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(t => t.LastUsedAt, _ => DateTimeOffset.UtcNow),
                cancellationToken)
            .ContinueWith(task => task.Result > 0, cancellationToken);

    public Task<ApiToken?> GetTokenByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Context.ApiTokens.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<bool> RevokeTokenAsync(ApiToken token, CancellationToken cancellationToken = default)
    {
        if (token.IsRevoked)
            return false;

        token.IsRevoked = true;
        Context.ApiTokens.Update(token);
        return await Context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> RestoreTokenAsync(ApiToken token, CancellationToken cancellationToken = default)
    {
        if (!token.IsRevoked)
            return false;

        token.IsRevoked = false;
        Context.ApiTokens.Update(token);
        return await Context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteTokenAsync(ApiToken token, CancellationToken cancellationToken = default)
    {
        Context.ApiTokens.Remove(token);
        return await Context.SaveChangesAsync(cancellationToken) > 0;
    }
}
