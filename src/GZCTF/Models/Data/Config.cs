using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace GZCTF.Models.Data;

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

    public Config(string key, string value, string[] cacheKeys)
    {
        ConfigKey = key;
        Value = value;
        CacheKeys = cacheKeys;
    }

    [Key]
    public string ConfigKey { get; set; } = string.Empty;

    public string? Value { get; set; }

    /// <summary>
    /// 在更新配置时使用，若不为空则会删除对应缓存
    /// </summary>
    /// <remarks>
    /// 仅在 <see cref="GZCTF.Services.Config.ConfigService.SaveConfigSet" /> 中使用
    /// </remarks>
    [NotMapped]
    [JsonIgnore]
    public string[]? CacheKeys { get; set; }
}
