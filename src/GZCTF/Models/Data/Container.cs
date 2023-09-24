using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(GameInstanceId))]
[Index(nameof(ExerciseInstanceId))]
public class Container
{
    [Key]
    public Guid Id { get; set; }

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
    public bool IsProxy { get; set; }

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
    public string Entry => IsProxy ? Id.ToString() : $"{PublicIP ?? IP}:{PublicPort ?? Port}";

    /// <summary>
    /// 是否启用流量捕获
    /// </summary>
    [NotMapped]
    public bool EnableTrafficCapture => GameInstance?.Challenge.EnableTrafficCapture ?? false;

    /// <summary>
    /// 容器实例流量捕获存储路径
    /// </summary>
    public string TrafficPath(string conn) =>
        GameInstance is null
            ? string.Empty
            : Path.Combine(FilePath.Capture,
                $"{GameInstance.ChallengeId}/{GameInstance.ParticipationId}/{DateTimeOffset.Now:yyyyMMdd-HH.mm.ss}-{conn}.pcap");

    /// <summary>
    /// 生成容器的元数据信息
    /// </summary>
    /// <returns></returns>
    public byte[]? GenerateMetadata(JsonSerializerOptions? options = null)
    {
        if (GameInstance is not null)
            return JsonSerializer.SerializeToUtf8Bytes(
                new
                {
                    Challenge = GameInstance.Challenge.Title,
                    GameInstance.ChallengeId,
                    Team = GameInstance.Participation.Team.Name,
                    GameInstance.Participation.TeamId,
                    ContainerId,
                    GameInstance.FlagContext?.Flag
                }, options);

        if (ExerciseInstance is not null)
            return JsonSerializer.SerializeToUtf8Bytes(
                new
                {
                    Challenge = ExerciseInstance.Challenge.Title,
                    ExerciseInstance.ChallengeId,
                    ExerciseInstance.User.UserName,
                    ExerciseInstance.UserId,
                    ContainerId,
                    ExerciseInstance.FlagContext?.Flag
                }, options);

        return null;
    }

    #region Db Relationship

    /// <summary>
    /// 比赛题目实例对象
    /// </summary>
    public GameInstance? GameInstance { get; set; }

    /// <summary>
    /// 比赛题目实例对象 ID
    /// </summary>
    public int GameInstanceId { get; set; }

    /// <summary>
    /// 练习题目实例对象
    /// </summary>
    public ExerciseInstance? ExerciseInstance { get; set; }

    /// <summary>
    /// 练习题目实例对象 ID
    /// </summary>
    public int ExerciseInstanceId { get; set; }

    #endregion Db Relationship
}