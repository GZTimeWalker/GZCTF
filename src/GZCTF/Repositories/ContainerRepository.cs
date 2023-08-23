using GZCTF.Models.Request.Admin;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Repositories;

public class ContainerRepository(IDistributedCache cache,
    AppDbContext context) : RepositoryBase(context), IContainerRepository
{
    public override Task<int> CountAsync(CancellationToken token = default) => context.Containers.CountAsync(token);

    public Task<Container?> GetContainerById(string guid, CancellationToken token = default)
        => context.Containers.FirstOrDefaultAsync(i => i.Id == guid, token);

    public Task<Container?> GetContainerWithInstanceById(string guid, CancellationToken token = default)
        => context.Containers.IgnoreAutoIncludes()
            .Include(c => c.Instance).ThenInclude(i => i!.Challenge)
            .Include(c => c.Instance).ThenInclude(i => i!.FlagContext)
            .Include(c => c.Instance).ThenInclude(i => i!.Participation).ThenInclude(p => p.Team)
            .FirstOrDefaultAsync(i => i.Id == guid, token);

    public Task<List<Container>> GetContainers(CancellationToken token = default)
        => context.Containers.ToListAsync(token);

    public async Task<ContainerInstanceModel[]> GetContainerInstances(CancellationToken token = default)
        => (await context.Containers
                .Where(c => c.Instance != null)
                .Include(c => c.Instance).ThenInclude(i => i!.Participation)
                .OrderBy(c => c.StartedAt).ToArrayAsync(token))
            .Select(ContainerInstanceModel.FromContainer)
            .ToArray();

    public Task<List<Container>> GetDyingContainers(CancellationToken token = default)
    {
        var now = DateTimeOffset.UtcNow;
        return context.Containers.Where(c => c.ExpectStopAt < now).ToListAsync(token);
    }

    public async Task RemoveContainer(Container container, CancellationToken token = default)
    {
        // Do not remove running container from database
        if (container.Status != ContainerStatus.Destroyed)
            return;

        await cache.RemoveAsync(CacheKey.ConnectionCount(container.Id), token);

        context.Containers.Remove(container);
        await SaveAsync(token);
    }

    public async Task<bool> ValidateContainer(string guid, CancellationToken token = default)
        => await context.Containers.AnyAsync(c => c.Id == guid, token);
}
