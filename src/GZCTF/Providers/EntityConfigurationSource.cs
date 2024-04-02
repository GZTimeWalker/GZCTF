using Microsoft.EntityFrameworkCore;

namespace GZCTF.Providers;

public class EntityConfigurationSource(Func<AppDbContext> contextFunc, int pollingInterval = 180000)
    : IConfigurationSource
{
    public Func<AppDbContext> GetContext { get; init; } = contextFunc;
    public int PollingInterval { get; private set; } = pollingInterval; // default to 3min

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new EntityConfigurationProvider(this);
}
