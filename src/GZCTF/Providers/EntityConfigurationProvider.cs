using System.Security.Cryptography;
using System.Text;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GZCTF.Providers;

public class EntityConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly EntityConfigurationSource _source;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _databaseWatcher;
    private byte[] _lastHash;
    private bool _disposed = false;

    public EntityConfigurationProvider(EntityConfigurationSource source)
    {
        _source = source;
        _lastHash = Array.Empty<byte>();
        _cancellationTokenSource = new();
    }

    private static HashSet<Config> DefaultConfigs()
    {
        HashSet<Config> configs = new();

        configs.UnionWith(ConfigService.GetConfigs(new AccountPolicy()));
        configs.UnionWith(ConfigService.GetConfigs(new GlobalConfig()));
        configs.UnionWith(ConfigService.GetConfigs(new GamePolicy()));

        return configs;
    }

    private async Task WatchDatabase(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_source.PollingInterval, token);
                IDictionary<string, string?> actualData = await GetDataAsync(token);

                byte[] computedHash = ConfigHash(actualData);
                if (!computedHash.SequenceEqual(_lastHash))
                {
                    Data = actualData;
                    OnReload();
                }
                _lastHash = computedHash;
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
        _source.OptionsAction(builder);

        return new AppDbContext(builder.Options);
    }

    private async Task<IDictionary<string, string?>> GetDataAsync(CancellationToken token = default)
    {
        var context = CreateAppDbContext();
        return await context.Configs.ToDictionaryAsync(c => c.ConfigKey, c => c.Value,
            StringComparer.OrdinalIgnoreCase, token);
    }

    private static byte[] ConfigHash(IDictionary<string, string?> configs)
        => SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(";", configs.Select(c => $"{c.Key}={c.Value}"))));

    public override void Load()
    {
        if (_databaseWatcher is not null)
        {
            var task = GetDataAsync();
            task.Wait();
            Data = task.Result;

            _lastHash = ConfigHash(Data);
            return;
        }

        var context = CreateAppDbContext();

        if (!context.Database.IsInMemory())
            context.Database.Migrate();

        context.Database.EnsureCreated();

        if (context is null || !context.Configs.Any())
        {
            Log.Logger.Debug("初始化数据库……");

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

        _lastHash = ConfigHash(Data);

        var cancellationToken = _cancellationTokenSource.Token;
        _databaseWatcher = Task.Run(() => WatchDatabase(cancellationToken), cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
