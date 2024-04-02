
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Context;

public class SqliteDbContext : AppDbContext
{
    public SqliteDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration) : base(options, configuration) { }

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite($"Data Source={FilePath.SQLite}/GZCTF.db");
}
