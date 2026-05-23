namespace GZCTF.Models.Request.Game;

public class ContainerInfoModel
{
    /// <summary>
    /// Container GUID — surfaced so the admin UI can target the
    /// in-browser exec shell at this specific container without
    /// going through the /admin/instances list. Same value the
    /// <c>ContainerExecHub</c> expects.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Container status
    /// </summary>
    public ContainerStatus Status { get; set; } = ContainerStatus.Pending;

    /// <summary>
    /// Container creation time
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Expected container stop time
    /// </summary>
    public DateTimeOffset ExpectStopAt { get; set; } = DateTimeOffset.UtcNow + TimeSpan.FromHours(2);

    /// <summary>
    /// Challenge entry point
    /// </summary>
    public string Entry { get; set; } = string.Empty;

    internal static ContainerInfoModel FromContainer(Container container) =>
        new()
        {
            Id = container.Id,
            Status = container.Status,
            StartedAt = container.StartedAt,
            ExpectStopAt = container.ExpectStopAt,
            Entry = container.Entry
        };
}
