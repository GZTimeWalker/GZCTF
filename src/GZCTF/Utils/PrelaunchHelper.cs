using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Utils;

public static class PrelaunchHelper
{
    public async static Task RunPrelaunchWork(this WebApplication app)
    {
        using var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cache = serviceScope.ServiceProvider.GetRequiredService<IDistributedCache>();

        if (!context.Database.IsInMemory())
            await context.Database.MigrateAsync();

        await context.Database.EnsureCreatedAsync();

        if (!await context.Posts.AnyAsync())
        {
            await context.Posts.AddAsync(new()
            {
                UpdateTimeUTC = DateTimeOffset.UtcNow,
                Title = "Welcome to GZ::CTF!",
                Summary = "一个开源的CTF比赛平台。",
                Content = "项目基于 AGPL-3.0 许可证，开源于 [GZTimeWalker/GZCTF](https://github.com/GZTimeWalker/GZCTF)。"
            });

            await context.SaveChangesAsync();
        }

        if (app.Environment.IsDevelopment() || app.Configuration.GetSection("ADMIN_PASSWORD").Exists())
        {
            var usermanager = serviceScope.ServiceProvider.GetRequiredService<UserManager<UserInfo>>();
            var admin = await usermanager.FindByNameAsync("Admin");
            var password = app.Environment.IsDevelopment() ? "Admin@2022" :
                app.Configuration.GetValue<string>("ADMIN_PASSWORD");

            if (admin is null && password is not null)
            {
                admin = new UserInfo
                {
                    UserName = "Admin",
                    Email = "admin@gzti.me",
                    Role = Role.Admin,
                    EmailConfirmed = true,
                    RegisterTimeUTC = DateTimeOffset.UtcNow
                };

                var result = await usermanager.CreateAsync(admin, password);
                if (!result.Succeeded)
                    logger.SystemLog($"管理员账户创建失败，错误信息：{result.Errors.FirstOrDefault()?.Description}", TaskStatus.Failed, LogLevel.Debug);
            }
        }

        if (!cache.CacheCheck())
            Program.ExitWithFatalMessage("缓存配置无效，请检查 RedisCache 字段配置。如不使用 Redis 请将配置项置空。");
    }

    public static bool CacheCheck(this IDistributedCache cache)
    {
        try
        {
            cache.SetString("_ValidCheck", "GZCTF");
            return cache.GetString("_ValidCheck") == "GZCTF";
        }
        catch
        {
            return false;
        }
    }
}
