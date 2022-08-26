using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Data;

/// <summary>
/// 用于存放配置项
/// </summary>
public record Config
{
    public Config() { }
    public Config(string key, string value)
    {
        ConfigKey = key;
        Value = value;
    }

    [Key]
    public string ConfigKey { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}