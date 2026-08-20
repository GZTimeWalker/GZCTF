namespace GZCTF.Models.Request.Cyctf;

/// <summary>
/// 报名请求
/// </summary>
public class RegistrationRequest
{
    /// <summary>
    /// 比赛 ID
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// 队伍 ID
    /// </summary>
    public int TeamId { get; set; }

    /// <summary>
    /// 组别 ID
    /// </summary>
    public int DivisionId { get; set; }

    /// <summary>
    /// 报名表单数据（JSON 字符串）
    /// </summary>
    public string? FormData { get; set; }
}

/// <summary>
/// 审核报名请求
/// </summary>
public class RegistrationReviewRequest
{
    /// <summary>
    /// 审核状态（APPROVED, REJECTED）
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 审核备注
    /// </summary>
    public string? ReviewNote { get; set; }
}
