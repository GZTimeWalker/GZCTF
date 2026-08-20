using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GZCTF.Extensions.Startup;

internal static class DatabaseExtension
{
    extension(WebApplicationBuilder builder)
    {
        internal void ConfigureDatabase()
        {
            if (!builder.Configuration.GetSection("ConnectionStrings").GetSection("Database").Exists())
                ExitWithFatalMessage(
                    StaticLocalizer[nameof(Resources.Program.Database_NoConnectionString)]);

            var dbProvider = builder.Configuration.GetValue<string>("DbProvider") ?? "PostgreSQL";

            builder.Services.AddDbContext<AppDbContext>(options =>
                {
                    if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                    {
                        options.UseSqlite(builder.Configuration.GetConnectionString("Database"),
                            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
                    }
                    else
                    {
                        options.UseNpgsql(builder.Configuration.GetConnectionString("Database"),
                            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
                    }

                    if (!builder.Environment.IsDevelopment())
                        return;

                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            );

            try
            {
                builder.Configuration.AddEntityConfiguration(options =>
                {
                    if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                    {
                        options.UseSqlite(builder.Configuration.GetConnectionString("Database"));
                    }
                    else
                    {
                        options.UseNpgsql(builder.Configuration.GetConnectionString("Database"));
                    }
                });
            }
            catch (Exception e)
            {
                if (builder.Configuration.GetSection("ConnectionStrings").GetSection("Database").Exists())
                    Log.Logger.Error(StaticLocalizer[
                        nameof(Resources.Program.Database_CurrentConnectionString),
                        builder.Configuration.GetConnectionString("Database") ?? "null"]);
                ExitWithFatalMessage(
                    StaticLocalizer[nameof(Resources.Program.Database_ConnectionFailed), e.Message]);
            }
        }
    }
}
