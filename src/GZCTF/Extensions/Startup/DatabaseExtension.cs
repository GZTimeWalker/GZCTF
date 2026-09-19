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

            var connectionString = builder.Configuration.GetConnectionString("Database");

            builder.Services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseNpgsql(connectionString,
                        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

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
                    options.UseNpgsql(connectionString);
                });
            }
            catch (Exception exception)
            {
                var diagnostic = DatabaseConnectionDiagnostic.Parse(connectionString);
                Log.Logger.Error(
                    "Database configuration failed for {DatabaseHost}/{DatabaseName} ({FailureType})",
                    diagnostic.Host,
                    diagnostic.Database,
                    exception.GetType().Name);

                ExitWithFatalMessage(
                    StaticLocalizer[nameof(Resources.Program.Database_ConnectionFailed),
                        exception.GetType().Name]);
            }
        }
    }
}
