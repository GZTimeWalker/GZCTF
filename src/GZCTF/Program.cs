global using GZCTF.Models;
using System.Reflection;
using System.Text;
using System.Text.Json;
using GZCTF.Extensions;
using GZCTF.Hubs;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Repositories;
using GZCTF.Repositories.Interface;
using GZCTF.Services;
using GZCTF.Services.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Generation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

Banner();

#region Directory

var uploadPath = Path.Combine(builder.Configuration.GetSection("UploadFolder").Value ?? "uploads");

if (!Directory.Exists(uploadPath))
    Directory.CreateDirectory(uploadPath);

#endregion Directory

#region Logging

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Host.UseSerilog(dispose: true);
builder.Configuration.AddEnvironmentVariables("GZCTF_");
Log.Logger = LogHelper.GetInitLogger();

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
    if (!builder.Configuration.GetSection("ConnectionStrings").GetSection("Database").Exists())
        ExitWithFatalMessage("未找到数据库连接字符串字段 ConnectionStrings，请检查 appsettings.json 是否正常挂载及配置");

    builder.Services.AddDbContext<AppDbContext>(
        options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("Database"),
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
    try
    {
        builder.Configuration.AddEntityConfiguration(options =>
        {
            if (builder.Configuration.GetSection("ConnectionStrings").GetSection("Database").Exists())
                options.UseNpgsql(builder.Configuration.GetConnectionString("Database"));
            else
                options.UseInMemoryDatabase("TestDb");
        });
    }
    catch
    {
        if (builder.Configuration.GetSection("ConnectionStrings").GetSection("Database").Exists())
            Log.Logger.Error($"当前连接字符串：{builder.Configuration.GetConnectionString("Database")}");
        ExitWithFatalMessage("数据库连接失败，请检查 Database 连接字符串配置");
    }
}
#endregion Configuration

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

#region SignalR

var signalrBuilder = builder.Services.AddSignalR().AddJsonProtocol();

#endregion SignalR

#region Cache

var redisConStr = builder.Configuration.GetConnectionString("RedisCache");
if (string.IsNullOrWhiteSpace(redisConStr))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConStr;
    });

    signalrBuilder.AddStackExchangeRedis(redisConStr, options =>
    {
        options.Configuration.ChannelPrefix = "GZCTF";
    });
}

#endregion Cache

#region Identity

builder.Services.AddDataProtection().PersistKeysToDbContext<AppDbContext>();

builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = IdentityConstants.ApplicationScheme;
    o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies(options =>
{
    options.ApplicationCookie?.Configure(cookie =>
    {
        cookie.Cookie.Name = "GZCTF_Token";
        cookie.SlidingExpiration = true;
        cookie.ExpireTimeSpan = TimeSpan.FromDays(7);
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

#region Services and Repositories

builder.Services.AddTransient<IMailSender, MailSender>()
    .Configure<EmailConfig>(builder.Configuration.GetSection(nameof(EmailConfig)));

builder.Services.AddSingleton<IRecaptchaExtension, RecaptchaExtension>()
    .Configure<RecaptchaConfig>(builder.Configuration.GetSection("GoogleRecaptcha"));

builder.Services.Configure<RegistryConfig>(builder.Configuration.GetSection(nameof(RegistryConfig)));
builder.Services.Configure<AccountPolicy>(builder.Configuration.GetSection(nameof(AccountPolicy)));
builder.Services.Configure<GlobalConfig>(builder.Configuration.GetSection(nameof(GlobalConfig)));
builder.Services.Configure<GamePolicy>(builder.Configuration.GetSection(nameof(GamePolicy)));
builder.Services.Configure<ContainerProvider>(builder.Configuration.GetSection(nameof(ContainerProvider)));

var forwardedOptions = builder.Configuration.GetSection(nameof(ForwardedOptions)).Get<ForwardedOptions>();
if (forwardedOptions is null)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });
}
else
{
    builder.Services.Configure<ForwardedHeadersOptions>(forwardedOptions.ToForwardedHeadersOptions);
}

if (builder.Configuration.GetSection(nameof(ContainerProvider))
    .GetValue<ContainerProviderType>(nameof(ContainerProvider.Type))
    is ContainerProviderType.Kubernetes)
{
    builder.Services.AddSingleton<IContainerService, K8sService>();
}
else
{
    builder.Services.AddSingleton<IContainerService, DockerService>();
}

builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IInstanceRepository, InstanceRepository>();
builder.Services.AddScoped<IContainerRepository, ContainerRepository>();
builder.Services.AddScoped<IChallengeRepository, ChallengeRepository>();
builder.Services.AddScoped<IGameEventRepository, GameEventRepository>();
builder.Services.AddScoped<ICheatInfoRepository, CheatInfoRepository>();
builder.Services.AddScoped<IGameNoticeRepository, GameNoticeRepository>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<IParticipationRepository, ParticipationRepository>();

builder.Services.AddChannel<Submission>();
builder.Services.AddChannel<CacheRequest>();
builder.Services.AddHostedService<CacheMaker>();
builder.Services.AddHostedService<FlagChecker>();
builder.Services.AddHostedService<CronJobService>();

#endregion Services and Repositories

builder.Services.AddHealthChecks();
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
    options.InvalidModelStateResponseFactory = InvalidModelStateHandler;
});

