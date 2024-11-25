using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace GZCTF.Models.Data;

/// <summary>
/// Used to store configuration items
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
    /// Used when updating the configuration, if not empty, the corresponding cache will be deleted
    /// </summary>
    /// <remarks>
    /// Only used in <see cref="GZCTF.Services.Config.ConfigService.SaveConfigSet" />
    /// </remarks>
    [NotMapped]
    [JsonIgnore]
    public string[]? CacheKeys { get; set; }
}
