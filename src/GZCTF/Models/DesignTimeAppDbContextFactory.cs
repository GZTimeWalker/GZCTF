using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GZCTF.Models;

public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        var dbProvider = configuration.GetValue<string>("DbProvider") ?? "PostgreSQL";
        var connectionString = configuration.GetConnectionString("Database");

        if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            builder.UseSqlite(connectionString,
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        }
        else
        {
            builder.UseNpgsql(connectionString,
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        }

        return new AppDbContext(builder.Options);
    }
}
