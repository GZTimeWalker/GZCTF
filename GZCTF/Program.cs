global using CTFServer.Models;

using System.Text;
using System.Text.Json;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CTFServer.Extensions;
using CTFServer.Hubs;
using CTFServer.Middlewares;
using CTFServer.Repositories;
using CTFServer.Repositories.Interface;
using CTFServer.Services;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using NJsonSchema.Generation;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;
using Serilog.Sinks.AspNetCore.SignalR.Extensions;
using CTFServer.Hubs.Client;
using Serilog.Sinks.File.Archive;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#region Directory

var uploadPath = Path.Combine(builder.Configuration.GetSection("UploadFolder").Value ?? "uploads");

if (!Directory.Exists(uploadPath))
    Directory.CreateDirectory(uploadPath);
#endregion

#region Configuration

builder.Host.ConfigureAppConfiguration((host, config) =>
{
    config.AddJsonFile("ratelimit.json", optional: true, reloadOnChange: true);
});

#endregion

#region SignalR

builder.Services.AddSignalR().AddJsonProtocol();

#endregion SignalR

#region Logging

const string LogTemplate = "[{@t:yy-MM-dd HH:mm:ss.fff} {@l:u3}] {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}: {@m} {#if Length(status) > 0}#{status} <{uname}>{#if Length(ip) > 0}@{ip}{#end}{#end}\n{@x}";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .WriteTo.Async(t => t.Console(
        formatter: new ExpressionTemplate(LogTemplate, theme: TemplateTheme.Literate),
        restrictedToMinimumLevel: LogEventLevel.Information
    ))
    .WriteTo.Async(t => t.File(
        path: "log/log_.log",
        formatter: new ExpressionTemplate(LogTemplate),
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        restrictedToMinimumLevel: LogEventLevel.Information,
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 5,
        hooks: new ArchiveHooks(CompressionLevel.Optimal, "log/archive/{UtcDate:yyyyMM}/{UtcDate:yyyy-MM-dd}")
    ))
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Host.UseSerilog(dispose: true);

//builder.Host.UseNLog(new() { RemoveLoggerFactoryFilter = true });
// Target.Register<SignalRTarget>("SignalR");
// NLog.LogManager.Configuration.Variables["connectionString"] = builder.Configuration.GetConnectionString("DefaultConnection");

#endregion

#region AppDbContext

builder.Services.AddDbContext<AppDbContext>(
    options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
    provideropt => provideropt.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null)));

#endregion

#region OpenApiDocument

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
}).AddIdentityCookies();

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

builder.Services.AddHostedService<ContainerChecker>();

builder.Services.AddTransient<IMailSender, MailSender>()
    .Configure<EmailOptions>(options => builder.Configuration.GetSection("EmailConfig").Bind(options));

builder.Services.AddSingleton<IRecaptchaExtension, RecaptchaExtension>()
    .Configure<RecaptchaOptions>(options => builder.Configuration.GetSection("GoogleRecaptcha").Bind(options));

builder.Services.AddSingleton<IContainerService, DockerService>()
    .Configure<DockerOptions>(options => builder.Configuration.GetSection("DockerConfig").Bind(options));

builder.Services.AddScoped<IContainerRepository, ContainerRepository>();
builder.Services.AddScoped<IChallengeRepository, ChallengeRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IInstanceRepository, InstanceRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<INoticeRepository, NoticeRepository>();
builder.Services.AddScoped<IParticipationRepository, ParticipationRepository>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();

#endregion Services and Repositories

builder.Services.AddResponseCompression(options =>
{
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat( new[] { "application/json" });
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

using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    await serviceScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseOpenApi();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();

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
    endpoints.MapHub<LoggingHub>("/hub/log");
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
