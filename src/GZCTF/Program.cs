/*
 * GZ::CTF
 *
 * Copyright © 2022-present GZTimeWalker
 *
 * This source code is licensed under the AGPLv3 license found in the LICENSE file
 * in the root directory of this source tree.
 *
 * Identifiers related to "GZCTF" (including variations and derivations) are protected.
 * Examples include "GZCTF", "GZ::CTF", "GZCTF_FLAG", and similar constructs.
 *
 * Modifications to these identifiers are prohibited as per the LICENSE_ADDENDUM.txt
 */

global using GZCTF.Models.Data;
global using GZCTF.Utils;
global using AppDbContext = GZCTF.Models.AppDbContext;
global using TaskStatus = GZCTF.Utils.TaskStatus;
using System.Globalization;
using System.Net.Mime;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using GZCTF.Extensions;
using GZCTF.Hubs;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Repositories;
using GZCTF.Repositories.Interface;
using GZCTF.Services;
using GZCTF.Services.Cache;
using GZCTF.Services.Config;
using GZCTF.Services.Container;
using GZCTF.Services.Mail;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using StackExchange.Redis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

GZCTF.Program.Banner();

#region Host

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources")
    .Configure<RequestLocalizationOptions>(options =>
    {
        string[] supportedCultures = ["en-US", "zh-CN", "ja-JP"];

        options
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        options.ApplyCurrentCultureToResponseHeaders = true;
    });

builder.WebHost.ConfigureKestrel(options =>
{
    IConfigurationSection kestrelSection = builder.Configuration.GetSection("Kestrel");
    options.Configure(kestrelSection);
    kestrelSection.Bind(options);
});

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Host.UseSerilog(dispose: true);
builder.Configuration.AddEnvironmentVariables("GZCTF_");
Log.Logger = LogHelper.GetInitLogger();

await FilePath.EnsureDirsAsync(builder.Environment);

#endregion Host

#region AppDbContext

if (GZCTF.Program.IsTesting || (builder.Environment.IsDevelopment() &&
                                !builder.Configuration.GetSection("ConnectionStrings").Exists()))
{
    builder.Services.AddDbContext<AppDbContext>(
        options => options.UseInMemoryDatabase("TestDb")
    );
}
else
{
    if (!builder.Configuration.GetSection("ConnectionStrings").GetSection("Database").Exists())
        GZCTF.Program.ExitWithFatalMessage(
            GZCTF.Program.StaticLocalizer[nameof(GZCTF.Resources.Program.Database_NoConnectionString)]);

    builder.Services.AddDbContext<AppDbContext>(
        options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("Database"),
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

            if (!builder.Environment.IsDevelopment())
                return;

            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    );
}

#endregion AppDbContext

#region Configuration

if (!GZCTF.Program.IsTesting)
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
    catch (Exception e)
    {
        if (builder.Configuration.GetSection("ConnectionStrings").GetSection("Database").Exists())
            Log.Logger.Error(GZCTF.Program.StaticLocalizer[
                nameof(GZCTF.Resources.Program.Database_CurrentConnectionString),
                builder.Configuration.GetConnectionString("Database") ?? "null"]);
        GZCTF.Program.ExitWithFatalMessage(
            GZCTF.Program.StaticLocalizer[nameof(GZCTF.Resources.Program.Database_ConnectionFailed), e.Message]);
    }

#endregion Configuration

#region OpenApiDocument

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddOpenApiDocument(settings =>
{
    settings.DocumentName = "v1";
    settings.Version = "v1";
    settings.Title = "GZCTF Server API";
    settings.Description = "GZCTF Server API Document";
    settings.UseControllerSummaryAsTagDescription = true;
});

#endregion OpenApiDocument

#region SignalR & Cache

ISignalRServerBuilder signalrBuilder = builder.Services.AddSignalR().AddJsonProtocol();

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
        options.Configuration.ChannelPrefix = new RedisChannel("GZCTF", RedisChannel.PatternMode.Literal);
    });
}

#endregion SignalR & Cache

#region Identity

builder.Services.AddDataProtection().PersistKeysToDbContext<AppDbContext>();

builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = IdentityConstants.ApplicationScheme;
    o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies(options =>
{
    options.ApplicationCookie?.Configure(auth =>
    {
        auth.Cookie.Name = "GZCTF_Token";
        auth.SlidingExpiration = true;
        auth.ExpireTimeSpan = TimeSpan.FromDays(7);
    });
});

builder.Services.AddIdentityCore<UserInfo>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedEmail = true;

        // Allow all characters in username
        options.User.AllowedUserNameCharacters = string.Empty;
    })
    .AddSignInManager<SignInManager<UserInfo>>()
    .AddUserManager<UserManager<UserInfo>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddErrorDescriber<TranslatedIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
    o.TokenLifespan = TimeSpan.FromHours(3)
);

#endregion Identity

#region Telemetry

var telemetryOptions = builder.Configuration.GetSection("Telemetry").Get<TelemetryConfig>();
builder.Services.AddTelemetry(telemetryOptions);

#endregion

#region Services and Repositories

builder.Services.AddSingleton<IMailSender, MailSender>()
    .Configure<EmailConfig>(builder.Configuration.GetSection(nameof(EmailConfig)));

builder.Services.Configure<RegistryConfig>(builder.Configuration.GetSection(nameof(RegistryConfig)));
builder.Services.Configure<AccountPolicy>(builder.Configuration.GetSection(nameof(AccountPolicy)));
builder.Services.Configure<GlobalConfig>(builder.Configuration.GetSection(nameof(GlobalConfig)));
builder.Services.Configure<ContainerPolicy>(builder.Configuration.GetSection(nameof(ContainerPolicy)));
builder.Services.Configure<ContainerProvider>(builder.Configuration.GetSection(nameof(ContainerProvider)));

var forwardedOptions = builder.Configuration.GetSection(nameof(ForwardedOptions)).Get<ForwardedOptions>();
if (forwardedOptions is null)
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });
else
    builder.Services.Configure<ForwardedHeadersOptions>(forwardedOptions.ToForwardedHeadersOptions);

builder.Services.AddCaptchaService(builder.Configuration);
builder.Services.AddContainerService(builder.Configuration);

builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IContainerRepository, ContainerRepository>();
builder.Services.AddScoped<IGameEventRepository, GameEventRepository>();
builder.Services.AddScoped<ICheatInfoRepository, CheatInfoRepository>();
builder.Services.AddScoped<IGameNoticeRepository, GameNoticeRepository>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<IGameInstanceRepository, GameInstanceRepository>();
builder.Services.AddScoped<IGameChallengeRepository, GameChallengeRepository>();
builder.Services.AddScoped<IParticipationRepository, ParticipationRepository>();
builder.Services.AddScoped<IExerciseInstanceRepository, ExerciseInstanceRepository>();
builder.Services.AddScoped<IExerciseChallengeRepository, ExerciseChallengeRepository>();

builder.Services.AddScoped<ExcelHelper>();

builder.Services.AddChannel<Submission>();
builder.Services.AddChannel<CacheRequest>();
builder.Services.AddSingleton<CacheHelper>();

builder.Services.AddHostedService<CacheMaker>();
builder.Services.AddHostedService<FlagChecker>();
builder.Services.AddHostedService<CronJobService>();

builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(RateLimiter.ConfigureRateLimiter);
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        [
            MediaTypeNames.Application.Json,
            MediaTypeNames.Text.Html,
            MediaTypeNames.Text.JavaScript,
            MediaTypeNames.Text.Css
        ]
    );
});

builder.Services.AddControllersWithViews().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = GZCTF.Program.InvalidModelStateHandler;
}).AddDataAnnotationsLocalization(options =>
{
    options.DataAnnotationLocalizerProvider = (_, factory) =>
        factory.Create(typeof(GZCTF.Resources.Program));
});

#endregion Services and Repositories

#region Middlewares

WebApplication app = builder.Build();

Log.Logger = LogHelper.GetLogger(app.Configuration, app.Services);

await app.RunPrelaunchWork();

app.UseRequestLocalization();

app.UseResponseCompression();

app.UseCustomFavicon();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = $"public, max-age={60 * 60 * 24 * 7}";
    }
});

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseOpenApi(options =>
    {
        options.PostProcess += (document, _) => document.Servers.Clear();
    });
    app.UseSwaggerUi();
}
else
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

app.UseRouting();

