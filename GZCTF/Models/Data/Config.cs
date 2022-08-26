using System;
namespace CTFServer.Models.Data;

/// <summary>
/// 用于存放配置项
/// </summary>
/// <param name="Key"></param>
/// <param name="Value"></param>
public record Config
{
    public Config(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
