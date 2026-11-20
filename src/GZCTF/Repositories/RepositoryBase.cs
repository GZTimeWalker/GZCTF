using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.EntityFrameworkCore.Storage;

namespace GZCTF.Repositories;

public abstract class RepositoryBase(AppDbContext context) : IRepository
{
    protected readonly AppDbContext Context = context;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default) =>
        Context.Database.BeginTransactionAsync(token);

    public string ChangeTrackerView => Context.ChangeTracker.DebugView.LongView;

    /// <summary>
    /// Save changes, retrying optimistic concurrency conflicts and transient
    /// PostgreSQL conflicts (serialization failures, deadlocks). The exception
    /// is rethrown once the retries are exhausted, so a failed save is never
    /// silently swallowed.
    /// </summary>
    /// <param name="token">Cancellation token</param>
    public async Task SaveAsync(CancellationToken token = default)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await Context.SaveChangesAsync(token);
                return;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < maxAttempts)
            {
                if (IsTransientConflict(ex))
                {
                    // Nothing to reload: the provider aborted the transaction.
                    await Task.Delay(TimeSpan.FromMilliseconds(20 * attempt), token);
                }
                else
                {
                    foreach (var entry in ex.Entries)
                        await entry.ReloadAsync(token);
                }
            }
        }
    }

    private static bool IsTransientConflict(DbUpdateConcurrencyException ex) =>
        ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected
        };

    public void Add(object item) => Context.Add(item);

    public virtual Task<int> CountAsync(CancellationToken token = default) => throw new NotImplementedException();
}
