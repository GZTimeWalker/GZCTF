using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using CTFServer.Models.Data;
using IdentityModel.OidcClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NPOI.SS.Formula.Functions;

namespace CTFServer.Services;

public class ConfigService : IHostedService
{
    private readonly ILogger<ConfigService> logger;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private HashSet<Config> Data = new();

    public ConfigService(IServiceScopeFactory provider, ILogger<ConfigService> _logger)
    {
        serviceScopeFactory = provider;
        logger = _logger;
    }

    private static void GetConfigsInternal(string key, HashSet<Config> configs, Type? type, object? value)
    {
        if (value is null || type is null)
            return;

        if (type.IsArray || IsArrayLikeInterface(type))
            throw new InvalidOperationException("不支持的配置项类型");

        TypeConverter converter = TypeDescriptor.GetConverter(type);
        if (type == typeof(string) || type.IsValueType)
        {
            configs.Add(new(key, converter.ConvertToString(value) ?? String.Empty));
        }
        else if (type.IsClass)
        {
            foreach (var item in type.GetProperties())
                GetConfigsInternal($"{key}:{item.Name}", configs, item.PropertyType, item.GetValue(value));
        }
    }

    public static HashSet<Config> GetConfigs<T>(T config) where T : class
    {
        HashSet<Config> configs = new();
        var type = typeof(T);

        foreach (var item in type.GetProperties())
            GetConfigsInternal($"{type.Name}:{item.Name}", configs, item.PropertyType, item.GetValue(config));

        return configs;
    }

    public void SaveConfig<T>(T config) where T : class
    {
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var configs = await context.Configs.AsNoTracking().ToListAsync(cancellationToken);
        Data = configs.ToHashSet();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static bool IsArrayLikeInterface(Type type)
    {
        if (!type.IsInterface || !type.IsConstructedGenericType) { return false; }

        Type genericTypeDefinition = type.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(IEnumerable<>)
            || genericTypeDefinition == typeof(ICollection<>)
            || genericTypeDefinition == typeof(IList<>)
            || genericTypeDefinition == typeof(IDictionary<,>)
            || genericTypeDefinition == typeof(ISet<>);
    }
}