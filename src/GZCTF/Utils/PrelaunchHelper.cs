using GZCTF.Models.Internal;
using GZCTF.Services.Cache;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace GZCTF.Utils;

public static class PrelaunchHelper
{
    public static async Task RunPrelaunchWorkAsync(this WebApplication app)
    {
        using var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cache = serviceScope.ServiceProvider.GetRequiredService<IDistributedCache>();

        if (context.Database.GetMigrations().Any())
            await context.Database.MigrateAsync();

        await context.Database.EnsureCreatedAsync();

        if (!await context.Posts.AnyAsync())
        {
            await context.Posts.AddAsync(new()
            {
                UpdateTimeUtc = DateTimeOffset.UtcNow,
                Title = StaticLocalizer[nameof(Resources.Program.Init_PostTitle)],
                Summary = StaticLocalizer[nameof(Resources.Program.Init_PostSummary)],
                Content = StaticLocalizer[nameof(Resources.Program.Init_PostContent)]
            });

            await context.SaveChangesAsync();
        }

        if (app.Environment.IsDevelopment() || app.Configuration.GetSection("ADMIN_PASSWORD").Exists())
        {
            var userManager =
                serviceScope.ServiceProvider.GetRequiredService<UserManager<UserInfo>>();
            var admin = await userManager.FindByNameAsync("Admin");
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

                var result = await userManager.CreateAsync(admin, password);
                if (!result.Succeeded)
                    logger.SystemLog(
                        StaticLocalizer[nameof(Resources.Program.Init_AdminCreationFailed),
                            result.Errors.FirstOrDefault()?.Description ?? "null"], TaskStatus.Failed,
                        LogLevel.Debug);
            }
        }

        var containerConfig =
            serviceScope.ServiceProvider.GetRequiredService<IOptions<ContainerProvider>>();
        if (containerConfig.Value.EnableTrafficCapture &&
            containerConfig.Value.PortMappingType != ContainerPortMappingType.PlatformProxy)
            logger.SystemLog(StaticLocalizer[nameof(Resources.Program.Init_CaptureNotAvailable)],
                TaskStatus.Failed, LogLevel.Warning);

        if (!cache.CacheCheck(logger))
            ExitWithFatalMessage(StaticLocalizer[nameof(Resources.Program.Init_InvalidCacheConfig)]);

        await cache.RemoveAsync(CacheKey.Index);
        await cache.RemoveAsync(CacheKey.ClientConfig);
        await cache.RemoveAsync(CacheKey.CaptchaConfig);
    }

    static bool CacheCheck(this IDistributedCache cache, ILogger<Program> logger)
    {
        var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
        var cacheVersion = $"GZCTF@{version}";

        try
        {
            cache.SetString("_ValidCheck", cacheVersion);
            return cache.GetString("_ValidCheck") == cacheVersion;
        }
        catch (Exception e)
        {
            logger.LogErrorMessage(e, StaticLocalizer[nameof(Resources.Program.Init_InvalidCacheConfig)]);
            return false;
        }
    }
}
