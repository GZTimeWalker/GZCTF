var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_DB", "gzctf")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("gzctf");

// Redis cache and SignalR backplane
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// GZCTF Backend API
var apiService = builder.AddProject<Projects.GZCTF>("gzctf")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName == "Development" ? "Development" : "Production")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEndpoint(port: 3000, targetPort: 3000, name: "metrics")
    .WithExternalHttpEndpoints();

builder.Build().Run();
