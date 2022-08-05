using Microsoft.EntityFrameworkCore.Storage;

namespace CTFServer.Repositories.Interface;

public interface IRepository
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default);

    public string ChangeTrackerView { get; }

    public void Detach(object item);

    public Task UpdateAsync(object item, CancellationToken token = default);
}