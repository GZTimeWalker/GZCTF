using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace GZCTF.Repositories;

public abstract class RepositoryBase(AppDbContext context) : IRepository
{
    protected readonly AppDbContext context = context;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default) => context.Database.BeginTransactionAsync(token);

    public string ChangeTrackerView => context.ChangeTracker.DebugView.LongView;

    public async Task SaveAsync(CancellationToken token = default)
    {
        var saved = false;
        while (!saved)
        {
            try
            {
                await context.SaveChangesAsync(token);
                saved = true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // FIXME: detect change
                foreach (EntityEntry entry in ex.Entries)
                    await entry.ReloadAsync(token);
            }
        }
    }

    public void Detach(object item) => context.Entry(item).State = EntityState.Detached;

    public void Add(object item) => context.Add(item);

    public virtual Task<int> CountAsync(CancellationToken token = default) => throw new NotImplementedException();
}