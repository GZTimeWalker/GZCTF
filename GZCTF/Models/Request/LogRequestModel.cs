using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request;

/// <summary>
/// 日志请求
/// </summary>
public class LogRequestModel
{
    /// <summary>
    /// 跳过数量
    /// </summary>
    [DefaultValue(0)]
    public int Skip { get; set; } = 0;

    /// <summary>
    /// 获取数量
    /// </summary>
    [Range(1, 50)]
    [DefaultValue(50)]
    public int Count { get; set; } = 50;

    /// <summary>
    /// 日志等级
    /// </summary>
    [DefaultValue("All")]
    public string Level { get; set; } = "All";
}
