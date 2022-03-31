using CTFServer.Models;
using Docker.DotNet.Models;

namespace CTFServer.Services.Interface;

public interface IContainerService
{
    /// <summary>
    /// 创建容器
    /// </summary>
    /// <param name="image">镜像名</param>
    /// <param name="port">需要暴露的端口</param>
    /// <param name="flag">Flag 内容</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Container?> CreateContainer(string image, int port, string flag, CancellationToken token = default);

    /// <summary>
    /// 创建容器
    /// </summary>
    /// <param name="parameters">容器</param>
    /// <returns></returns>
    public Task<Container?> CreateContainer(CreateContainerParameters parameters, CancellationToken token = default);


    /// <summary>
    /// 销毁容器
    /// </summary>
    /// <param name="container">容器对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskStatus> DestoryContainer(Container container, CancellationToken token = default);

    /// <summary>
    /// 获取容器主机信息
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<(VersionResponse, SystemInfoResponse)> GetHostInfo(CancellationToken token = default);

    /// <summary>
    /// 获取全部容器
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<IList<ContainerListResponse>> GetContainers(CancellationToken token = default);
}