if (app.Configuration.GetValue<bool>("DisableRateLimit") is not true)
    app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("RequestLogging"))
    app.UseRequestLogging();

app.UseWebSockets(new() { KeepAliveInterval = TimeSpan.FromMinutes(30) });

app.UseTelemetry(telemetryOptions);

app.MapHealthChecks("/healthz");
app.MapControllers();

app.MapHub<UserHub>("/hub/user");
app.MapHub<MonitorHub>("/hub/monitor");
app.MapHub<AdminHub>("/hub/admin");

await app.UseIndexAsync();

#endregion Middlewares

await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<GZCTF.Program>>();

try
{
    var version = typeof(GZCTF.Program).Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
    logger.SystemLog(version ?? "GZ::CTF", TaskStatus.Pending, LogLevel.Debug);
    await app.RunAsync();
}
catch (Exception exception)
{
    logger.LogError(exception, GZCTF.Program.StaticLocalizer[nameof(GZCTF.Resources.Program.Server_Failed)]);
    throw;
}
finally
{
    logger.SystemLog(GZCTF.Program.StaticLocalizer[nameof(GZCTF.Resources.Program.Server_Exited)], TaskStatus.Exit,
        LogLevel.Debug);
    Log.CloseAndFlush();
}

namespace GZCTF
{
    public class Program
    {
        static Program()
        {
            using Stream stream = typeof(Program).Assembly
                .GetManifestResourceStream("GZCTF.Resources.favicon.webp")!;
            DefaultFavicon = new byte[stream.Length];

            stream.ReadExactly(DefaultFavicon);
            DefaultFaviconHash = BitConverter.ToString(SHA256.HashData(DefaultFavicon))
                .Replace("-", "").ToLowerInvariant();
        }

        public static bool IsTesting { get; set; }

        internal static IStringLocalizer<Program> StaticLocalizer { get; } =
            new CulturedLocalizer<Program>(CultureInfo.CurrentCulture);

        internal static byte[] DefaultFavicon { get; }
        internal static string DefaultFaviconHash { get; }

        internal static void Banner()
        {
            // The GZCTF identifier is protected by the License.
            // DO NOT REMOVE OR MODIFY THE FOLLOWING LINE.
            // Please see LICENSE_ADDENDUM.txt for details.
            const string banner =
                """
                      ___           ___           ___                       ___
                     /  /\         /  /\         /  /\          ___        /  /\
                    /  /:/_       /  /::|       /  /:/         /  /\      /  /:/_
                   /  /:/ /\     /  /:/:|      /  /:/         /  /:/     /  /:/ /\
                  /  /:/_/::\   /  /:/|:|__   /  /:/  ___    /  /:/     /  /:/ /:/
                 /__/:/__\/\:\ /__/:/ |:| /\ /__/:/  /  /\  /  /::\    /__/:/ /:/
                 \  \:\ /~~/:/ \__\/  |:|/:/ \  \:\ /  /:/ /__/:/\:\   \  \:\/:/
                  \  \:\  /:/      |  |:/:/   \  \:\  /:/  \__\/  \:\   \  \::/
                   \  \:\/:/       |  |::/     \  \:\/:/        \  \:\   \  \:\
                    \  \::/        |  |:/       \  \::/          \__\/    \  \:\
                     \__\/         |__|/         \__\/                     \__\/
                """ + "\n";
            Console.WriteLine(banner);

            var versionStr = "";
            Version? version = typeof(Program).Assembly.GetName().Version;
            if (version is not null)
                versionStr = $"Version: {version.Major}.{version.Minor}.{version.Build}";

            // ReSharper disable once LocalizableElement
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
            var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<Program>>();
            if (context.ModelState.ErrorCount <= 0)
                return new JsonResult(new RequestResponse(
                    localizer[nameof(Resources.Program.Model_ValidationFailed)]))
                { StatusCode = 400 };

            var error = context.ModelState.Values.Where(v => v.Errors.Count > 0)
                .Select(v => v.Errors.FirstOrDefault()?.ErrorMessage).FirstOrDefault();

            return new JsonResult(new RequestResponse(error is [_, ..]
                ? error
                : localizer[nameof(Resources.Program.Model_ValidationFailed)]))
            { StatusCode = 400 };
        }
    }
}
