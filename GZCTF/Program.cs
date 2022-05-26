global using CTFServer.Models;
using AspNetCoreRateLimit;
using CTFServer.Extensions;
using CTFServer.Hubs;
using CTFServer.Middlewares;
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

#region Configuration

builder.Host.ConfigureAppConfiguration((host, config) =>
{
    config.AddJsonFile("ratelimit.json", optional: true, reloadOnChange: true);
});

#endregion Configuration

#region SignalR

builder.Services.AddSignalR().AddJsonProtocol();

#endregion SignalR

#region Logging

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Host.UseSerilog(dispose: true);

#endregion Logging

#region AppDbContext

if (builder.Environment.IsDevelopment() && !builder.Configuration.GetSection("ConnectionStrings").Exists())
{
    builder.Services.AddDbContext<AppDbContext>(
        options => options.UseInMemoryDatabase("TestDb")
    );
}
else
{
    builder.Services.AddDbContext<AppDbContext>(
        options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")
    ));
}

#endregion AppDbContext

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

//从appsettings.json获取相应配置
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));

//注入计数器和规则存储
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

#endregion IP Rate Limit

#region Services and Repositories

builder.Services.AddTransient<IMailSender, MailSender>()
    .Configure<EmailOptions>(options => builder.Configuration.GetSection("EmailConfig").Bind(options));

builder.Services.AddSingleton<IRecaptchaExtension, RecaptchaExtension>()
    .Configure<RecaptchaOptions>(options => builder.Configuration.GetSection("GoogleRecaptcha").Bind(options));

builder.Services.AddSingleton<IContainerService, DockerService>()
    .Configure<DockerOptions>(options => builder.Configuration.GetSection("DockerConfig").Bind(options));

builder.Services.AddScoped<IContainerRepository, ContainerRepository>();
builder.Services.AddScoped<IChallengeRepository, ChallengeRepository>();
builder.Services.AddScoped<IGameNoticeRepository, GameNoticeRepository>();
builder.Services.AddScoped<IGameEventRepository, GameEventRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IInstanceRepository, InstanceRepository>();
builder.Services.AddScoped<INoticeRepository, NoticeRepository>();
builder.Services.AddScoped<IParticipationRepository, ParticipationRepository>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();

builder.Services.AddChannel<Submission>();
builder.Services.AddHostedService<FlagChecker>();
builder.Services.AddHostedService<ContainerChecker>();

#endregion Services and Repositories

builder.Services.AddResponseCompression(options =>
{
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});

builder.Services.AddControllersWithViews().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errmsg = context.ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault()?.ErrorMessage;
        return new JsonResult(new RequestResponse(errmsg ?? "验证失败"))
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

    if (!await context.Notices.AnyAsync())
    {
        await context.Notices.AddAsync(new()
        {
            PublishTimeUTC = DateTimeOffset.UtcNow,
            Title = "Welcome to GZ::CTF!",
            Content = "一个开源的CTF比赛平台"
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
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "[{StatusCode}] @{Elapsed,8:####0.00}ms HTTP {RequestMethod,-6} {RequestPath}";
    options.GetLevel = (context, time, ex) =>
        time > 10000 ? LogEventLevel.Warning :
        (context.Response.StatusCode > 499 || ex is not null) ? LogEventLevel.Error : LogEventLevel.Debug;
});

app.UseSwaggerUi3();

app.UseMiddleware<ProxyMiddleware>();
app.UseIpRateLimiting();

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
    logger.SystemLog("服务器初始化");
    await app.RunAsync();
}
catch (Exception exception)
{
    logger.LogError(exception, "因异常，应用程序意外停止");
    throw;
}
finally
{
    Log.CloseAndFlush();
}