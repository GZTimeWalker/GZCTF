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

    [Required]
    public int GameId { get; set; }

    /// <summary>
    /// 比赛对象
    /// </summary>
    public Game Game { get; set; } = default!;

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