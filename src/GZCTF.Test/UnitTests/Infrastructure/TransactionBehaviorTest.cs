using System;
using System.Threading.Tasks;
using FluentAssertions;
using GZCTF.Repositories;
using GZCTF.Test.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Test.UnitTests.Infrastructure;

public class TransactionBehaviorTest : TransactionTestBase
{
    public TransactionBehaviorTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Transaction_ShouldRollbackOnException()
    {
        // Arrange
        var user = TestDataFactory.CreateUser("testuser", "test@example.com");
        
        await TestTransactionRollback(
            setupAction: async () =>
            {
                // Setup: Add initial user
                DbContext.Users.Add(user);
            },
            testAction: async () =>
            {
                // Test action that should be rolled back
                var anotherUser = TestDataFactory.CreateUser("failuser", "fail@example.com");
                DbContext.Users.Add(anotherUser);
                await DbContext.SaveChangesAsync();
                
                // Simulate an error that should trigger rollback
                throw new InvalidOperationException("Simulated error");
            },
            verificationAction: async () =>
            {
                // Verify: Only the initial user should exist, rollback should have occurred
                var userCount = await DbContext.Users.CountAsync();
                userCount.Should().Be(1, "transaction should have been rolled back, leaving only the initial user");
                
                var existingUser = await DbContext.Users.FirstOrDefaultAsync(u => u.UserName == "testuser");
                existingUser.Should().NotBeNull("initial user should still exist");
                
                var failedUser = await DbContext.Users.FirstOrDefaultAsync(u => u.UserName == "failuser");
                failedUser.Should().BeNull("user added in failed transaction should not exist");
            }
        );
    }

    [Fact]
    public async Task Transaction_ShouldCommitSuccessfully()
    {
        // Arrange
        var user1 = TestDataFactory.CreateUser("user1", "user1@example.com");
        var user2 = TestDataFactory.CreateUser("user2", "user2@example.com");

        // Act
        await ExecuteInTransactionAsync(async () =>
        {
            DbContext.Users.AddRange(user1, user2);
            await DbContext.SaveChangesAsync();
        }, shouldCommit: true);

        // Assert
        var userCount = await DbContext.Users.CountAsync();
        userCount.Should().Be(2, "both users should be committed");
    }

    [Fact]
    public async Task RepositoryTransaction_ShouldWorkCorrectly()
    {
        // Arrange
        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<TestRepository>();
        var repository = new TestRepository(DbContext, logger);
        var captain = TestDataFactory.CreateUser();
        var team = TestDataFactory.CreateTeam(captain, "Test Team");

        // Act
        using var transaction = await repository.BeginTransactionAsync();
        
        DbContext.Users.Add(captain);
        DbContext.Teams.Add(team);
        await repository.SaveAsync();
        
        await transaction.CommitAsync();

        // Assert
        var savedTeam = await DbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        savedTeam.Should().NotBeNull("team should be saved after transaction commit");
    }

    [Fact]
    public void TransactionSupport_ShouldBeAvailable()
    {
        // Assert
        SupportsTransactions.Should().BeTrue("SQLite in-memory should support transactions");
    }

    [Fact]
    public async Task InMemoryDatabaseLimitation_ShouldBeDocumented()
    {
        // This test documents the limitation of EF Core InMemory database
        // and shows how our framework addresses it
        
        // Arrange: This test class uses SQLite (TransactionTestBase) which DOES support transactions
        // Act: Check transaction support
        var supportsTransactions = TransactionTestHelper.SupportsTransactions(DbContext);
        
        // Assert: SQLite supports transactions (unlike EF Core InMemory)
        supportsTransactions.Should().BeTrue("SQLite in-memory provider supports real transactions");
        
        // Our framework addresses the InMemory limitation by providing:
        // 1. TransactionTestBase with SQLite in-memory for real transaction testing (this class)
        // 2. TransactionTestHelper for simulating transaction behavior with InMemory
        // 3. Isolation helpers for avoiding cross-test contamination
        
        Output.WriteLine("✓ Transaction limitation documented and addressed in framework");
        Output.WriteLine("✓ TransactionTestBase uses SQLite for real transaction support");
        Output.WriteLine("✓ Use TransactionTestHelper methods for InMemory transaction simulation");
        Output.WriteLine($"✓ Current provider: {DbContext.Database.ProviderName}");
    }

    // Helper repository class for testing
    private class TestRepository : GZCTF.Repositories.RepositoryBase
    {
        public TestRepository(GZCTF.Models.AppDbContext context, ILogger<TestRepository> logger) 
            : base(context)
        {
        }
    }
}