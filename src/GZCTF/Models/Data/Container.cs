using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GZCTF.Utils;

namespace GZCTF.Models;

public class Container
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 镜像名称
    /// </summary>
    [Required]
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// 容器 ID
    /// </summary>
    [Required]
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>
    /// 容器状态
    /// </summary>
    [Required]
    public ContainerStatus Status { get; set; } = ContainerStatus.Pending;

    /// <summary>
    /// 容器创建时间
    /// </summary>
    [Required]
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 容器期望终止时间
    /// </summary>
    [Required]
    public DateTimeOffset ExpectStopAt { get; set; } = DateTimeOffset.UtcNow + TimeSpan.FromHours(2);

    /// <summary>
    /// 是否具备反向代理
    /// </summary>
    [Required]
    public bool IsProxy { get; set; } = false;

    /// <summary>
    /// 本地 IP
    /// </summary>
    [Required]
    public string IP { get; set; } = string.Empty;

    /// <summary>
    /// 本地端口
    /// </summary>
    [Required]
    public int Port { get; set; }

    /// <summary>
    /// 公开 IP
    /// </summary>
    public string? PublicIP { get; set; }

    /// <summary>
    /// 公开端口
    /// </summary>
    public int? PublicPort { get; set; }

    /// <summary>
    /// 容器实例访问方式
    /// </summary>
    [NotMapped]
    public string Entry => IsProxy ? Id : $"{PublicIP ?? IP}:{PublicPort ?? Port}";

    /// <summary>
    /// 容器实例流量捕获存储路径
    /// </summary>
    public string TrafficPath(string conn) => Instance is null ? string.Empty :
        Path.Combine(FilePath.Capture,
            $"{Instance.ChallengeId}/{Instance.ParticipationId}/{DateTimeOffset.Now:yyyyMMdd-HH.mm.ssZ}-{conn}.pcap");

    #region Db Relationship

    /// <summary>
    /// 比赛题目实例对象
    /// </summary>
    public Instance? Instance { get; set; }

    /// <summary>
    /// 实例对象ID
    /// </summary>
    public int InstanceId { get; set; }

    #endregion Db Relationship
}
