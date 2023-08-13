using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GZCTF.Repositories;

public class RepositoryBase : IRepository
{
    protected readonly AppDbContext _context;

    public RepositoryBase(AppDbContext context)
        => _context = context;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default)
        => _context.Database.BeginTransactionAsync(token);

    public string ChangeTrackerView => _context.ChangeTracker.DebugView.LongView;

    public async Task SaveAsync(CancellationToken token = default)
    {
        bool saved = false;
        while (!saved)
        {
            try
            {
                await _context.SaveChangesAsync(token);
                saved = true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // FIXME: detect change
                foreach (var entry in ex.Entries)
                    entry.Reload();
            }
            catch
            {
                throw;
            }
        }
    }

    public void Detach(object item) => _context.Entry(item).State = EntityState.Detached;

    public void Add(object item) => _context.Add(item);

    public virtual Task<int> CountAsync(CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
