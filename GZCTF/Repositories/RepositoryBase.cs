using CTFServer.Models;

namespace CTFServer.Repositories;

public class RepositoryBase
{
    protected readonly AppDbContext context;

    public RepositoryBase(AppDbContext _context)
        => context = _context;
}
