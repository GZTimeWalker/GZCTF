using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithDataVolume();

if (builder.Environment.IsDevelopment())
{
    postgres.WithPgAdmin();
}

var database = postgres.AddDatabase("database");

var redis = builder.AddRedis("redis").WithDataVolume();

var web = builder.AddProject<Projects.GZCTF>("gzctf")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(redis)
    .WaitFor(redis)
    .WithEnvironment("Telemetry__OpenTelemetry__Enable", "true")
    .WithEnvironment("Telemetry__Prometheus__Enable", "true")
    .WithEndpoint("http", e => e.IsProxied = false)
    .WithOtlpExporter();

if (!builder.Environment.IsDevelopment())
{
    web.WithEnvironment("Storage__ConnectionString", "disk://path=/app/files");
}

builder.Build().Run();
