using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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

    public Task SaveConfig([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        object? value, CancellationToken token = default) =>
        SaveConfigSet(GetConfigs(type, value), token);

    public Task SaveConfig<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T config,
        CancellationToken token = default) where T : class =>
        SaveConfigSet(GetConfigs(config), token);

    public void ReloadConfig() => _configuration?.Reload();

    public async Task SaveConfigSet(HashSet<ConfigModel> configs, CancellationToken token = default)
    {
        var dbConfigs = await context.Configs
            .ToDictionaryAsync(c => c.ConfigKey, c => c, token);
        HashSet<string> cacheKeys = [];

        foreach (var conf in configs)
        {
            if (dbConfigs.TryGetValue(conf.ConfigKey, out var dbConf))
            {
                if (dbConf.Value == conf.Value)
                    continue;

                dbConf.Value = conf.Value;

                if (conf.CacheKeys is not null)
                    cacheKeys.UnionWith(conf.CacheKeys);

                logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.Config_GlobalConfigUpdated), conf.ConfigKey,
                        conf.Value ?? "null"],
                    TaskStatus.Success, LogLevel.Debug);
            }
            else
            {
                if (conf.CacheKeys is not null)
                    cacheKeys.UnionWith(conf.CacheKeys);

                await context.Configs.AddAsync(conf, token);

                logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.Config_GlobalConfigAdded), conf.ConfigKey,
                        conf.Value ?? "null"],
                    TaskStatus.Success, LogLevel.Debug);
            }
        }

        await context.SaveChangesAsync(token);
        ReloadConfig();

        // flush cache
        foreach (var key in cacheKeys)
            await cache.RemoveAsync(key, token);
    }

    static void MapConfigsInternal(string key, HashSet<ConfigModel> configs, PropertyInfo info, object? value)
    {
        // ignore when value with `AutoSaveIgnoreAttribute`
        if (value is null || info.GetCustomAttribute<AutoSaveIgnoreAttribute>() != null)
            return;

        var type = info.PropertyType;
        if (type.IsArray || IsArrayLikeInterface(type))
            throw new NotSupportedException(StaticLocalizer[nameof(Resources.Program.Config_TypeNotSupported)]);

        var converter = TypeDescriptor.GetConverter(type);

        if (type == typeof(string) || type.IsValueType)
        {
            var keys = info.GetCustomAttributes<CacheFlushAttribute>()
                .Select(a => a.CacheKey).ToArray();

            configs.Add(new(key, converter.ConvertToString(value) ?? string.Empty, keys));
        }
        else if (type.IsClass)
        {
            foreach (var item in type.GetProperties())
                MapConfigsInternal($"{key}:{item.Name}", configs, item, item.GetValue(value));
        }
    }

    static HashSet<ConfigModel> GetConfigs(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type, object? value)
    {
        HashSet<ConfigModel> configs = [];

        foreach (var item in type.GetProperties())
            MapConfigsInternal($"{type.Name}:{item.Name}", configs, item, item.GetValue(value));

        return configs;
    }

    public static HashSet<ConfigModel> GetConfigs<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    T>(T config) where T : class
    {
        HashSet<ConfigModel> configs = [];
        var type = typeof(T);

        foreach (var item in type.GetProperties())
            MapConfigsInternal($"{type.Name}:{item.Name}", configs, item, item.GetValue(config));

        return configs;
    }

    static bool IsArrayLikeInterface(Type type)
    {
        if (!type.IsInterface || !type.IsConstructedGenericType)
            return false;

        var genericTypeDefinition = type.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(IEnumerable<>)
               || genericTypeDefinition == typeof(ICollection<>)
               || genericTypeDefinition == typeof(IList<>)
               || genericTypeDefinition == typeof(IDictionary<,>)
               || genericTypeDefinition == typeof(ISet<>);
    }
}
