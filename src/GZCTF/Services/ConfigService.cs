using System.ComponentModel;
using System.Reflection;
using GZCTF.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Services;

public class ConfigService(AppDbContext context,
    ILogger<ConfigService> logger,
    IConfiguration configuration) : IConfigService
{
    readonly IConfigurationRoot? _configuration = configuration as IConfigurationRoot;

    public Task SaveConfig(Type type, object? value, CancellationToken token = default) => SaveConfigInternal(GetConfigs(type, value), token);

    public Task SaveConfig<T>(T config, CancellationToken token = default) where T : class => SaveConfigInternal(GetConfigs(config), token);

    public void ReloadConfig() => _configuration?.Reload();

    static void MapConfigsInternal(string key, HashSet<Config> configs, Type? type, object? value)
    {
        if (value is null || type is null)
            return;

        if (type.IsArray || IsArrayLikeInterface(type))
            throw new InvalidOperationException("不支持的配置项类型");

        TypeConverter converter = TypeDescriptor.GetConverter(type);
        if (type == typeof(string) || type.IsValueType)
            configs.Add(new(key, converter.ConvertToString(value) ?? string.Empty));
        else if (type.IsClass)
            foreach (PropertyInfo item in type.GetProperties())
                MapConfigsInternal($"{key}:{item.Name}", configs, item.PropertyType, item.GetValue(value));
    }

    static HashSet<Config> GetConfigs(Type type, object? value)
    {
        HashSet<Config> configs = new();

        foreach (PropertyInfo item in type.GetProperties())
            MapConfigsInternal($"{type.Name}:{item.Name}", configs, item.PropertyType, item.GetValue(value));

        return configs;
    }

    public static HashSet<Config> GetConfigs<T>(T config) where T : class
    {
        HashSet<Config> configs = new();
        Type type = typeof(T);

        foreach (PropertyInfo item in type.GetProperties())
            MapConfigsInternal($"{type.Name}:{item.Name}", configs, item.PropertyType, item.GetValue(config));

        return configs;
    }

    async Task SaveConfigInternal(HashSet<Config> configs, CancellationToken token = default)
    {
        Dictionary<string, Config> dbConfigs = await context.Configs.ToDictionaryAsync(c => c.ConfigKey, c => c, token);
        foreach (Config conf in configs)
            if (dbConfigs.TryGetValue(conf.ConfigKey, out Config? dbConf))
            {
                if (dbConf.Value != conf.Value)
                {
                    dbConf.Value = conf.Value;
                    logger.SystemLog($"更新全局设置：{conf.ConfigKey} => {conf.Value}", TaskStatus.Success, LogLevel.Debug);
                }
            }
            else
            {
                logger.SystemLog($"添加全局设置：{conf.ConfigKey} => {conf.Value}", TaskStatus.Success, LogLevel.Debug);
                await context.Configs.AddAsync(conf, token);
            }

        await context.SaveChangesAsync(token);
        _configuration?.Reload();
    }

    static bool IsArrayLikeInterface(Type type)
    {
        if (!type.IsInterface || !type.IsConstructedGenericType)
            return false;

        Type genericTypeDefinition = type.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(IEnumerable<>)
               || genericTypeDefinition == typeof(ICollection<>)
               || genericTypeDefinition == typeof(IList<>)
               || genericTypeDefinition == typeof(IDictionary<,>)
               || genericTypeDefinition == typeof(ISet<>);
    }
}