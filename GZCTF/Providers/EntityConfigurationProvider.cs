using System;
using CTFServer.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Providers;

public class EntityConfigurationProvider : ConfigurationProvider
{
    private readonly EntityConfigurationSource source;

    public EntityConfigurationProvider(EntityConfigurationSource _source)
    {
        source = _source;
    }

    private static IDictionary<string, string> DefaultConfigs
        => new Dictionary<string, string>
        {
            ["AccountPolicy:EmailConfirmationRequired"] = bool.TrueString,
            ["AccountPolicy:ActiveOnRegister"] = bool.FalseString,
            ["AccountPolicy:UseGoogleRecaptcha"] = bool.FalseString,
        };

    public override void Load()
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        source.OptionsAction(builder);

        var context = new AppDbContext(builder.Options);

        if (context.Database.IsRelational())
            context.Database.Migrate();

        context.Database.EnsureCreated();

        if (context is null || !context.Configs.Any())
        {
            Data = DefaultConfigs;

            if (context is not null)
            {
                context.Configs.AddRange(Data.Select(kvp => new Config(kvp.Key, kvp.Value)).ToArray());
                context.SaveChanges();
            }
            return;
        }

        Data = context.Configs.ToDictionary(c => c.ConfigKey, c => c.Value, StringComparer.OrdinalIgnoreCase);
    }
}

