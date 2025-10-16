using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using GZCTF.Models;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;
using Xunit.Extensions.Logging;

namespace GZCTF.Test.Infrastructure;

/// <summary>
/// Base class for all tests providing common setup and utilities
/// </summary>
public abstract class TestBase : IDisposable
{
    protected readonly IFixture Fixture;
    protected readonly ITestOutputHelper Output;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly AppDbContext DbContext;
    protected readonly Mock<IDistributedCache> MockCache;
    protected readonly Mock<ILogger> MockLogger;
    protected readonly IConfiguration Configuration;

    protected TestBase(ITestOutputHelper output)
    {
        Output = output;
        Fixture = new Fixture();
        
        // Configure AutoFixture to handle circular references
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Setup configuration
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = "Host=localhost;Database=gzctf_test;Username=test;Password=test",
                ["XorKey"] = "test-xor-key",
                ["GlobalConfig:Title"] = "Test GZCTF",
                ["GlobalConfig:Slogan"] = "Test slogan"
            });
        Configuration = configurationBuilder.Build();

        // Setup mocks
        MockCache = new Mock<IDistributedCache>();
        MockLogger = new Mock<ILogger>();

        // Setup services
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // Setup in-memory database
        DbContext = ServiceProvider.GetRequiredService<AppDbContext>();
        DbContext.Database.EnsureCreated();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Add in-memory database
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Add configuration
        services.AddSingleton(Configuration);

        // Add mocked services
        services.AddSingleton(MockCache.Object);
        services.AddSingleton(MockLogger.Object);

        // Add logging
        services.AddLogging();

        // Add common services that tests might need
        services.AddOptions();
        services.AddMemoryCache();
    }

    /// <summary>
    /// Creates an isolated database context for transaction testing
    /// This helps avoid issues with in-memory database transaction limitations
    /// </summary>
    protected AppDbContext CreateIsolatedDbContext() => 
        TransactionTestHelper.CreateIsolatedDbContext(ServiceProvider);

    /// <summary>
    /// Executes an action in a transaction-like scope with proper handling for in-memory databases
    /// </summary>
    protected Task ExecuteInTransactionScopeAsync(Func<AppDbContext, Task> action, bool rollback = false) =>
        TransactionTestHelper.ExecuteInTransactionScopeAsync(ServiceProvider, action, rollback);

    /// <summary>
    /// Simulates transaction rollback for testing scenarios where transactions need to be reverted
    /// </summary>
    protected Task SimulateTransactionRollback(Func<Task> action) =>
        TransactionTestHelper.SimulateTransactionRollback(DbContext, action);

    /// <summary>
    /// Checks if the current database provider supports real transactions
    /// </summary>
    protected bool SupportsTransactions => TransactionTestHelper.SupportsTransactions(DbContext);

    protected T GetService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();

    protected T? GetOptionalService<T>() => ServiceProvider.GetService<T>();

    protected Mock<T> GetMock<T>() where T : class => Mock.Get(GetService<T>());

    public virtual void Dispose()
    {
        DbContext?.Dispose();
        ServiceProvider?.GetService<IServiceScope>()?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Attribute to automatically inject test data using AutoFixture
/// </summary>
public class GzctfAutoDataAttribute : AutoDataAttribute
{
    public GzctfAutoDataAttribute() : base(() => new Fixture())
    {
    }
}

/// <summary>
/// Attribute to automatically inject test data with inline values
/// </summary>
public class GzctfInlineAutoDataAttribute : InlineAutoDataAttribute
{
    public GzctfInlineAutoDataAttribute(params object[] values) : base(new GzctfAutoDataAttribute(), values)
    {
    }
}