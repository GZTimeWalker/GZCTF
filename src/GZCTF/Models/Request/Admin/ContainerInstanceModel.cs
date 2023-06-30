using System.Text.Json.Serialization;

namespace CTFServer.Models.Request.Admin;

/// <summary>
/// 容器实例信息（Admin）
/// </summary>
public class ContainerInstanceModel
{
    /// <summary>
    /// 队伍信息
    /// </summary>
    /// <param name="Name">队名</param>
    /// <param name="Id">队伍 ID</param>
    public record TeamModel(int Id, string Name);

    /// <summary>
    /// 题目信息
    /// </summary>
    /// <param name="Id">题目 ID</param>
    /// <param name="Title">题目名称</param>
    /// <param name="Tag">题目标签</param>
    public record ChallengeModel(int Id, string Title, ChallengeTag Tag);

    /// <summary>
    /// 队伍
    /// </summary>
    public TeamModel? Team { get; set; } = default!;

    /// <summary>
    /// 题目
    /// </summary>
    public ChallengeModel? Challenge { get; set; } = default!;

    /// <summary>
    /// 容器镜像
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// 容器数据库 ID
    /// </summary>
    public string ContainerGuid { get; set; } = string.Empty;

    /// <summary>
    /// 容器 ID
    /// </summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>
    /// 容器创建时间
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 容器期望终止时间
    /// </summary>
    public DateTimeOffset ExpectStopAt { get; set; } = DateTimeOffset.UtcNow + TimeSpan.FromHours(2);

    /// <summary>
    /// 公开 IP
    /// </summary>
    public string? PublicIP { get; set; }

    /// <summary>
    /// 公开端口
    /// </summary>
    public int? PublicPort { get; set; }

    internal static ContainerInstanceModel FromContainer(Container container)
    {
        var team = container.Instance?.Participation?.Team;
        var chal = container.Instance?.Challenge;

        var model = new ContainerInstanceModel() {
            Image = container.Image,
            ContainerGuid = container.Id,
            ContainerId = container.ContainerId,
            StartedAt = container.StartedAt,
            ExpectStopAt = container.ExpectStopAt,
            PublicIP = container.PublicIP,
            PublicPort = container.PublicPort
        };

        if (team is not null && chal is not null)
        {
            model.Team = new TeamModel(team.Id, team.Name);
            model.Challenge = new ChallengeModel(chal.Id, chal.Title, chal.Tag);
        }

        return model;
    }
}