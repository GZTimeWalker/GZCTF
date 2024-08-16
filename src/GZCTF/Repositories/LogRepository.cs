using GZCTF.Models.Request.Admin;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class LogRepository(AppDbContext context) : RepositoryBase(context), ILogRepository
{
    public Task<LogMessageModel[]> GetLogs(int skip, int count, string? level, CancellationToken token)
    {
        IQueryable<LogModel> data = Context.Logs;

        if (level is not null && level != "All")
            data = data.Where(x => x.Level == level);
        data = data.OrderByDescending(x => x.TimeUtc).Skip(skip).Take(count);

        return (from log in data select LogMessageModel.FromLogModel(log)).ToArrayAsync(token);
    }
}
