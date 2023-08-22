using GZCTF.Models.Request.Admin;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Repositories;

public class ContainerRepository : RepositoryBase, IContainerRepository
{
    private readonly IDistributedCache _cache;

    public ContainerRepository(IDistributedCache cache,
        AppDbContext context) : base(context)
    {
        _cache = cache;
    }

    public override Task<int> CountAsync(CancellationToken token = default) => _context.Containers.CountAsync(token);

    public Task<Container?> GetContainerById(string guid, CancellationToken token = default)
        => _context.Containers.FirstOrDefaultAsync(i => i.Id == guid, token);

    public Task<Container?> GetContainerWithInstanceById(string guid, CancellationToken token = default)
        => _context.Containers.IgnoreAutoIncludes()
            .Include(c => c.Instance).ThenInclude(i => i!.Challenge)
            .Include(c => c.Instance).ThenInclude(i => i!.FlagContext)
            .Include(c => c.Instance).ThenInclude(i => i!.Participation).ThenInclude(p => p.Team)
            .FirstOrDefaultAsync(i => i.Id == guid, token);

    public Task<List<Container>> GetContainers(CancellationToken token = default)
        => _context.Containers.ToListAsync(token);

    public async Task<ContainerInstanceModel[]> GetContainerInstances(CancellationToken token = default)
        => (await _context.Containers
                .Where(c => c.Instance != null)
                .Include(c => c.Instance).ThenInclude(i => i!.Participation)
                .OrderBy(c => c.StartedAt).ToArrayAsync(token))
            .Select(ContainerInstanceModel.FromContainer)
            .ToArray();

    public Task<List<Container>> GetDyingContainers(CancellationToken token = default)
    {
        var now = DateTimeOffset.UtcNow;
        return _context.Containers.Where(c => c.ExpectStopAt < now).ToListAsync(token);
    }

    public async Task RemoveContainer(Container container, CancellationToken token = default)
    {
        // Do not remove running container from database
        if (container.Status != ContainerStatus.Destroyed)
            return;

        await _cache.RemoveAsync(CacheKey.ConnectionCount(container.Id), token);

        _context.Containers.Remove(container);
        await SaveAsync(token);
    }

    public async Task<bool> ValidateContainer(string guid, CancellationToken token = default)
        => await _context.Containers.AnyAsync(c => c.Id == guid, token);
}
