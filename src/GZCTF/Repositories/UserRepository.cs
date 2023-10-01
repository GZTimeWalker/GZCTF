using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public Task<UserInfo?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
}