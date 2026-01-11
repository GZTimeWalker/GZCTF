using GZCTF.Models.Request.Admin;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class LogRepository(AppDbContext context) : RepositoryBase(context), ILogRepository
{
    public Task<LogMessageModel[]> GetLogs(int skip, int count, string? level, string? search = null, CancellationToken token = default)
    {
        IQueryable<LogModel> data;

        // Use raw SQL to handle IP address search with CAST
        if (!string.IsNullOrWhiteSpace(search) && level != "All")
        {
            data = Context.Logs.FromSqlInterpolated($@"
                SELECT * FROM ""Logs""
                WHERE ""Level"" = {level}
                AND (""UserName"" ILIKE {"%"+ search + "%"} 
                     OR ""Message"" ILIKE {"%" + search + "%"}
                     OR CAST(""RemoteIP"" AS text) ILIKE {"%" + search + "%"})
                ");
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            data = Context.Logs.FromSqlInterpolated($@"
                SELECT * FROM ""Logs""
                WHERE ""UserName"" ILIKE {"%" + search + "%"} 
                   OR ""Message"" ILIKE {"%" + search + "%"}
                   OR CAST(""RemoteIP"" AS text) ILIKE {"%" + search + "%"}
                ");
        }
        else if (level != "All")
        {
            data = Context.Logs.Where(e => e.Level == level);
        }
        else
        {
            data = Context.Logs;
        }

        return (from log in data.OrderByDescending(e => e.TimeUtc).Skip(skip).Take(count) select LogMessageModel.FromLogModel(log)).ToArrayAsync(token);
    }
}
