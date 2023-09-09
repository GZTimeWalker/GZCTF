using GZCTF.Models.Internal;

namespace GZCTF.Services.Interface;

public interface IContainerManager
{
    /// <summary>
    /// 创建容器
    /// </summary>
    /// <param name="config">容器配置</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Models.Data.Container?> CreateContainerAsync(ContainerConfig config, CancellationToken token = default);

    /// <summary>
    /// 销毁容器
    /// </summary>
    /// <param name="container">容器对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DestroyContainerAsync(Models.Data.Container container, CancellationToken token = default);
}
