using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using GZCTF.Models;
using Testcontainers.PostgreSql;
using Xunit;

namespace GZCTF.Integration.Test;

/// <summary>
/// Test application factory for integration tests with PostgreSQL testcontainer
/// </summary>
public class GZCTFApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("gzctf_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    private string? _connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variable to allow file directory creation
        Environment.SetEnvironmentVariable("YES_I_KNOW_FILES_ARE_NOT_PERSISTED_GO_AHEAD_PLEASE", "true");

        builder.UseEnvironment("Test");
        
        // Set content root to the test project output directory so appsettings.Test.json is found
        var testProjectDir = Directory.GetCurrentDirectory();
        builder.UseContentRoot(testProjectDir);
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing sources and rebuild with our connection string first
            // This ensures it's available during ConfigureDatabase()
            var sources = config.Sources.ToList();
            config.Sources.Clear();
            
            // Add our connection string FIRST
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _connectionString,
                ["ConnectionStrings:Storage"] = "disk://path=./files/test"
            });
            
            // Then add back all the original sources
            foreach (var source in sources)
            {
                config.Sources.Add(source);
            }
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove any background services that might interfere with testing
            services.RemoveAll(typeof(IHostedService));
            
            // Replace the DbContext with our test connection string
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_connectionString,
                    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });
    }

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container first
        await _postgresContainer.StartAsync();
        
        // Get and cache connection string
        _connectionString = _postgresContainer.GetConnectionString();
        
        // Set environment variable so it's available during configuration
        Environment.SetEnvironmentVariable("GZCTF_ConnectionStrings__Database", _connectionString);
        
        // Ensure the database is created and migrated
        // We do this after container is fully started
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(_connectionString);
        
        using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
