using System.Security.Cryptography;
using System.Text;
using GZCTF.Models.Internal;
using GZCTF.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GZCTF.Providers;

public class EntityConfigurationProvider(EntityConfigurationSource source) : ConfigurationProvider, IDisposable
{
    readonly CancellationTokenSource _cancellationTokenSource = new();
    Task? _databaseWatcher;
    bool _disposed;
    byte[] _lastHash = [];

    public void Dispose()
    {
        if (_disposed)
            return;

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    static HashSet<Config> DefaultConfigs()
    {
        HashSet<Config> configs = [];

        configs.UnionWith(ConfigService.GetConfigs(new AccountPolicy()));
        configs.UnionWith(ConfigService.GetConfigs(new GlobalConfig()));
        configs.UnionWith(ConfigService.GetConfigs(new ContainerPolicy()));

        return configs;
    }

    async Task WatchDatabase(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(source.PollingInterval, token);
                Dictionary<string, string?> actualData = await GetDataAsync(token);

                var computedHash = ConfigHash(actualData);
                if (!computedHash.SequenceEqual(_lastHash))
                {
                    Data = actualData;
                    OnReload();
                }

                _lastHash = computedHash;
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, Program.StaticLocalizer[nameof(Resources.Program.Config_ReloadFailed)]);
            }
        }
    }

    AppDbContext CreateAppDbContext()
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        source.OptionsAction(builder);

        return new AppDbContext(builder.Options);
    }

    async Task<Dictionary<string, string?>> GetDataAsync(CancellationToken token = default)
    {
        AppDbContext context = CreateAppDbContext();
        return await context.Configs.ToDictionaryAsync(c => c.ConfigKey, c => c.Value,
            StringComparer.OrdinalIgnoreCase, token);
    }

    static byte[] ConfigHash(IDictionary<string, string?> configs) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Join(";", configs.Select(c => $"{c.Key}={c.Value}"))
        ));

    public override void Load()
    {
        if (_databaseWatcher is not null)
        {
            Data = GetDataAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            _lastHash = ConfigHash(Data);
            return;
        }

        AppDbContext context = CreateAppDbContext();

        if (!context.Database.IsInMemory() && context.Database.GetMigrations().Any())
            context.Database.Migrate();

        context.Database.EnsureCreated();

        if (!context.Configs.Any())
        {
            Log.Logger.Debug(Program.StaticLocalizer[nameof(Resources.Program.Config_InitializingDatabase)]);

            HashSet<Config> configs = DefaultConfigs();

            context.Configs.AddRange(configs);
            context.SaveChanges();

            Data = configs.ToDictionary(c => c.ConfigKey, c => c.Value, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            Data = context.Configs.ToDictionary(c => c.ConfigKey, c => c.Value, StringComparer.OrdinalIgnoreCase);
        }

        _lastHash = ConfigHash(Data);

        CancellationToken cancellationToken = _cancellationTokenSource.Token;
        _databaseWatcher = Task.Run(() => WatchDatabase(cancellationToken), cancellationToken);
    }
}
