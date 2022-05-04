using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore.Storage;

namespace CTFServer.Repositories;

public class RepositoryBase : IRepository
{
    protected readonly AppDbContext context;

    public RepositoryBase(AppDbContext _context)
        => context = _context;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default)
        => context.Database.BeginTransactionAsync(token);

    public Task UpdateAsync(object item, CancellationToken token = default)
    {
        context.Update(item);
        return context.SaveChangesAsync(token);
    }
}