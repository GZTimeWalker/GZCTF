using Microsoft.EntityFrameworkCore;

namespace GZCTF.Providers;

public class EntityConfigurationSource : IConfigurationSource
{
    public Action<DbContextOptionsBuilder> OptionsAction { get; set; }
    public int PollingInterval { get; private set; }

    public EntityConfigurationSource(Action<DbContextOptionsBuilder> optionsAction, int pollingInterval = 180000)
    {
        OptionsAction = optionsAction;
        PollingInterval = pollingInterval; // default to 3min
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new EntityConfigurationProvider(this);
}
