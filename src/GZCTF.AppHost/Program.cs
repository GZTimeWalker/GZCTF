using GZCTF.AppHost.MinIO;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithDataVolume();

if (builder.Environment.IsDevelopment())
{
    postgres.WithPgAdmin();
}

var database = postgres.AddDatabase("database");

var redis = builder.AddRedis("redis").WithDataVolume();

var storage = builder.AddMinIO("minio");

var web = builder.AddProject<Projects.GZCTF>("gzctf")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(storage)
    .WaitFor(storage)
    .WithEnvironment("Telemetry__OpenTelemetry__Enable", "true")
    .WithEnvironment("Telemetry__Prometheus__Enable", "true")
    .WithEnvironment("XorKey", "gzctf-xor-key")
    .WithEndpoint("http", e => e.IsProxied = false)
    .WithOtlpExporter();

builder.Build().Run();
