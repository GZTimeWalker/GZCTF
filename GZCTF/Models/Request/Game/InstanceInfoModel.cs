using CTFServer.Models.Request.Edit;

namespace CTFServer.Models.Request.Game;

public class InstanceInfoModel
{
    /// <summary>
    /// 实例 Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 队伍 Id
    /// </summary>
    public int TeamId { get; set; }

    /// <summary>
    /// 队伍名
    /// </summary>
    public string TeamName { get; set; } = string.Empty;

    /// <summary>
    /// 题目详情
    /// </summary>
    public ChallengeInfoModel Challenge { get; set; } = default!;

    /// <summary>
    /// 容器信息
    /// </summary>
    public ContainerInfoModel? Container { get; set; }

    public static InstanceInfoModel FromInstance(Instance instance)
        => new()
        {
            Id = instance.Id,
            TeamId = instance.Participation.TeamId,
            TeamName = instance.Participation.Team.Name,
            Challenge = new()
            {
                Id = instance.ChallengeId,
                Title = instance.Challenge.Title,
                Type = instance.Challenge.Type,
                Tag = instance.Challenge.Tag
            },
            Container = instance.Container is null ? null : ContainerInfoModel.FromContainer(instance.Container)
        };
}

public class ContainerInfoModel
{
    /// <summary>
    /// 容器状态
    /// </summary>
    public ContainerStatus Status { get; set; } = ContainerStatus.Pending;

    /// <summary>
    /// 容器创建时间
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 容器期望终止时间
    /// </summary>
    public DateTimeOffset ExpectStopAt { get; set; } = DateTimeOffset.UtcNow + TimeSpan.FromHours(2);

    /// <summary>
    /// 题目入口
    /// </summary>
    public string Entry { get; set; } = string.Empty;

    public static ContainerInfoModel FromContainer(Container container)
        => new()
        {
            Status = container.Status,
            StartedAt = container.StartedAt,
            ExpectStopAt = container.ExpectStopAt,
            Entry = container.Entry
        };
}