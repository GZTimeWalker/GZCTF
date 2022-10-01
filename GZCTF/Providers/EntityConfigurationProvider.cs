using System.Security.Cryptography;
using System.Text;
using CTFServer.Models.Data;
using CTFServer.Models.Internal;
using CTFServer.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CTFServer.Providers;

public class EntityConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly EntityConfigurationSource source;
    private readonly CancellationTokenSource cancellationTokenSource;
    private Task? databaseWatcher;
    private byte[] lastHash;
    private bool disposed = false;

    public EntityConfigurationProvider(EntityConfigurationSource _source)
    {
        source = _source;
        lastHash = Array.Empty<byte>();
        cancellationTokenSource = new();
    }

    private static HashSet<Config> DefaultConfigs()
    {
        HashSet<Config> configs = new();

        configs.UnionWith(ConfigService.GetConfigs(new AccountPolicy()));
        configs.UnionWith(ConfigService.GetConfigs(new GlobalConfig()));

        return configs;
    }

    private async Task WatchDatabase(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(source.PollingInterval, token);
                IDictionary<string, string> actualData = await GetDataAsync(token);

                byte[] computedHash = ConfigHash(actualData);
                if (!computedHash.SequenceEqual(lastHash))
                {
                    Data = actualData;
                    OnReload();
                }
                lastHash = computedHash;
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "全局配置重载失败");
            }
        }
    }

    private AppDbContext CreateAppDbContext()
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        source.OptionsAction(builder);

        return new AppDbContext(builder.Options);
    }

    private async Task<IDictionary<string, string>> GetDataAsync(CancellationToken token = default)
    {
        var context = CreateAppDbContext();
        return await context.Configs.ToDictionaryAsync(c => c.ConfigKey, c => c.Value,
            StringComparer.OrdinalIgnoreCase, token);
    }

    private byte[] ConfigHash(IDictionary<string, string> configs)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(
            Encoding.UTF8.GetBytes(string.Join(";", configs.Select(c => $"{c.Key}={c.Value}")))
        );
    }

    public override void Load()
    {
        if (databaseWatcher is not null)
        {
            var task = GetDataAsync();
            task.Wait();
            Data = task.Result;

            lastHash = ConfigHash(Data);
            return;
        }

        var context = CreateAppDbContext();

        if (context.Database.IsRelational())
            context.Database.Migrate();

        context.Database.EnsureCreated();

        if (context is null || !context.Configs.Any())
        {
            var configs = DefaultConfigs();

            if (context is not null)
            {
                context.Configs.AddRange(configs);
                context.SaveChanges();
            }

            Data = configs.ToDictionary(c => c.ConfigKey, c => c.Value, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            Data = context.Configs.ToDictionary(c => c.ConfigKey, c => c.Value, StringComparer.OrdinalIgnoreCase);
        }

        lastHash = ConfigHash(Data);

        var cancellationToken = cancellationTokenSource.Token;
        databaseWatcher = Task.Run(() => WatchDatabase(cancellationToken), cancellationToken);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        disposed = true;
    }
}
