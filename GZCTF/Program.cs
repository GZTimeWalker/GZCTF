global using CTFServer.Models;
using AspNetCoreRateLimit;
using CTFServer.Extensions;
using CTFServer.Hubs;
using CTFServer.Middlewares;
using CTFServer.Models.Internal;
using CTFServer.Repositories;
using CTFServer.Repositories.Interface;
using CTFServer.Services;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Generation;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#region Directory

var uploadPath = Path.Combine(builder.Configuration.GetSection("UploadFolder").Value ?? "uploads");

if (!Directory.Exists(uploadPath))
    Directory.CreateDirectory(uploadPath);

#endregion Directory

#region Logging

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Host.UseSerilog(dispose: true);

#endregion Logging

#region AppDbContext
if (IsTesting || (builder.Environment.IsDevelopment() && !builder.Configuration.GetSection("ConnectionStrings").Exists()))
{
    builder.Services.AddDbContext<AppDbContext>(
        options => options.UseInMemoryDatabase("TestDb")
    );
}
else
{
    builder.Services.AddDbContext<AppDbContext>(
        options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            if (builder.Environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        }
    );
}
#endregion AppDbContext

#region Configuration
if (!IsTesting)
{
    builder.Host.ConfigureAppConfiguration((host, config) =>
    {
        config.AddJsonFile("ratelimit.json", optional: true, reloadOnChange: true);
        config.AddEntityConfiguration(options =>
        {
            if (builder.Configuration.GetSection("ConnectionStrings").Exists())
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
            else
                options.UseInMemoryDatabase("TestDb");
        });
    });
}
#endregion Configuration

#region SignalR

builder.Services.AddSignalR().AddJsonProtocol();

#endregion SignalR

#region OpenApiDocument

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddOpenApiDocument(settings =>
{
    settings.DocumentName = "v1";
    settings.Version = "v1";
    settings.Title = "GZCTF Server API";
    settings.Description = "GZCTF Server 接口文档";
    settings.UseControllerSummaryAsTagDescription = true;
    settings.SerializerSettings = SystemTextJsonUtilities.ConvertJsonOptionsToNewtonsoftSettings(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
    settings.DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
});

#endregion OpenApiDocument

#region MemoryCache

builder.Services.AddMemoryCache();

#endregion MemoryCache

#region Identity

builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = IdentityConstants.ApplicationScheme;
    o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies(options =>
{
    options.ApplicationCookie.Configure(cookie =>
    {
        cookie.Cookie.Name = "GZCTF_Token";
    });
});

builder.Services.AddIdentityCore<UserInfo>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedEmail = true;
}).AddSignInManager<SignInManager<UserInfo>>()
.AddUserManager<UserManager<UserInfo>>()
.AddEntityFrameworkStores<AppDbContext>()
.AddErrorDescriber<TranslatedIdentityErrorDescriber>()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
    o.TokenLifespan = TimeSpan.FromHours(3));

#endregion Identity

#region IP Rate Limit
if (!IsTesting)
{
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));

    builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
    builder.Services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
    builder.Services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
}
#endregion IP Rate Limit

#region Services and Repositories

builder.Services.AddTransient<IMailSender, MailSender>()
    .Configure<EmailConfig>(builder.Configuration.GetSection(nameof(EmailConfig)));

builder.Services.AddSingleton<IRecaptchaExtension, RecaptchaExtension>()
    .Configure<RecaptchaConfig>(builder.Configuration.GetSection("GoogleRecaptcha"));

builder.Services.Configure<RegistryConfig>(builder.Configuration.GetSection(nameof(RegistryConfig)));
builder.Services.Configure<AccountPolicy>(builder.Configuration.GetSection(nameof(AccountPolicy)));
builder.Services.Configure<GlobalConfig>(builder.Configuration.GetSection(nameof(GlobalConfig)));


if (builder.Configuration.GetSection("ContainerProvider").Value == "K8s")
{
    builder.Services.AddSingleton<IContainerService, K8sService>();
}
else
{
    builder.Services.AddSingleton<IContainerService, DockerService>()
        .Configure<DockerConfig>(builder.Configuration.GetSection(nameof(DockerConfig)));
}

builder.Services.AddScoped<IContainerRepository, ContainerRepository>();
builder.Services.AddScoped<IChallengeRepository, ChallengeRepository>();
builder.Services.AddScoped<IGameNoticeRepository, GameNoticeRepository>();
builder.Services.AddScoped<IGameEventRepository, GameEventRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IInstanceRepository, InstanceRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IParticipationRepository, ParticipationRepository>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IConfigService, ConfigService>();

builder.Services.AddChannel<Submission>();
builder.Services.AddHostedService<FlagChecker>();
builder.Services.AddHostedService<ContainerChecker>();

#endregion Services and Repositories

builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
        { "application/json", "text/javascript", "text/html", "text/css" }
    );
});

builder.Services.AddControllersWithViews().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errmsg = context.ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault()?.ErrorMessage;
        return new JsonResult(new RequestResponse(errmsg ?? "验证失败，请检查输入。"))
        {
            StatusCode = 400
        };
    };
});

var app = builder.Build();

Log.Logger = LogHelper.GetLogger(app.Configuration, app.Services);

using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (context.Database.IsRelational())
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

    // only for testing
    if (app.Environment.IsDevelopment())
    {
        var usermanager = serviceScope.ServiceProvider.GetRequiredService<UserManager<UserInfo>>();
        var admin = await usermanager.FindByNameAsync("Admin");
        if (admin is null)
        {
            admin = new UserInfo
            {
                UserName = "Admin",
                Email = "admin@gzti.me",
                Role = CTFServer.Role.Admin,
                EmailConfirmed = true,
                RegisterTimeUTC = DateTimeOffset.UtcNow
            };
            await usermanager.CreateAsync(admin, "Admin@2022");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseOpenApi(options => { options.PostProcess += (document, _) => { document.Servers.Clear(); }; });
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "[{StatusCode}] @{Elapsed,8:####0.00}ms HTTP {RequestMethod,-6} {RequestPath}";
        options.GetLevel = (context, time, ex) =>
            time > 10000 && context.Response.StatusCode != 101 ? LogEventLevel.Warning :
            (context.Response.StatusCode > 499 || ex is not null) ? LogEventLevel.Error : LogEventLevel.Debug;
    });
    app.UseSwaggerUi3();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseMiddleware<ProxyMiddleware>();

if (!IsTesting)
{
    app.UseIpRateLimiting();
}

app.UseStaticFiles();

app.UseResponseCompression();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<UserHub>("/hub/user");
    endpoints.MapHub<MonitorHub>("/hub/monitor");
    endpoints.MapHub<AdminHub>("/hub/admin");
    endpoints.MapFallbackToFile("index.html");
});

await using var scope = app.Services.CreateAsyncScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    logger.SystemLog("服务器初始化", CTFServer.TaskStatus.Pending, LogLevel.Debug);
    await app.RunAsync();
}
catch (Exception exception)
{
    logger.LogError(exception, "因异常，应用程序意外停止");
    throw;
}
finally
{
    logger.SystemLog("服务器已退出", CTFServer.TaskStatus.Exit, LogLevel.Debug);
    Log.CloseAndFlush();
}

public partial class Program
{
    public static bool IsTesting { get; set; }
}