using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models;

public class Instance
{
    /// <summary>
    /// 题目是否已经解决
    /// </summary>
    public bool IsSolved { get; set; } = false;

    /// <summary>
    /// 题目是否已经加载
    /// </summary>
    public bool IsLoaded { get; set; } = false;

    /// <summary>
    /// 自定义成绩 (unused)
    /// </summary>
    public int Score { get; set; } = 0;

    /// <summary>
    /// 最后一次容器操作的时间，确保单题目容器操作不会过于频繁
    /// </summary>
    public DateTimeOffset LastContainerOperation { get; set; } = DateTimeOffset.MinValue;

    #region Db Relationship

    public int? FlagId { get; set; }

    /// <summary>
    /// Flag 上下文对象
    /// </summary>
    public FlagContext? FlagContext { get; set; } = default!;

    [Required]
    public int ChallengeId { get; set; }

    /// <summary>
    /// 赛题对象
    /// </summary>
    public Challenge Challenge { get; set; } = default!;

    public string? ContainerId { get; set; }

    /// <summary>
    /// 容器对象
    /// </summary>
    public Container? Container { get; set; }

    [Required]
    public int ParticipationId { get; set; }

    /// <summary>
    /// 参与队伍对象
    /// </summary>
    public Participation Participation { get; set; } = default!;

    #endregion Db Relationship
}
