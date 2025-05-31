using Microsoft.EntityFrameworkCore;

namespace GZCTF.Providers;

public class EntityConfigurationSource(Action<DbContextOptionsBuilder> optionsAction, int pollingInterval = 300000)
    : IConfigurationSource
{
    public Action<DbContextOptionsBuilder> OptionsAction { get; init; } = optionsAction;
    public int PollingInterval { get; private set; } = pollingInterval; // default to 5min

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new EntityConfigurationProvider(this);
}
