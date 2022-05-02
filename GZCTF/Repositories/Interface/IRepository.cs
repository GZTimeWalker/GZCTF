using Microsoft.EntityFrameworkCore.Storage;

namespace CTFServer.Repositories.Interface;

public interface IRepository
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default);

    public Task UpdateAsync(object item, CancellationToken token = default);
}
