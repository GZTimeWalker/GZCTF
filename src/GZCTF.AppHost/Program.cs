using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("PostgreSQL").WithDataVolume();

if (builder.Environment.IsDevelopment())
{
    postgres.WithPgAdmin();
}

var database = postgres.AddDatabase("Database");

var redis = builder.AddRedis("Redis").WithDataVolume();

var apiService = builder.AddProject<Projects.GZCTF>("GZCTF")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("Telemetry__OpenTelemetry__Enable", "true")
    .WithEnvironment("Telemetry__Prometheus__Enable", "true")
    .WithEndpoint("http", e => e.IsProxied = false)
    .WithOtlpExporter();

if (!builder.Environment.IsDevelopment())
{
    apiService.WithEnvironment("Storage__ConnectionString", "disk://path=/app/files");
}

builder.Build().Run();
