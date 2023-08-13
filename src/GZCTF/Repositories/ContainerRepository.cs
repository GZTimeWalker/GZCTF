using GZCTF.Models.Request.Admin;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ContainerRepository : RepositoryBase, IContainerRepository
{
    public ContainerRepository(AppDbContext context) : base(context)
    {
    }

    public override Task<int> CountAsync(CancellationToken token = default) => _context.Containers.CountAsync(token);

    public Task<Container?> GetContainerById(string guid, CancellationToken token = default)
        => _context.Containers.FirstOrDefaultAsync(i => i.Id == guid, token);

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

    public Task RemoveContainer(Container container, CancellationToken token = default)
    {
        // Do not remove running container from database
        if (container.Status != ContainerStatus.Destroyed)
            return Task.FromResult(-1);

        _context.Containers.Remove(container);
        return SaveAsync(token);
    }
}
