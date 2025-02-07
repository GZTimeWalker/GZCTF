using Serilog;
using StackExchange.Redis;

namespace GZCTF.Extensions.Startup;

static class AppBuilderExtensions
{
    internal static void ConfigureWebHost(this WebApplicationBuilder builder)
    {
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            options.SerializerOptions.Converters.Add(new DateTimeOffsetJsonConverter());
        });

        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources")
            .Configure<RequestLocalizationOptions>(options =>
            {
                options
                    .AddSupportedCultures(SupportedCultures)
                    .AddSupportedUICultures(SupportedCultures);

                options.ApplyCurrentCultureToResponseHeaders = true;
            });

        builder.WebHost.ConfigureKestrel(options =>
        {
            var kestrelSection = builder.Configuration.GetSection("Kestrel");
            options.Configure(kestrelSection);
            kestrelSection.Bind(options);
        }).UseKestrel(options =>
        {
            options.ListenAnyIP(ServerPort);
            options.ListenAnyIP(MetricPort);
        });

        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Host.UseSerilog(dispose: true);
        builder.Configuration.AddEnvironmentVariables("GZCTF_");
    }

    internal static void ConfigureCacheAndSignalR(this WebApplicationBuilder builder)
    {
        var signalrBuilder = builder.Services.AddSignalR().AddJsonProtocol();

        var connectionString = builder.Configuration.GetConnectionString("RedisCache");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            builder.Services.AddDistributedMemoryCache();
        }
        else
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
            });

            signalrBuilder.AddStackExchangeRedis(connectionString, options =>
            {
                options.Configuration.ChannelPrefix = new RedisChannel("GZCTF", RedisChannel.PatternMode.Literal);
            });
        }
    }
}
