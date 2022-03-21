using Microsoft.EntityFrameworkCore;
using CTFServer.Models;
using CTFServer.Models.Request.Admin;
using CTFServer.Repositories.Interface;

namespace CTFServer.Repositories;

public class LogRepository : RepositoryBase, ILogRepository
{
    public LogRepository(AppDbContext context) : base(context) { }

    public Task<List<LogMessageModel>> GetLogs(int skip, int count, string? level, CancellationToken token)
    {
        IQueryable<LogModel> data = context.Logs;
        if (level is not null && level != "All")
            data = data.Where(x => x.Level == level);
        data = data.OrderByDescending(x => x.TimeUTC).Skip(skip).Take(count);

        return (from log in data select LogMessageModel.FromLogModel(log)).ToListAsync(token);
    }
}
