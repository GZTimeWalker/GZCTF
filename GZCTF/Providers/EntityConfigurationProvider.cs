using System;
using CTFServer.Models.Data;
using CTFServer.Models.Internal;
using CTFServer.Services;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Providers;

public class EntityConfigurationProvider : ConfigurationProvider
{
    private readonly EntityConfigurationSource source;

    public EntityConfigurationProvider(EntityConfigurationSource _source)
    {
        source = _source;
    }

    private HashSet<Config> DefaultConfigs()
    {
        HashSet<Config> configs = new();

        configs.UnionWith(ConfigService.GetConfigs(new AccountPolicy()));

        return configs;
    }

    public override void Load()
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        source.OptionsAction(builder);

        var context = new AppDbContext(builder.Options);

        context.Database.EnsureCreated();

        if (context.Database.IsRelational())
            context.Database.Migrate();

        if (context is null || !context.Configs.Any())
        {
            var configs = DefaultConfigs();

            if (context is not null)
            {
                context.Configs.AddRange(configs);
                context.SaveChanges();
            }

            Data = configs.ToDictionary(c => c.ConfigKey, c => c.Value, StringComparer.OrdinalIgnoreCase);
            return;
        }

        Data = context.Configs.ToDictionary(c => c.ConfigKey, c => c.Value, StringComparer.OrdinalIgnoreCase);
    }
}