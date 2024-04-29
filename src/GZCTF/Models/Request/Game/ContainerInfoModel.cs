namespace GZCTF.Models.Request.Game;

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

    internal static ContainerInfoModel FromContainer(Container container) =>
        new()
        {
            Status = container.Status,
            StartedAt = container.StartedAt,
            ExpectStopAt = container.ExpectStopAt,
            Entry = container.Entry
        };
}
