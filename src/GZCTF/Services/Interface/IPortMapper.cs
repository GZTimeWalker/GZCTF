using GZCTF.Models.Internal;

namespace GZCTF.Services.Interface;

public interface IPortMapper
{
    /// <summary>
    /// 对容器进行端口映射
    /// </summary>
    /// <param name="container">容器实例</param>
    /// <param name="config">容器创建配置</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Container?> MapContainer(Container container, ContainerConfig config, CancellationToken token = default);

    /// <summary>
    /// 对容器取消端口映射
    /// </summary>
    /// <param name="container">容器实例</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Container?> UnmapContainer(Container container, CancellationToken token = default);
}