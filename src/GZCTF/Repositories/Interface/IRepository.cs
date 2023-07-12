using Microsoft.EntityFrameworkCore.Storage;

namespace GZCTF.Repositories.Interface;

public interface IRepository
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default);

    public string ChangeTrackerView { get; }

    public void Detach(object item);

    public void Add(object item);

    public Task<int> CountAsync(CancellationToken token = default);

    public Task SaveAsync(CancellationToken token = default);
}
