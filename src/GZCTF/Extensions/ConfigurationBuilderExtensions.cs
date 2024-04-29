using GZCTF.Providers;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Extensions;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddEntityConfiguration(this IConfigurationBuilder builder,
        Action<DbContextOptionsBuilder> optionsAction) =>
        builder.Add(new EntityConfigurationSource(optionsAction));
}
