using System;
using CTFServer.Providers;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Extensions;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddEntityConfiguration(this IConfigurationBuilder builder, Action<DbContextOptionsBuilder> optionsAction)
        => builder.Add(new EntityConfigurationSource(optionsAction));
}