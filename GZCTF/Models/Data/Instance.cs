using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models;

public class Instance
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 关闭时间
    /// </summary>
    public DateTimeOffset EndTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 题目是否已经解决
    /// </summary>
    public bool IsSolved { get; set; } = false;

    /// <summary>
    /// 题目实例排名
    /// </summary>
    public int Ranking { get; set; } = 0;

    #region Db Relationship
    [Required]
    public int FlagId { get; set; }

    /// <summary>
    /// Flag 对象
    /// </summary>
    public FlagContext Flag { get; set; } = default!;

    [Required]
    public int ChallengeId { get; set; }

    /// <summary>
    /// 赛题对象
    /// </summary>
    public Challenge Challenge { get; set; } = default!;

    [Required]
    public int GameId { get; set; }

    /// <summary>
    /// 比赛对象
    /// </summary>
    public Game? Game { get; set; }

    public string? ContainerId { get; set; }

    /// <summary>
    /// 容器对象
    /// </summary>
    public Container? Container { get; set; }

    #endregion Db Relationship
}
