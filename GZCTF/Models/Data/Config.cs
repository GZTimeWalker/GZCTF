using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Data;

/// <summary>
/// 用于存放配置项
/// </summary>
public record Config
{
    public Config(string key, string value)
    {
        Key = key;
        Value = value;
    }

    [Key]
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}