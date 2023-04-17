using CTFServer.Models.Request.Admin;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class LogRepository : RepositoryBase, ILogRepository
{
    public LogRepository(AppDbContext _context) : base(_context)
    {
    }

    public Task<LogMessageModel[]> GetLogs(int skip, int count, string? level, CancellationToken token)
    {
        IQueryable<LogModel> data = context.Logs;

        if (level is not null && level != "All")
            data = data.Where(x => x.Level == level);
        data = data.OrderByDescending(x => x.TimeUTC).Skip(skip).Take(count);

        return (from log in data select LogMessageModel.FromLogModel(log)).ToArrayAsync(token);
    }
}