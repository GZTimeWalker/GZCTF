using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CTFServer.Extensions;
using CTFServer.Hubs;
using CTFServer.Middlewares;
using CTFServer.Models;
using CTFServer.Repositories;
using CTFServer.Repositories.Interface;
using CTFServer.Services;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using NJsonSchema.Generation;
using NLog;
using NLog.Web;
using NLog.Targets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Mvc;

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
builder.Services.AddSingleton<SignalRLoggingService>();

#endregion SignalR

#region Logging

builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
}).UseNLog();

Target.Register<SignalRTarget>("SignalR");
LogManager.Configuration.Variables["connectionString"] = builder.Configuration.GetConnectionString("DefaultConnection");

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

builder.Services.AddTransient<IMailSender, MailSender>();
builder.Services.AddSingleton<IRecaptchaExtension, RecaptchaExtension>();
builder.Services.AddSingleton<IContainerService, ContainerService>();

builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();

#endregion Services and Repositories

builder.Services.AddResponseCompression(options =>
{
    options.MimeTypes =
                ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/json" });
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

using (var serviceScope = app.Services.GetService<IServiceScopeFactory>()?.CreateScope())
{
    serviceScope?.ServiceProvider.GetService<AppDbContext>()?.Database.Migrate();
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

var logger = LogManager.GetLogger("Main");

try
{
    LogHelper.SystemLog(logger, "服务器初始化。");
    await app.RunAsync();
}
catch (Exception exception)
{
    logger.Error(exception, "因异常，应用程序意外停止。");
    throw;
}
finally
{
    LogManager.Shutdown();
}
