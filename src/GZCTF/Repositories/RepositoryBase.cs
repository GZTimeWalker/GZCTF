using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace GZCTF.Repositories;

public abstract class RepositoryBase(AppDbContext context) : IRepository
{
    protected readonly AppDbContext Context = context;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default) =>
        Context.Database.BeginTransactionAsync(token);

    public string ChangeTrackerView => Context.ChangeTracker.DebugView.LongView;

    /// <summary>
    /// 调用此方法保存更改，如果发生并发冲突则重试
    /// </summary>
    /// <param name="token"></param>
    public async Task SaveAsync(CancellationToken token = default)
    {
        var saved = false;
        var retry = 0;
        while (!saved && retry++ < 3)
        {
            try
            {
                await Context.SaveChangesAsync(token);
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

    public void Add(object item) => Context.Add(item);

    public virtual Task<int> CountAsync(CancellationToken token = default) => throw new NotImplementedException();
}
