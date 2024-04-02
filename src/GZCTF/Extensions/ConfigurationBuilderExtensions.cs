using GZCTF.Providers;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Extensions;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddEntityConfiguration(this IConfigurationBuilder builder,
        Func<AppDbContext> dbBuilder) =>
        builder.Add(new EntityConfigurationSource(dbBuilder));
}
