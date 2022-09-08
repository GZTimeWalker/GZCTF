using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CTFServer.Repositories;

public class RepositoryBase : IRepository
{
    protected readonly AppDbContext context;

    public RepositoryBase(AppDbContext _context)
        => context = _context;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default)
        => context.Database.BeginTransactionAsync(token);

    public string ChangeTrackerView => context.ChangeTracker.DebugView.LongView;

    public async Task SaveAsync(CancellationToken token = default)
    {
        bool saved = false;
        while (!saved)
        {
            try
            {
                await context.SaveChangesAsync(token);
                saved = true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // TODO: detect change

                foreach (var entry in ex.Entries)
                    entry.Reload();
            }
            catch
            {
                throw;
            }
        }
    }

    public void Detach(object item) => context.Entry(item).State = EntityState.Detached;

    public void Add(object item) => context.Add(item);
}