using GZCTF.Models.Request.Admin;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Repositories;

public class ContainerRepository(
    IDistributedCache cache,
    IContainerManager service,
    ILogger<ContainerRepository> logger,
    AppDbContext context) : RepositoryBase(context), IContainerRepository
{
    public override Task<int> CountAsync(CancellationToken token = default) => Context.Containers.CountAsync(token);

    public Task<Container?> GetContainerById(Guid guid, CancellationToken token = default) =>
        Context.Containers.FirstOrDefaultAsync(i => i.Id == guid, token);

    public Task<Container?> GetContainerWithInstanceById(Guid guid, CancellationToken token = default) =>
        Context.Containers.IgnoreAutoIncludes()
            .Include(c => c.GameInstance).ThenInclude(i => i!.Challenge)
            .Include(c => c.GameInstance).ThenInclude(i => i!.FlagContext)
            .Include(c => c.GameInstance).ThenInclude(i => i!.Participation).ThenInclude(p => p.Team)
            .FirstOrDefaultAsync(i => i.Id == guid, token);

    public async Task<ContainerInstanceModel[]> GetContainerInstances(CancellationToken token = default) =>
        (await Context.Containers
            .Where(c => c.GameInstance != null)
            .Include(c => c.GameInstance).ThenInclude(i => i!.Participation)
            .OrderBy(c => c.StartedAt).ToArrayAsync(token))
        .Select(ContainerInstanceModel.FromContainer)
        .ToArray();

    public Task<List<Container>> GetDyingContainers(CancellationToken token = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return Context.Containers.Where(c => c.ExpectStopAt < now).ToListAsync(token);
    }

    public Task ExtendLifetime(Container container, TimeSpan time, CancellationToken token = default)
    {
        container.ExpectStopAt += time;
        return SaveAsync(token);
    }

    public async Task<bool> ValidateContainer(Guid guid, CancellationToken token = default) =>
        await Context.Containers.AnyAsync(c => c.Id == guid, token);

    public async Task<bool> DestroyContainer(Container container, CancellationToken token = default)
    {
        try
        {
            await service.DestroyContainerAsync(container, token);

            if (container.Status != ContainerStatus.Destroyed)
                return false;

            await cache.RemoveAsync(CacheKey.ConnectionCount(container.Id), token);

            Context.Containers.Remove(container);
            await SaveAsync(token);

            return true;
        }
        catch (Exception ex)
        {
            logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerRepository_ContainerDestroyFailed),
                    container.ContainerId[..12],
                    container.Image.Split("/").LastOrDefault() ?? "", ex.Message],
                TaskStatus.Failed, LogLevel.Warning);
            return false;
        }
    }

    public Task<List<Container>> GetContainers(CancellationToken token = default) =>
        Context.Containers.ToListAsync(token);
}
