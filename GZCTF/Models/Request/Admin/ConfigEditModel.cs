using CTFServer.Models.Internal;

namespace CTFServer.Models.Request.Admin;

/// <summary>
/// 全局配置更新对象
/// </summary>
public class ConfigEditModel
{
    /// <summary>
    /// 用户策略
    /// </summary>
    public AccountPolicy? AccountPolicy { get; set; }

    /// <summary>
    /// 全局配置项
    /// </summary>
    public GlobalConfig? GlobalConfig { get; set; }
}