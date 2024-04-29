namespace GZCTF.Services.Interface;

public interface IContainerProvider<T, M>
{
    /// <summary>
    /// 获取容器服务提供者
    /// </summary>
    /// <returns></returns>
    public T GetProvider();

    /// <summary>
    /// 获取容器服务信息
    /// </summary>
    /// <returns></returns>
    public M GetMetadata();
}
