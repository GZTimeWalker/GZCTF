using GZCTF.Models.Request.Admin;

namespace GZCTF.Repositories.Interface;

public interface IContainerRepository : IRepository
{
    /// <summary>
    /// Get container by database ID
    /// </summary>
    /// <param name="guid">container ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Container?> GetContainerById(Guid guid, CancellationToken token = default);

    /// <summary>
    /// Get container with instance info by database ID
    /// </summary>
    /// <param name="guid">container ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Container?> GetContainerWithInstanceById(Guid guid, CancellationToken token = default);

    /// <summary>
    /// Check if the container exists by database ID
    /// </summary>
    /// <param name="guid">container ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> ValidateContainer(Guid guid, CancellationToken token = default);

    /// <summary>
    /// Get all container instances
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ContainerInstanceModel[]> GetContainerInstances(CancellationToken token = default);

    /// <summary>
    /// Get all containers that are about to be stopped
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Container[]> GetDyingContainers(CancellationToken token = default);

    /// <summary>
    /// Extend container lifetime
    /// </summary>
    /// <param name="container">container</param>
    /// <param name="time">extension period</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task ExtendLifetime(Container container, TimeSpan time, CancellationToken token = default);

    /// <summary>
    /// Destroy container and remove it from database
    /// </summary>
    /// <param name="container"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> DestroyContainer(Container container, CancellationToken token = default);
}
