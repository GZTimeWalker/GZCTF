using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using GZCTF.Models;

namespace GZCTF.Test.Infrastructure;

/// <summary>
/// Helper class to handle in-memory database transaction limitations
/// </summary>
public static class TransactionTestHelper
{
    /// <summary>
    /// Creates a new DbContext instance with proper isolation for transaction testing
    /// This ensures each test gets a fresh database instance to avoid transaction conflicts
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>A new AppDbContext instance</returns>
    public static AppDbContext CreateIsolatedDbContext(IServiceProvider serviceProvider)
    {
        // Create a new service scope to ensure isolation
        var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Ensure the database is created
        context.Database.EnsureCreated();
        
        return context;
    }

    /// <summary>
    /// Executes an action within a transaction-like scope that handles in-memory database limitations
    /// For in-memory databases, this provides savepoint-like behavior by using separate contexts
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="action">The action to execute</param>
    /// <param name="rollback">Whether to rollback changes (simulated for in-memory)</param>
    public static async Task ExecuteInTransactionScopeAsync(
        IServiceProvider serviceProvider, 
        Func<AppDbContext, Task> action, 
        bool rollback = false)
    {
        if (rollback)
        {
            // For rollback scenarios, use a separate context that gets disposed
            using var isolatedContext = CreateIsolatedDbContext(serviceProvider);
            await action(isolatedContext);
            // Context disposal simulates rollback for in-memory databases
        }
        else
        {
            // For commit scenarios, use the main context
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await action(context);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Simulates transaction rollback by not saving changes to the context
    /// This is a workaround for in-memory database transaction limitations
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="action">The action to perform</param>
    public static async Task SimulateTransactionRollback(AppDbContext context, Func<Task> action)
    {
        // Track the original state
        var originalEntries = context.ChangeTracker.Entries().ToList();
        var originalStates = originalEntries.ToDictionary(e => e, e => e.State);

        try
        {
            await action();
            
            // Simulate rollback by reverting all changes
            foreach (var entry in context.ChangeTracker.Entries())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                    case EntityState.Modified:
                        entry.Reload();
                        break;
                    case EntityState.Deleted:
                        entry.Reload();
                        break;
                }
            }
        }
        catch
        {
            // On exception, automatically revert changes
            foreach (var entry in originalEntries)
            {
                if (originalStates.TryGetValue(entry, out var originalState))
                {
                    entry.State = originalState;
                }
            }
            throw;
        }
    }

    /// <summary>
    /// Checks if the current database provider supports transactions
    /// </summary>
    /// <param name="context">The database context</param>
    /// <returns>True if transactions are supported, false otherwise</returns>
    public static bool SupportsTransactions(AppDbContext context)
    {
        return context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
    }

    /// <summary>
    /// Creates a database context that uses SQLite in-memory for better transaction support
    /// This is an alternative to EF Core InMemory provider when transaction behavior is critical
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    /// <param name="databaseName">Unique database name</param>
    public static void ConfigureSqliteInMemoryContext(IServiceCollection services, string databaseName)
    {
        // Remove existing DbContext registration if present
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
        if (descriptor != null)
            services.Remove(descriptor);

        // Add SQLite in-memory database which has better transaction support
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite($"Data Source={databaseName};Mode=Memory;Cache=Shared");
        });
    }
}