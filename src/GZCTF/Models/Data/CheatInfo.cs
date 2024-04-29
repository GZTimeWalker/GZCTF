using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// 抄袭行为记录
/// </summary>
[Index(nameof(GameId))]
[Index(nameof(SubmissionId), IsUnique = true)]
public class CheatInfo
{
    #region Db Relationship

    /// <summary>
    /// 比赛对象
    /// </summary>
    public Game Game { get; set; } = default!;

    /// <summary>
    /// 比赛对象 ID
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// 提交对应 flag 的队伍
    /// </summary>
    public Participation SubmitTeam { get; set; } = default!;

    /// <summary>
    /// 提交对应 flag 的队伍 ID
    /// </summary>
    public int SubmitTeamId { get; set; }

    /// <summary>
    /// 对应 flag 所属队伍
    /// </summary>
    public Participation SourceTeam { get; set; } = default!;

    /// <summary>
    /// 对应 flag 所属队伍 ID
    /// </summary>
    public int SourceTeamId { get; set; }

    /// <summary>
    /// 本次抄袭行为对应的提交
    /// </summary>
    public Submission Submission { get; set; } = default!;

    /// <summary>
    /// 本次抄袭行为对应的提交 ID
    /// </summary>
    public int SubmissionId { get; set; }

    #endregion Db Relationship
}
