using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GZCTF.Extensions;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Services.Cache;
using GZCTF.Services.Config;
using GZCTF.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.Logging;

namespace GZCTF.Test.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration tests
/// </summary>
public class GzctfWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly ITestOutputHelper? _output;
    private readonly Action<IServiceCollection>? _configureServices;

    public GzctfWebApplicationFactory(ITestOutputHelper? output = null, Action<IServiceCollection>? configureServices = null)
    {
        _output = output;
        _configureServices = configureServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override configuration for testing
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = "Host=localhost;Database=gzctf_integration_test;Username=test;Password=test",
                ["ConnectionStrings:RedisCache"] = "",  // Disable Redis for tests
                ["ConnectionStrings:Storage"] = "disk://path=./test_storage",
                ["XorKey"] = "test-xor-key-for-integration",
                ["GlobalConfig:Title"] = "Integration Test GZCTF",
                ["GlobalConfig:Slogan"] = "Testing is fun",
                ["ContainerProvider:Type"] = "Docker",
                ["ContainerProvider:PublicEntry"] = "localhost",
                ["DisableRateLimit"] = "true",
                ["CaptchaConfig:Provider"] = "HashPow"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the database with in-memory database
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTest_" + Guid.NewGuid());
            });

            // Add test logging if output helper is provided
            if (_output != null)
            {
                services.AddLogging();
            }

            // Configure additional test services
            _configureServices?.Invoke(services);

            // Ensure the database is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Seed the database with test data
    /// </summary>
    public async Task SeedDataAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await TestDataFactory.SeedDatabaseAsync(context);
    }

    /// <summary>
    /// Get a service from the test container
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Execute a database operation in a scope
    /// </summary>
    public async Task<T> ExecuteDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(context);
    }

    /// <summary>
    /// Execute a database operation in a scope
    /// </summary>
    public async Task ExecuteDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(context);
    }
}

/// <summary>
/// Base class for integration tests
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<GzctfWebApplicationFactory>, IDisposable
{
    protected readonly GzctfWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected readonly ITestOutputHelper Output;

    protected IntegrationTestBase(GzctfWebApplicationFactory factory, ITestOutputHelper output)
    {
        Factory = factory;
        Output = output;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Create an authenticated client for a specific user role
    /// </summary>
    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string username = "admin", string email = "admin@test.com", Role role = Role.Admin)
    {
        // This would be implemented based on your authentication mechanism
        // For now, returning the base client
        await Task.CompletedTask;
        return Client;
    }

    public virtual void Dispose()
    {
        Client?.Dispose();
        GC.SuppressFinalize(this);
    }
}