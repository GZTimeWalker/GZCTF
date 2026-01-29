using GZCTF.Models.Internal;
using GZCTF.Services.Cache;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace GZCTF.Utils;

public static class PrelaunchHelper
{
    extension(WebApplication app)
    {
        public async Task RunPrelaunchWorkAsync()
        {
            using var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

            var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cache = serviceScope.ServiceProvider.GetRequiredService<IDistributedCache>();

            if (app.Configuration["xorKey"] is not { Length: > 0 })
                ExitWithFatalMessage(StaticLocalizer[nameof(Resources.Program.Init_XorKeyNotSet)]);

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

            if (!await context.SuspicionRules.AnyAsync())
            {
                var rules = new List<SuspicionRule>
                {
                    new() { RuleCode = "StolenFlag", Weight = 100, Description = "Flag stolen from another team" },
                    new() { RuleCode = "SharedIP", Weight = 50, Description = "Multiple team members using same IP" },
                    new() { RuleCode = "UnknownIP", Weight = 20, Description = "Using IP not seen in game before" },
                    new() { RuleCode = "CrossTeamIP", Weight = 40, Description = "IP used by members from multiple teams" },
                    new() { RuleCode = "TokenAbuse", Weight = 60, Description = "Multiple people using same submission token" },
                    new() { RuleCode = "Hoarding", Weight = 30, Description = "Solved challenge long after container destroy" },
                    new() { RuleCode = "Burst", Weight = 40, Description = "Multiple challenges solved in a very short time" },
                    new() { RuleCode = "NoDownload", Weight = 30, Description = "Solved without downloading attachment" },
                    new() { RuleCode = "NoContainer", Weight = 60, Description = "Solved without starting container" },
                    new() { RuleCode = "FastSolve-Open", Weight = 50, Description = "Solved very quickly after opening challenge" },
                    new() { RuleCode = "FastSolve-Download", Weight = 50, Description = "Solved very quickly after downloading attachment" },
                    new() { RuleCode = "FastSolve-Container", Weight = 50, Description = "Solved very quickly after starting container" },
                    new() { RuleCode = "SequenceSimilarity", Weight = 40, Description = "High similarity in solve order and timing" },
                    new() { RuleCode = "Corroboration", Weight = 20, Description = "Multiple suspicion indicators detected" }
                };

                await context.SuspicionRules.AddRangeAsync(rules);
                await context.SaveChangesAsync();
            }
        }
    }

    extension(IDistributedCache cache)
    {
        private bool CacheCheck(ILogger<Program> logger)
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
}
