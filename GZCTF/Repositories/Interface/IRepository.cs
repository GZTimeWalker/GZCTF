using Microsoft.EntityFrameworkCore.Storage;

namespace CTFServer.Repositories.Interface;

public interface IRepository
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
