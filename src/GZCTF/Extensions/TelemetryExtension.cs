using Azure.Monitor.OpenTelemetry.AspNetCore;
using GZCTF.Models.Internal;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GZCTF.Extensions;

public static class TelemetryExtension
{
    public static void AddTelemetry(this IServiceCollection services, TelemetryConfig? config)
    {
        if (config is not (
        { OpenTelemetry.Enable: true }
            or { Prometheus.Enable: true }
            or { AzureMonitor.Enable: true }
            or { Console.Enable: true }))
            return;

        var otl = services.AddOpenTelemetry();

        otl.ConfigureResource(resource =>
            resource.AddService("GZCTF",
                serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(3)));

        otl.WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation();
            metrics.AddHttpClientInstrumentation();
            metrics.AddRuntimeInstrumentation();
            metrics.AddProcessInstrumentation();

            if (config is { Prometheus.Enable: true })
                metrics.AddPrometheusExporter(options =>
                {
                    options.DisableTotalNameSuffixForCounters = !config.Prometheus.TotalNameSuffixForCounters;
                });

            if (config is { Console.Enable: true })
                metrics.AddConsoleExporter();
        });

        otl.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();
            tracing.AddEntityFrameworkCoreInstrumentation();
            tracing.AddRedisInstrumentation();
            tracing.AddNpgsql();
            if (config is { Console.Enable: true })
                tracing.AddConsoleExporter();
        });

        if (config is { AzureMonitor.Enable: true })
            otl.UseAzureMonitor(
                options => options.ConnectionString = config.AzureMonitor.ConnectionString);

        if (config is { OpenTelemetry.Enable: true })
            otl.UseOtlpExporter(config.OpenTelemetry.Protocol,
                new(config.OpenTelemetry.EndpointUri ?? "http://localhost:4317"));
    }

    public static void UseTelemetry(this IApplicationBuilder app, TelemetryConfig? config)
    {
        if (config is not { Prometheus.Enable: true })
            return;

        if (config.Prometheus.Port is { } port)
            app.UseOpenTelemetryPrometheusScrapingEndpoint(context
                => context.Connection.LocalPort == port
                   && string.Equals(
                       context.Request.Path.ToString().TrimEnd('/'),
                       "/metrics",
                       StringComparison.OrdinalIgnoreCase));
        else
            app.UseOpenTelemetryPrometheusScrapingEndpoint(context
                => string.Equals(
                    context.Request.Path.ToString().TrimEnd('/'),
                    "/metrics",
                    StringComparison.OrdinalIgnoreCase));
    }
}
