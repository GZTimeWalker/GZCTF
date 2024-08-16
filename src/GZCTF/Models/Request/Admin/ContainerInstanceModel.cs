namespace GZCTF.Models.Request.Admin;

/// <summary>
/// 容器实例信息（Admin）
/// </summary>
public class ContainerInstanceModel
{
    /// <summary>
    /// 队伍
    /// </summary>
    public TeamModel? Team { get; set; }

    /// <summary>
    /// 题目
    /// </summary>
    public ChallengeModel? Challenge { get; set; }

    /// <summary>
    /// 容器镜像
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// 容器数据库 ID
    /// </summary>
    public Guid ContainerGuid { get; set; }

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
    /// 访问 IP
    /// </summary>
    public string IP { get; set; } = string.Empty;

    /// <summary>
    /// 访问端口
    /// </summary>
    public int Port { get; set; }

    internal static ContainerInstanceModel FromContainer(Container container)
    {
        Team? team = container.GameInstance?.Participation.Team;
        GameChallenge? chal = container.GameInstance?.Challenge;

        var model = new ContainerInstanceModel
        {
            Image = container.Image,
            ContainerGuid = container.Id,
            ContainerId = container.ContainerId,
            StartedAt = container.StartedAt,
            ExpectStopAt = container.ExpectStopAt,
            // fallback to host if public is null
            IP = container.PublicIP ?? container.IP,
            Port = container.PublicPort ?? container.Port
        };

        if (team is not null && chal is not null)
        {
            model.Team = TeamModel.FromTeam(team);
            model.Challenge = ChallengeModel.FromChallenge(chal);
        }

        return model;
    }
}
