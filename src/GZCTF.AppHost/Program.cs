var builder = DistributedApplication.CreateBuilder(args);

var isDevelopment = builder.Environment.EnvironmentName == "Development";

// PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_DB", "gzctf")
    .WithDataVolume();

// Add PgAdmin only in development
if (isDevelopment)
{
    postgres.WithPgAdmin();
}

var database = postgres.AddDatabase("database");

// Redis cache and SignalR backplane
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// GZCTF Backend API
var apiService = builder.AddProject<Projects.GZCTF>("gzctf")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", isDevelopment ? "Development" : "Production")
    .WithEnvironment("DOTNET_RUNNING_IN_ASPIRE", "true");

// Enable Kubernetes manifest generation for production deployment
if (!isDevelopment)
{
    // Configure for production deployment
    apiService.WithEnvironment("Storage__ConnectionString", "disk://path=/app/files");
}

builder.Build().Run();
