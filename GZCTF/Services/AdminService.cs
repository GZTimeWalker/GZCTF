using CTFServer.Models.Request.Admin;
using CTFServer.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Services;

public class AdminService : IAdminService
{
    private readonly ILogger<AdminService> logger;
    protected readonly AppDbContext context;

    public AdminService(AppDbContext _context, ILogger<AdminService> _logger)
    {
        context = _context;
        logger = _logger;
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
