using Microsoft.EntityFrameworkCore;

namespace GZCTF.Providers;

public class EntityConfigurationSource(Action<DbContextOptionsBuilder> optionsAction, int pollingInterval = 180000)
    : IConfigurationSource
{
    public Action<DbContextOptionsBuilder> OptionsAction { get; init; } = optionsAction;
    public int PollingInterval { get; private set; } = pollingInterval; // default to 3min

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new EntityConfigurationProvider(this);
}
