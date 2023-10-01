namespace GZCTF.Models.Internal;

public class ContainerConfig
{
    /// <summary>
    /// 容器镜像
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// 队伍 Id
    /// </summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    /// 用户 Id
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 容器需要暴露的端口
    /// </summary>
    public int ExposedPort { get; set; }

    /// <summary>
    /// Flag文本
    /// </summary>
    public string? Flag { get; set; } = string.Empty;

    /// <summary>
    /// 是否需要记录访问流量
    /// </summary>
    public bool EnableTrafficCapture { get; set; }

    /// <summary>
    /// 内存限制（MB）
    /// </summary>
    public int MemoryLimit { get; set; } = 64;

    /// <summary>
    /// CPU 限制 (0.1 CPUs)
    /// </summary>
    public int CPUCount { get; set; } = 1;

    /// <summary>
    /// 存储写入限制
    /// </summary>
    public int StorageLimit { get; set; } = 256;
}
