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

            builder.Services.AddDbContext<AppDbContext>(options =>
                {
                    // Single query by default: one statement is inherently consistent, so a
                    // concurrent write cannot make EF 11 throw DbQueryConcurrencyException.
                    // Where a query must split (multiple or large collections), call
                    // AsSplitQuery() explicitly and run it inside a repeatable-read
                    // transaction so all of its statements share one snapshot.
                    options.UseNpgsql(builder.Configuration.GetConnectionString("Database"),
                        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));

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
                    options.UseNpgsql(builder.Configuration.GetConnectionString("Database"));
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
