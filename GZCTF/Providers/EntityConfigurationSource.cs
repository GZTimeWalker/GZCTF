using Microsoft.EntityFrameworkCore;

namespace CTFServer.Providers;

public class EntityConfigurationSource : IConfigurationSource
{
    public EntityConfigurationSource(Action<DbContextOptionsBuilder> _optionsAction)
    {
        OptionsAction = _optionsAction;
    }

    public Action<DbContextOptionsBuilder> OptionsAction { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new EntityConfigurationProvider(this);
}