var app = builder.Build();

Log.Logger = LogHelper.GetLogger(app.Configuration, app.Services);

await app.RunPrelaunchWork();

app.UseResponseCompression();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
             "Cache-Control", $"public, max-age={60 * 60 * 24 * 7}");
    }
});

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseOpenApi(options => options.PostProcess += (document, _) => document.Servers.Clear());
    app.UseSwaggerUi3();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

if (app.Configuration.GetValue<bool>("DisableRateLimit") is not true)
    app.UseConfiguredRateLimiter();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("RequestLogging") is true)
    app.UseRequestLogging();

app.MapControllers();
app.MapHub<UserHub>("/hub/user");
app.MapHub<MonitorHub>("/hub/monitor");
app.MapHub<AdminHub>("/hub/admin");
app.MapHealthChecks("/healthz");
app.MapFallbackToFile("index.html");

await using var scope = app.Services.CreateAsyncScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    var version = typeof(Program).Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
    logger.SystemLog(version ?? "GZ::CTF", GZCTF.TaskStatus.Pending, LogLevel.Debug);
    await app.RunAsync();
}
catch (Exception exception)
{
    logger.LogError(exception, "因异常，应用程序意外终止");
    throw;
}
finally
{
    logger.SystemLog("服务器已退出", GZCTF.TaskStatus.Exit, LogLevel.Debug);
    Log.CloseAndFlush();
}

public partial class Program
{
    public static bool IsTesting { get; set; } = false;

    public static void Banner()
    {
        const string banner =
            @"      ___           ___           ___                       ___   " + "\n" +
            @"     /  /\         /  /\         /  /\          ___        /  /\  " + "\n" +
            @"    /  /:/_       /  /::|       /  /:/         /  /\      /  /:/_ " + "\n" +
            @"   /  /:/ /\     /  /:/:|      /  /:/         /  /:/     /  /:/ /\" + "\n" +
            @"  /  /:/_/::\   /  /:/|:|__   /  /:/  ___    /  /:/     /  /:/ /:/" + "\n" +
            @" /__/:/__\/\:\ /__/:/ |:| /\ /__/:/  /  /\  /  /::\    /__/:/ /:/ " + "\n" +
            @" \  \:\ /~~/:/ \__\/  |:|/:/ \  \:\ /  /:/ /__/:/\:\   \  \:\/:/  " + "\n" +
            @"  \  \:\  /:/      |  |:/:/   \  \:\  /:/  \__\/  \:\   \  \::/   " + "\n" +
            @"   \  \:\/:/       |  |::/     \  \:\/:/        \  \:\   \  \:\   " + "\n" +
            @"    \  \::/        |  |:/       \  \::/          \__\/    \  \:\  " + "\n" +
            @"     \__\/         |__|/         \__\/                     \__\/  " + "\n";
        Console.WriteLine(banner);

        var versionStr = "";
        var version = typeof(Codec).Assembly.GetName().Version;
        if (version is not null)
            versionStr = $"Version: {version.Major}.{version.Minor}.{version.Build}";

        Console.WriteLine($"GZCTF © 2022-present GZTimeWalker {versionStr,33}\n");
    }

    public static void ExitWithFatalMessage(string msg)
    {
        Log.Logger.Fatal(msg);
        Thread.Sleep(30000);
        Environment.Exit(1);
    }

    public static IActionResult InvalidModelStateHandler(ActionContext context)
    {
        string? errmsg = null;

        if (context.ModelState.ErrorCount > 0)
        {
            foreach (var val in context.ModelState.Values)
            {
                if (val.Errors.Count > 0)
                {
                    errmsg = val.Errors.FirstOrDefault()?.ErrorMessage;
                    break;
                }
            }
        }

        return new JsonResult(new RequestResponse(errmsg is not null && errmsg.Length > 0 ? errmsg : "校验失败，请检查输入。"))
        {
            StatusCode = 400
        };
    }
}
