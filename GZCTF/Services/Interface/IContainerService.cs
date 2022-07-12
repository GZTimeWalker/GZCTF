using CTFServer.Models.Internal;

namespace CTFServer.Services.Interface;

public interface IContainerService
{
    /// <summary>
    /// 创建容器
    /// </summary>
    /// <param name="config">容器配置</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Container?> CreateContainer(ContainerConfig config, CancellationToken token = default);

    /// <summary>
    /// 销毁容器
    /// </summary>
    /// <param name="container">容器对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DestoryContainer(Container container, CancellationToken token = default);

    /// <summary>
    /// 获取容器主机信息
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<string> GetHostInfo(CancellationToken token = default);

    /// <summary>
    /// 获取全部容器
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<IList<ContainerInfo>> GetContainers(CancellationToken token = default);
}