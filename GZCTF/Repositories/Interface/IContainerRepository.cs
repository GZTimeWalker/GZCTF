using CTFServer.Models;

namespace CTFServer.Repositories.Interface;

public interface IContainerRepository : IRepository
{
    /// <summary>
    /// 获取全部容器信息
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<List<Container>> GetContainers(CancellationToken token = default);

    /// <summary>
    /// 获取全部待销毁容器
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<List<Container>> GetDyingContainers(CancellationToken token = default);

    /// <summary>
    /// 移除指定的容器（已经销毁的容器）
    /// </summary>
    /// <param name="container">容器对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> RemoveContainer(Container container, CancellationToken token = default);
}
