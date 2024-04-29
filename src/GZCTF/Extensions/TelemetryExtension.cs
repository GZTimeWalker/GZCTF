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
        if (config is not { Enable: true })
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

            if (config.Prometheus.Enable)
                metrics.AddPrometheusExporter(options =>
                {
                    options.DisableTotalNameSuffixForCounters = !config.Prometheus.TotalNameSuffixForCounters;
                });

            if (config.Console.Enable)
                metrics.AddConsoleExporter();
        });

        otl.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();
            tracing.AddEntityFrameworkCoreInstrumentation();
            tracing.AddRedisInstrumentation();
            tracing.AddNpgsql();
            if (config.Console.Enable)
                tracing.AddConsoleExporter();
        });

        if (config.AzureMonitor.Enable)
            otl.UseAzureMonitor(
                options => options.ConnectionString = config.AzureMonitor.ConnectionString);

        if (config.OpenTelemetry.Enable)
            otl.UseOtlpExporter(config.OpenTelemetry.Protocol,
                new(config.OpenTelemetry.EndpointUri ?? "http://localhost:4317"));
    }

    public static void UseTelemetry(this IApplicationBuilder app, TelemetryConfig? config)
    {
        if (config is not { Enable: true, Prometheus.Enable: true })
            return;

        if (config.Prometheus.Port is { } port)
            app.UseOpenTelemetryPrometheusScrapingEndpoint(context =>
            {
                if (context.Connection.LocalPort != port
                    || !string.Equals(
                        context.Request.Path.ToString().TrimEnd('/'),
                        "/metrics",
                        StringComparison.OrdinalIgnoreCase))
                    return false;

                // FIXME: workaround for prometheus
                context.Request.Headers.Accept = "application/openmetrics-text";
                return true;
            });
        else
            app.UseOpenTelemetryPrometheusScrapingEndpoint(context =>
            {
                if (!string.Equals(
                        context.Request.Path.ToString().TrimEnd('/'),
                        "/metrics",
                        StringComparison.OrdinalIgnoreCase))
                    return false;

                // FIXME: workaround for prometheus
                context.Request.Headers.Accept = "application/openmetrics-text";
                return true;
            });
    }
}
