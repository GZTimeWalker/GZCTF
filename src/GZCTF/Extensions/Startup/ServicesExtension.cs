using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Features.ChallengeLibrary.Application;
using GZCTF.Features.ChallengeRuntime.Application;
using GZCTF.Features.ChallengeRuntime.Infrastructure;
using GZCTF.Features.Imports.Application;
using GZCTF.Features.Imports.Infrastructure;
using GZCTF.Features.LearningPaths.Application;
using GZCTF.Features.LearningProgress.Application;
using GZCTF.Features.Dashboard.Application;
using GZCTF.Features.SkillTrees.Application;
using GZCTF.Features.SkillTrees.Migration;
using GZCTF.Models.Internal;
using GZCTF.Repositories;
using GZCTF.Repositories.Interface;
using GZCTF.Services;
using GZCTF.Services.Cache;
using GZCTF.Services.Config;
using GZCTF.Services.Container;
using GZCTF.Services.CronJob;
using GZCTF.Services.Mail;
using GZCTF.Services.Token;
using GZCTF.Services.Traffic;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;

namespace GZCTF.Extensions.Startup;

internal static class ServicesExtension
{
    extension(WebApplicationBuilder builder)
    {
        private void AddConfig<TConfig>()
            where TConfig : class
            => builder.Services.Configure<TConfig>(builder.Configuration.GetSection(typeof(TConfig).Name));

        internal void AddServiceConfigurations()
        {
            builder.AddConfig<EmailConfig>();
            builder.AddConfig<AccountPolicy>();
            builder.AddConfig<GlobalConfig>();
            builder.AddConfig<ManagedConfig>();
            builder.AddConfig<ContainerPolicy>();
            builder.AddConfig<ContainerProvider>();

            builder.Services.Configure<RegistrySet<RegistryConfig>>(builder.Configuration.GetSection("Registries"));

            var oldConfig = builder.Configuration.GetSection(nameof(RegistryConfig)).Get<RegistryConfig>();
            if (!string.IsNullOrWhiteSpace(oldConfig?.ServerAddress))
                // Add old config to new config set
                builder.Services.Configure<RegistrySet<RegistryConfig>>(set =>
                {
                    if (!set.TryAdd(oldConfig.ServerAddress, oldConfig))
                        set[oldConfig.ServerAddress] = oldConfig;
                });

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

        internal void AddCustomServices()
        {
            builder.Services.AddCaptchaService(builder.Configuration);
            builder.Services.AddContainerService(builder.Configuration);

            builder.Services.AddScoped<IConfigService, ConfigService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<ILogRepository, LogRepository>();
            builder.Services.AddScoped<IBlobRepository, BlobRepository>();
            builder.Services.AddScoped<IPostRepository, PostRepository>();
            builder.Services.AddScoped<IApiTokenRepository, ApiTokenRepository>();
            builder.Services.AddScoped<IContainerRepository, ContainerRepository>();
            builder.Services.AddScoped<ChallengeLibraryService>();
            builder.Services.AddScoped<LearningPathService>();
            builder.Services.AddScoped<EnrollmentService>();
            builder.Services.AddScoped<LessonProgressService>();
            builder.Services.AddScoped<LearningRecordService>();
            builder.Services.AddScoped<SkillTreeQueryService>();
            builder.Services.AddScoped<AdminSkillTreeService>();
            builder.Services.AddScoped<SkillCategoryService>();
            builder.Services.AddScoped<ContentPublicationService>();
            builder.Services.AddScoped<SkillTreeEnrollmentService>();
            builder.Services.AddScoped<LearningRedirectService>();
            builder.Services.AddSingleton<ISkillTreeCacheInvalidator, NoopSkillTreeCacheInvalidator>();
            builder.Services.AddScoped<SkillTreeBackfillService>();
            builder.Services.AddScoped<DynamicAttachmentAllocator>();
            builder.Services.AddScoped<ILegacyStorageAdapter, LegacyStorageAdapter>();
            builder.Services.AddScoped<ChallengeRuntimeService>();
            builder.Services.AddScoped<ILegacyContainerRuntimeAdapter, LegacyContainerRuntimeAdapter>();
            builder.Services.AddScoped<ChallengeSubmissionService>();
            builder.Services.AddScoped<ChallengeHelpService>();
            builder.Services.AddScoped<DailySolveProjection>();
            builder.Services.AddScoped<DashboardSnapshotService>();
            builder.Services.AddSingleton(_ => new DashboardRequestLimiter());
            builder.Services.AddScoped<DashboardTokenService>();
            builder.Services.AddScoped<DashboardDeltaPublisher>();
            builder.Services.AddSingleton<DashboardCache>();
            builder.Services.AddScoped<CanonicalImportService>();
            builder.Services.AddScoped<ImportParityService>();
            builder.Services.AddScoped<LegacyDatabaseSource>();
            builder.Services.AddScoped<StartupLegacyMigrationService>();
            builder.Services.AddScoped<LegacyZipSource>();
            builder.Services.AddScoped<ImportBlobStaging>();
            builder.Services.AddSingleton<IChallengeMergeConflictChecker, NoopChallengeMergeConflictChecker>();

            builder.Services.AddChannel<CacheRequest>();
            builder.Services.AddSingleton<CacheHelper>();
            builder.Services.AddSingleton<IMailSender, MailSender>();
            builder.Services.AddSingleton<TrafficRecorderRegistry>();

            builder.Services.AddHostedService<CronJobService>();
        }

        internal void AddWebServices()
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
                options.JsonSerializerOptions.ConfigCustomSerializerOptions();
            });
            builder.Services.AddResponseCaching();
        }

        internal void AddDevelopmentServices()
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
                settings.SchemaSettings.TypeMappers.Add(new OpenApiIPAddressToStringMapper());
                settings.SchemaSettings.ReflectionService = new GenericsSystemTextJsonReflectionService();
            });
        }
    }
}
