namespace GZCTF.Models.Request.Cyctf;

/// <summary>
/// 创建/更新组别扩展信息请求
/// </summary>
public class DivisionExtensionRequest
{
    /// <summary>
    /// 最小队伍人数
    /// </summary>
    public int? MinTeamSize { get; set; }

    /// <summary>
    /// 最大队伍人数
    /// </summary>
    public int? MaxTeamSize { get; set; }

    /// <summary>
    /// 报名自定义字段配置（JSON 字符串）
    /// </summary>
    public string? RegistrationFields { get; set; }
}
