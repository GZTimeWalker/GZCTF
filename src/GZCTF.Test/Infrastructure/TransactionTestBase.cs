using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace GZCTF.Test.Infrastructure;

/// <summary>
/// Base class for tests that require transaction support
/// Uses SQLite in-memory database which provides better transaction behavior than EF Core InMemory
/// </summary>
public abstract class TransactionTestBase : TestBase
{
    protected TransactionTestBase(ITestOutputHelper output) : base(output)
    {
        // Ensure database schema is created for SQLite
        DbContext.Database.EnsureCreated();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // Use SQLite in-memory for better transaction support
        TransactionTestHelper.ConfigureSqliteInMemoryContext(services, $"TransactionTest_{Guid.NewGuid()}");

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
    /// Executes a test action within a real database transaction
    /// This provides proper transaction isolation and rollback capabilities
    /// </summary>
    /// <param name="action">The test action to execute</param>
    /// <param name="shouldCommit">Whether to commit or rollback the transaction</param>
    protected async Task ExecuteInTransactionAsync(Func<Task> action, bool shouldCommit = true)
    {
        using var transaction = await DbContext.Database.BeginTransactionAsync();
        
        try
        {
            await action();
            
            if (shouldCommit)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Tests that a specific action properly handles transaction rollback
    /// </summary>
    /// <param name="setupAction">Action to set up test data</param>
    /// <param name="testAction">Action that should be rolled back</param>
    /// <param name="verificationAction">Action to verify rollback occurred</param>
    protected async Task TestTransactionRollback(
        Func<Task> setupAction,
        Func<Task> testAction,
        Func<Task> verificationAction)
    {
        // Setup
        await setupAction();
        await DbContext.SaveChangesAsync();

        // Execute test action in transaction that will be rolled back
        // If the action throws, the transaction is automatically rolled back
        try
        {
            await ExecuteInTransactionAsync(testAction, shouldCommit: false);
        }
        catch
        {
            // Exception is expected in rollback tests - it triggers the rollback
            // Swallow it and proceed to verification
        }

        // Verify rollback
        await verificationAction();
    }
}