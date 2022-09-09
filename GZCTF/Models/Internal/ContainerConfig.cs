namespace CTFServer.Models.Internal;

public class ContainerConfig
{
    /// <summary>
    /// 容器镜像
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// 队伍信息
    /// </summary>
    public string TeamInfo { get; set; } = string.Empty;

    /// <summary>
    /// 容器需要暴露的端口
    /// </summary>
    public int ExposedPort { get; set; }

    /// <summary>
    /// Flag文本
    /// </summary>
    public string? Flag { get; set; } = string.Empty;

    /// <summary>
    /// 内存限制（MB）
    /// </summary>
    public int MemoryLimit { get; set; } = 64;

    /// <summary>
    /// CPU 数量
    /// </summary>
    public int CPUCount { get; set; } = 1;
}