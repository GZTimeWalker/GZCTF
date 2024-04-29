using GZCTF.Models.Request.Admin;

namespace GZCTF.Repositories.Interface;

public interface IContainerRepository : IRepository
{
    /// <summary>
    /// 根据容器数据库 ID 获取容器
    /// </summary>
    /// <param name="guid">容器数据库 ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Container?> GetContainerById(Guid guid, CancellationToken token = default);

    /// <summary>
    /// 根据容器数据库 ID 获取容器及实例信息
    /// </summary>
    /// <param name="guid">容器数据库 ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Container?> GetContainerWithInstanceById(Guid guid, CancellationToken token = default);

    /// <summary>
    /// 容器数据库 ID 对应容器是否存在
    /// </summary>
    /// <param name="guid">容器数据库 ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> ValidateContainer(Guid guid, CancellationToken token = default);

    /// <summary>
    /// 获取容器实例信息
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ContainerInstanceModel[]> GetContainerInstances(CancellationToken token = default);

    /// <summary>
    /// 获取全部待销毁容器
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<List<Container>> GetDyingContainers(CancellationToken token = default);

    /// <summary>
    /// 容器延期
    /// </summary>
    /// <param name="container">容器实例对象</param>
    /// <param name="time">延长时间</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task ExtendLifetime(Container container, TimeSpan time, CancellationToken token = default);

    /// <summary>
    /// 销毁容器
    /// </summary>
    /// <param name="container"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> DestroyContainer(Container container, CancellationToken token = default);
}
