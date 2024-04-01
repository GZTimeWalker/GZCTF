using GZCTF.Extensions;
using GZCTF.Models.Internal;
using Microsoft.EntityFrameworkCore;
using Namotion.Reflection;
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

        Log.Logger.Debug(Program.StaticLocalizer[nameof(Resources.Program.Database_UsingProvider), dbType]);

        var optionsAction = new Action<DbContextOptionsBuilder>(options =>
        {
            if (dbType == DatabaseProviderType.SQLite)
                options.UseSqlite($"Data Source={FilePath.SQLite}/GZCTF.db",
                    x => x.MigrationsAssembly("GZCTF.Migrations.SQLite"));
            else
                options.UseNpgsql(builder.Configuration.GetConnectionString("Database"),
                    x => x.MigrationsAssembly("GZCTF.Migrations.PostgreSQL"));

            if (!builder.Environment.IsDevelopment())
                return;

            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        builder.Services.AddDbContext<AppDbContext>(optionsAction);

        try
        {
            builder.Configuration.AddEntityConfiguration(optionsAction);
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
