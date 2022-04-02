using CTFServer.Models;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore.Storage;

namespace CTFServer.Repositories;

public class RepositoryBase : IRepository
{
    protected readonly AppDbContext context;

    public RepositoryBase(AppDbContext _context)
        => context = _context;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return context.Database.BeginTransactionAsync(cancellationToken);
    }
}
