using System.ComponentModel;
using System.Reflection;
using GZCTF.Models.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using ConfigModel = GZCTF.Models.Data.Config;

namespace GZCTF.Services.Config;

public class ConfigService(
    AppDbContext context,
    IDistributedCache cache,
    ILogger<ConfigService> logger,
    IConfiguration configuration) : IConfigService
{
    readonly IConfigurationRoot? _configuration = configuration as IConfigurationRoot;

    public Task SaveConfig(Type type, object? value, CancellationToken token = default) =>
        SaveConfigSet(GetConfigs(type, value), token);

    public Task SaveConfig<T>(T config, CancellationToken token = default) where T : class =>
        SaveConfigSet(GetConfigs(config), token);

    public void ReloadConfig() => _configuration?.Reload();

    public async Task SaveConfigSet(HashSet<ConfigModel> configs, CancellationToken token = default)
    {
        Dictionary<string, ConfigModel> dbConfigs = await context.Configs
            .ToDictionaryAsync(c => c.ConfigKey, c => c, token);
        HashSet<string> cacheKeys = [];

        foreach (ConfigModel conf in configs)
        {
            if (dbConfigs.TryGetValue(conf.ConfigKey, out ConfigModel? dbConf))
            {
                if (dbConf.Value == conf.Value)
                    continue;

                dbConf.Value = conf.Value;

                if (conf.CacheKeys is not null)
                    cacheKeys.UnionWith(conf.CacheKeys);

                logger.SystemLog(
                    Program.StaticLocalizer[nameof(Resources.Program.Config_GlobalConfigUpdated), conf.ConfigKey,
                        conf.Value ?? "null"],
                    TaskStatus.Success, LogLevel.Debug);
            }
            else
            {
                if (conf.CacheKeys is not null)
                    cacheKeys.UnionWith(conf.CacheKeys);

                await context.Configs.AddAsync(conf, token);

                logger.SystemLog(
                    Program.StaticLocalizer[nameof(Resources.Program.Config_GlobalConfigAdded), conf.ConfigKey,
                        conf.Value ?? "null"],
                    TaskStatus.Success, LogLevel.Debug);
            }
        }

        await context.SaveChangesAsync(token);
        _configuration?.Reload();

        // flush cache
        foreach (var key in cacheKeys)
            await cache.RemoveAsync(key, token);
    }

    static void MapConfigsInternal(string key, HashSet<ConfigModel> configs, PropertyInfo info, object? value)
    {
        // ignore when value with `AutoSaveIgnoreAttribute`
        if (value is null || info.GetCustomAttribute<AutoSaveIgnoreAttribute>() != null)
            return;

        Type type = info.PropertyType;
        if (type.IsArray || IsArrayLikeInterface(type))
            throw new NotSupportedException(Program.StaticLocalizer[nameof(Resources.Program.Config_TypeNotSupported)]);

        TypeConverter converter = TypeDescriptor.GetConverter(type);

        if (type == typeof(string) || type.IsValueType)
        {
            var keys = info.GetCustomAttributes<CacheFlushAttribute>()
                .Select(a => a.CacheKey).ToArray();

            configs.Add(new(key, converter.ConvertToString(value) ?? string.Empty, keys));
        }
        else if (type.IsClass)
        {
            foreach (PropertyInfo item in type.GetProperties())
                MapConfigsInternal($"{key}:{item.Name}", configs, item, item.GetValue(value));
        }
    }

    static HashSet<ConfigModel> GetConfigs(Type type, object? value)
    {
        HashSet<ConfigModel> configs = [];

        foreach (PropertyInfo item in type.GetProperties())
            MapConfigsInternal($"{type.Name}:{item.Name}", configs, item, item.GetValue(value));

        return configs;
    }

    public static HashSet<ConfigModel> GetConfigs<T>(T config) where T : class
    {
        HashSet<ConfigModel> configs = [];
        Type type = typeof(T);

        foreach (PropertyInfo item in type.GetProperties())
            MapConfigsInternal($"{type.Name}:{item.Name}", configs, item, item.GetValue(config));

        return configs;
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
