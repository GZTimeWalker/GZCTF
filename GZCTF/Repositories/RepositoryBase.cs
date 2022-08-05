using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class RepositoryBase : IRepository
{
    protected readonly AppDbContext context;

    public RepositoryBase(AppDbContext _context)
        => context = _context;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default)
        => context.Database.BeginTransactionAsync(token);

    public string ChangeTrackerView => context.ChangeTracker.DebugView.LongView;

    public Task UpdateAsync(object item, CancellationToken token = default)
    {
        context.Update(item);
        return context.SaveChangesAsync(token);
    }

    public void Detach(object item)
        => context.Entry(item).State = EntityState.Detached;
}