# Transaction Testing Guide

## Overview

This document explains how to handle database transaction testing in GZCTF, addressing the limitations of in-memory databases and providing proper testing strategies.

## The Problem

Entity Framework Core's `InMemory` database provider has significant limitations when it comes to transaction support:

1. **No Isolation**: Transactions don't provide true isolation between concurrent operations
2. **Limited Rollback**: Transaction rollbacks may not work as expected
3. **No Concurrency Control**: Multiple transactions can interfere with each other
4. **No Real ACID Properties**: The in-memory provider doesn't implement full ACID semantics

## Solutions Provided

### 1. TransactionTestHelper

The `TransactionTestHelper` class provides utilities to work around in-memory database limitations:

```csharp
// Create isolated contexts for transaction-like behavior
var isolatedContext = TransactionTestHelper.CreateIsolatedDbContext(serviceProvider);

// Simulate transaction rollback
await TransactionTestHelper.SimulateTransactionRollback(context, async () => {
    // Operations that should be "rolled back"
});

// Check if current provider supports transactions
var supportsTransactions = TransactionTestHelper.SupportsTransactions(context);
```

### 2. TransactionTestBase

For tests that require real transaction behavior, use `TransactionTestBase` which uses SQLite in-memory:

```csharp
public class MyTransactionTest : TransactionTestBase
{
    public MyTransactionTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task MyTest_ShouldRollbackOnError()
    {
        await ExecuteInTransactionAsync(async () => {
            // Test operations
        }, shouldCommit: false);
    }
}
```

### 3. Enhanced TestBase

The standard `TestBase` now includes transaction-aware methods:

```csharp
public class MyTest : TestBase
{
    [Fact]
    public async Task MyTest_WithTransactionHelpers()
    {
        // Check if transactions are supported
        if (SupportsTransactions)
        {
            // Use real transactions
        }
        else
        {
            // Use simulation methods
            await SimulateTransactionRollback(async () => {
                // Operations to simulate rollback for
            });
        }
    }
}
```

## Best Practices

### When to Use Each Approach

1. **Standard TestBase**: For most unit tests that don't need transaction behavior
2. **TransactionTestBase**: For tests that specifically need to verify transaction logic
3. **TransactionTestHelper**: For simulating transaction behavior in integration tests

### Testing Transaction-Heavy Code

When testing repository methods or services that use transactions:

```csharp
[Fact]
public async Task Repository_ShouldHandleTransactionCorrectly()
{
    // Use TransactionTestBase for real transaction testing
    await ExecuteInTransactionAsync(async () => {
        using var transaction = await repository.BeginTransactionAsync();
        
        // Perform operations
        await repository.SaveAsync();
        
        await transaction.CommitAsync();
    });
}
```

### Testing Rollback Scenarios

```csharp
[Fact]
public async Task Service_ShouldRollbackOnError()
{
    await TestTransactionRollback(
        setupAction: async () => {
            // Setup test data
        },
        testAction: async () => {
            // Operations that should fail and rollback
            throw new Exception("Simulated error");
        },
        verificationAction: async () => {
            // Verify rollback occurred
        }
    );
}
```

## Migration Guide

If you have existing tests that fail due to transaction issues:

1. **For unit tests**: Use the enhanced `TestBase` with transaction simulation methods
2. **For integration tests**: Consider using `TransactionTestBase` if real transactions are needed
3. **For repository tests**: Use `ExecuteInTransactionScopeAsync` for proper isolation

## Performance Considerations

- **EF Core InMemory**: Fastest, but limited transaction support
- **SQLite InMemory**: Slightly slower, but full transaction support
- **Isolation**: Each test gets its own database instance to prevent interference

## Examples

See `TransactionBehaviorTest.cs` for comprehensive examples of:
- Transaction rollback testing
- Transaction commit verification
- Repository transaction testing
- Documentation of limitations and solutions