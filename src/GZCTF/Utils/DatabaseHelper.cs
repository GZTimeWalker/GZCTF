using GZCTF.Extensions;
using GZCTF.Models.Context;
using GZCTF.Models.Internal;
using GZCTF.Providers;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GZCTF.Utils;

public static class DatabaseHelper
{
    public static void ConfigureDatabase(this WebApplicationBuilder builder)
    {
        var dbType = builder.Configuration.GetValue("DatabaseProvider", DatabaseProviderType.PostgreSQL);

        if (dbType != DatabaseProviderType.SQLite &&
                !builder.Configuration.GetSection("ConnectionStrings").GetSection("Database").Exists())
            Program.ExitWithFatalMessage(
                Program.StaticLocalizer[nameof(Resources.Program.Database_NoConnectionString)]);

        var optionsAction = new Action<DbContextOptionsBuilder>(options =>
        {
            if (!builder.Environment.IsDevelopment())
                return;

            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        if (dbType == DatabaseProviderType.SQLite)
            builder.Services.AddDbContext<AppDbContext, SqliteDbContext>(optionsAction);
        else
            builder.Services.AddDbContext<AppDbContext>(optionsAction);

        try
        {
            builder.Configuration.AddEntityConfiguration(new(builder.Services.BuildServiceProvider().GetRequiredService<AppDbContext>));
        }
        catch (Exception e)
        {
            if (dbType != DatabaseProviderType.SQLite &&
                builder.Configuration.GetSection("ConnectionStrings").GetSection("Database").Exists())
                Log.Logger.Error(Program.StaticLocalizer[
                    nameof(Resources.Program.Database_CurrentConnectionString),
                    builder.Configuration.GetConnectionString("Database") ?? "null"]);
            Program.ExitWithFatalMessage(
                Program.StaticLocalizer[nameof(Resources.Program.Database_ConnectionFailed), e.Message]);
        }
    }
}
