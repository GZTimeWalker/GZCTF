using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Repositories;
using GZCTF.Repositories.Interface;
using GZCTF.Services;
using GZCTF.Services.Cache;
using GZCTF.Services.Config;
using GZCTF.Services.Container;
using GZCTF.Services.CronJob;
using GZCTF.Services.Mail;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;

namespace GZCTF.Extensions.Startup;

static class ServicesExtension
{
    static void AddConfig<TConfig>(this WebApplicationBuilder builder)
        where TConfig : class
        => builder.Services.Configure<TConfig>(builder.Configuration.GetSection(typeof(TConfig).Name));

    internal static void AddServiceConfigurations(this WebApplicationBuilder builder)
    {
        builder.AddConfig<EmailConfig>();
        builder.AddConfig<RegistryConfig>();
        builder.AddConfig<AccountPolicy>();
        builder.AddConfig<GlobalConfig>();
        builder.AddConfig<ContainerPolicy>();
        builder.AddConfig<ContainerProvider>();

        var forwardedOptions =
            builder.Configuration.GetSection(nameof(ForwardedOptions)).Get<ForwardedOptions>();
        if (forwardedOptions is null)
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
        else
            builder.Services.Configure<ForwardedHeadersOptions>(forwardedOptions.ToForwardedHeadersOptions);
    }

    internal static void AddCustomServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddCaptchaService(builder.Configuration);
        builder.Services.AddContainerService(builder.Configuration);

        builder.Services.AddScoped<IConfigService, ConfigService>();
        builder.Services.AddScoped<ILogRepository, LogRepository>();
        builder.Services.AddScoped<IBlobRepository, BlobRepository>();
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
        builder.Services.AddSingleton<IMailSender, MailSender>();

        builder.Services.AddHostedService<CacheMaker>();
        builder.Services.AddHostedService<FlagChecker>();
        builder.Services.AddHostedService<CronJobService>();
    }

    internal static void AddWebServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRouting(options => options.LowercaseUrls = true);
        builder.Services.AddRateLimiter(RateLimiter.ConfigureRateLimiter);
        builder.Services.AddResponseCompression(options =>
        {
            options.Providers.Add<ZStandardCompressionProvider>();
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                [
                    // See others in ResponseCompressionDefaults.MimeTypes
                    MediaTypeNames.Application.Pdf
                ]
            );
            options.EnableForHttps = true;
        });

        builder.Services.AddControllersWithViews().ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = InvalidModelStateHandler;
        }).AddDataAnnotationsLocalization(options =>
        {
            options.DataAnnotationLocalizerProvider = (_, factory) =>
                factory.Create(typeof(Resources.Program));
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            options.JsonSerializerOptions.Converters.Add(new DateTimeOffsetJsonConverter());
        });
        builder.Services.AddResponseCaching();
    }

    internal static void AddDevelopmentServices(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
            return;

        builder.Services.AddOpenApiDocument(settings =>
        {
            settings.DocumentName = "v1";
            settings.Version = "v1";
            settings.Title = "GZCTF Server API";
            settings.Description = "GZCTF Server API Document";
            settings.UseControllerSummaryAsTagDescription = true;
            settings.SchemaSettings.TypeMappers.Add(new OpenApiDateTimeOffsetToUIntMapper());
            settings.SchemaSettings.ReflectionService = new GenericsSystemTextJsonReflectionService();
        });
    }
}
