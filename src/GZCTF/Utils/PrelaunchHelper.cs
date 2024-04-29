using GZCTF.Models.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace GZCTF.Utils;

public static class PrelaunchHelper
{
    public static async Task RunPrelaunchWork(this WebApplication app)
    {
        using IServiceScope serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

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
                UpdateTimeUtc = DateTimeOffset.UtcNow,
                Title = Program.StaticLocalizer[nameof(Resources.Program.Init_PostTitle)],
                Summary = Program.StaticLocalizer[nameof(Resources.Program.Init_PostSummary)],
                Content = Program.StaticLocalizer[nameof(Resources.Program.Init_PostContent)]
            });

            await context.SaveChangesAsync();
        }

        if (app.Environment.IsDevelopment() || app.Configuration.GetSection("ADMIN_PASSWORD").Exists())
        {
            var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<UserInfo>>();
            UserInfo? admin = await userManager.FindByNameAsync("Admin");
            var password = app.Environment.IsDevelopment()
                ? "Admin@2022"
                : app.Configuration.GetValue<string>("ADMIN_PASSWORD");

            if (admin is null && password is not null)
            {
                admin = new UserInfo
                {
                    UserName = "Admin",
                    Email = "admin@gzti.me",
                    Role = Role.Admin,
                    EmailConfirmed = true,
                    RegisterTimeUtc = DateTimeOffset.UtcNow
                };

                IdentityResult result = await userManager.CreateAsync(admin, password);
                if (!result.Succeeded)
                    logger.SystemLog(
                        Program.StaticLocalizer[nameof(Resources.Program.Init_AdminCreationFailed),
                            result.Errors.FirstOrDefault()?.Description ?? "null"], TaskStatus.Failed,
                        LogLevel.Debug);
            }
        }

        var containerConfig = serviceScope.ServiceProvider.GetRequiredService<IOptions<ContainerProvider>>();
        if (containerConfig.Value.EnableTrafficCapture &&
            containerConfig.Value.PortMappingType != ContainerPortMappingType.PlatformProxy)
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.Init_CaptureNotAvailable)],
                TaskStatus.Failed, LogLevel.Warning);

        if (!cache.CacheCheck())
            Program.ExitWithFatalMessage(Program.StaticLocalizer[nameof(Resources.Program.Init_InvalidCacheConfig)]);
    }

    static bool CacheCheck(this IDistributedCache cache)
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
