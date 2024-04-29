using Microsoft.EntityFrameworkCore.Storage;

namespace GZCTF.Repositories.Interface;

public interface IRepository
{
    public string ChangeTrackerView { get; }

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default);

    public void Add(object item);

    public Task<int> CountAsync(CancellationToken token = default);

    public Task SaveAsync(CancellationToken token = default);
}
