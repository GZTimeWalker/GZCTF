using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class ContainerRepository : RepositoryBase, IContainerRepository
{
    public ContainerRepository(AppDbContext _context) : base(_context)
    {
    }

    public Task<List<Container>> GetContainers(CancellationToken token = default)
        => context.Containers.ToListAsync(token);

    public Task<List<Container>> GetDyingContainers(CancellationToken token = default)
    {
        var now = DateTimeOffset.UtcNow;
        return context.Containers.Where(c => c.ExpectStopAt < now).ToListAsync(token);
    }

    public Task RemoveContainer(Container container, CancellationToken token = default)
    {
        // Do not remove running container from database
        if (container.Status != ContainerStatus.Destroyed)
            return Task.FromResult(-1);

        context.Containers.Remove(container);
        return SaveAsync(token);
    }
}