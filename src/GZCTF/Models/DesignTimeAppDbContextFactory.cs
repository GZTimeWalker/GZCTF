using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GZCTF.Models;

public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseNpgsql(o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

        return new AppDbContext(builder.Options);
    }
}
