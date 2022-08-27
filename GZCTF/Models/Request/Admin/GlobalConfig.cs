using System;
using CTFServer.Models.Internal;

namespace CTFServer.Models.Request.Admin;

/// <summary>
/// 全局配置更新对象
/// </summary>
public class GlobalConfig
{
    public AccountPolicy? AccoutPolicy { get; set; }
}