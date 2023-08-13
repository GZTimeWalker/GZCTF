using GZCTF.Models.Request.Admin;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class LogRepository : RepositoryBase, ILogRepository
{
    public LogRepository(AppDbContext context) : base(context)
    {
    }

    public Task<LogMessageModel[]> GetLogs(int skip, int count, string? level, CancellationToken token)
    {
        IQueryable<LogModel> data = _context.Logs;

        if (level is not null && level != "All")
            data = data.Where(x => x.Level == level);
        data = data.OrderByDescending(x => x.TimeUTC).Skip(skip).Take(count);

        return (from log in data select LogMessageModel.FromLogModel(log)).ToArrayAsync(token);
    }
}
