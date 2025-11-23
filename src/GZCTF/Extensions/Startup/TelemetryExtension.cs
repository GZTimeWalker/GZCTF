using Azure.Monitor.OpenTelemetry.AspNetCore;
using GZCTF.Models.Internal;
using GZCTF.Services.HealthCheck;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GZCTF.Extensions.Startup;

public static class TelemetryExtension
{
    internal static TelemetryConfig? TelemetryConfig;

    extension(WebApplicationBuilder builder)
    {
        public void ConfigureTelemetry()
        {
            builder.Services.AddHealthChecks()
                .AddApplicationLifecycleHealthCheck()
                .AddCheck<StorageHealthCheck>("Storage")
                .AddCheck<CacheHealthCheck>("Cache")
                .AddCheck<DatabaseHealthCheck>("Database");

            TelemetryConfig = builder.Configuration.GetSection("Telemetry").Get<TelemetryConfig>();

            if (TelemetryConfig is not { Enable: true })
                return;

            builder.Services.AddTelemetryHealthCheckPublisher();
            var otl = builder.Services.AddOpenTelemetry();

            otl.ConfigureResource(resource =>
                resource.AddService("GZCTF",
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(3)));

            otl.WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();
                metrics.AddProcessInstrumentation();
                metrics.AddNpgsqlInstrumentation();
                metrics.AddAWSInstrumentation();
                metrics.AddMeter("Microsoft.Extensions.Diagnostics.HealthChecks");

                if (TelemetryConfig is { Prometheus.Enable: true })
                    metrics.AddPrometheusExporter(options =>
                    {
                        options.DisableTotalNameSuffixForCounters =
                            !TelemetryConfig.Prometheus.TotalNameSuffixForCounters;
                    });

                if (TelemetryConfig is { Console.Enable: true })
                    metrics.AddConsoleExporter();
            });

            otl.WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddEntityFrameworkCoreInstrumentation();
                tracing.AddRedisInstrumentation();
                tracing.AddNpgsql();
                tracing.AddAWSInstrumentation();
                tracing.AddGrpcClientInstrumentation();

                if (TelemetryConfig is { Console.Enable: true })
                    tracing.AddConsoleExporter();
            });

            if (TelemetryConfig is { AzureMonitor.Enable: true })
                otl.UseAzureMonitor(options =>
                    options.ConnectionString = TelemetryConfig.AzureMonitor.ConnectionString);

            if (TelemetryConfig is not { OpenTelemetry: { Enable: true, EndpointUri: var uri } })
                return;

            builder.Logging.AddOpenTelemetry(options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
                options.ParseStateValues = true;
            });

            if (uri is null)
                otl.UseOtlpExporter();
            else
                otl.UseOtlpExporter(TelemetryConfig.OpenTelemetry.Protocol, new(uri));
        }
    }

    extension(WebApplication app)
    {
        public void MapHealthCheck() =>
            app.MapHealthChecks("/healthz").DisableHttpMetrics()
                .AddEndpointFilter(async (context, next)
                    => context.HttpContext.Connection.LocalPort == MetricPort
                        ? await next(context)
                        : Results.NotFound());
    }

    extension(IApplicationBuilder appBuilder)
    {
        public void UseTelemetry()
        {
            if (TelemetryConfig is not { Prometheus.Enable: true })
                return;

            appBuilder.UseOpenTelemetryPrometheusScrapingEndpoint(context
                => context.Connection.LocalPort == MetricPort
                   && string.Equals(
                       context.Request.Path.ToString().TrimEnd('/'),
                       "/metrics",
                       StringComparison.OrdinalIgnoreCase));
        }
    }
}
