using System;
using CTFServer.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Providers;

public class EntityConfigurationProvider : ConfigurationProvider, IDisposable
{
    private bool _disposed;
    private readonly EntityConfigurationSource source;

    private CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();

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

    public override async void Load()
    {
        var token = TokenSource.Token;
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        source.OptionsAction(builder);

        var context = new AppDbContext(builder.Options);

        if (context.Database.IsRelational())
            await context.Database.MigrateAsync();

        await context.Database.EnsureCreatedAsync();

        if (context is null || !context.Configs.Any())
        {
            Data = DefaultConfigs;

            if (context is not null)
            {
                await context.Configs.AddRangeAsync(Data.Select(kvp => new Config(kvp.Key, kvp.Value)).ToArray(), token);
                await context.SaveChangesAsync(token);
            }
            return;
        }

        Data = await context.Configs.ToDictionaryAsync(c => c.ConfigKey, c => c.Value, StringComparer.OrdinalIgnoreCase);
    }

    public override async void Set(string key, string value)
    {
        var token = TokenSource.Token;
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        source.OptionsAction(builder);

        var context = new AppDbContext(builder.Options);

        if (context is null)
            return;

        var config = await context.Configs.FirstOrDefaultAsync(c => c.ConfigKey == key, token);
        if (config is null)
            return;

        config.Value = value;
        await context.SaveChangesAsync(token);

        base.Set(key, value);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        TokenSource.Cancel();
        TokenSource.Dispose();
        _disposed = true;
    }
}

