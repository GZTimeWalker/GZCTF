using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GZCTF.Extensions.Startup;

static class DatabaseExtension
{
    internal static void ConfigureDatabase(this WebApplicationBuilder builder)
    {
        if (!builder.Configuration.GetSection("ConnectionStrings").GetSection("Database").Exists())
            ExitWithFatalMessage(
                StaticLocalizer[nameof(Resources.Program.Database_NoConnectionString)]);

        builder.Services.AddDbContext<AppDbContext>(
            options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("Database"),
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
