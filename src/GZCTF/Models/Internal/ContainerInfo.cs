namespace GZCTF.Models.Internal;

public class ContainerInfo
{
    /// <summary>
    /// 容器 Id
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// 容器名称
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// 容器镜像
    /// </summary>
    public string Image { get; set; } = default!;

    /// <summary>
    /// 容器状态
    /// </summary>
    public string State { get; set; } = default!;
}
