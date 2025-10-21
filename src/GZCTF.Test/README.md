# GZCTF Unit Tests

This project contains unit tests for GZCTF that test individual components in isolation without external dependencies.

## Overview

The unit tests use:
- **xUnit**: Test framework
- **No external dependencies**: All tests run in-memory without databases or HTTP servers

## Prerequisites

- .NET 9 SDK

## Running Tests

### Local Development

```bash
# From the repository root
cd src
dotnet test GZCTF.Test/GZCTF.Test.csproj
```

### With Coverage

```bash
dotnet test GZCTF.Test/GZCTF.Test.csproj /p:CollectCoverage=true
```

### Specific Test

```bash
dotnet test GZCTF.Test/GZCTF.Test.csproj --filter "FullyQualifiedName~CryptoUtilsTests"
```

## Test Structure

### Directory Organization

```
GZCTF.Test/
├── UnitTests/
│   └── Utils/
│       └── CryptoUtilsTests.cs
├── ConfigServiceTest.cs
└── SignatureTest.cs
```

### Test Coverage

1. **CryptoUtilsTests** - Cryptographic utilities (11 tests)
   - SHA256 hashing consistency
   - XOR encoding/decoding reversibility
   - Base64 encoding/decoding
   - UTF8 byte conversion

2. **ConfigServiceTest** - Configuration service tests (6 tests)
   - XOR key retrieval
   - Configuration saving and updating

3. **SignatureTest** - Digital signature validation (2 tests)
   - Ed25519 signature generation and verification

## Adding New Tests

Create new test classes in appropriate subdirectories:
- `UnitTests/Utils/` - Utility function tests
- `UnitTests/Services/` - Service logic tests
- `UnitTests/Repositories/` - Repository tests (with mocked DbContext)

Example:

```csharp
using Xunit;

namespace GZCTF.Test.UnitTests.Utils;

public class MyUtilTests
{
    [Fact]
    public void MyTest()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

## Difference from Integration Tests

- **Unit Tests (this project)**: Fast, isolated, no external dependencies
- **Integration Tests (GZCTF.Integration.Test)**: Requires PostgreSQL database and HTTP server

Use unit tests when possible for faster feedback. Use integration tests when testing:
- API endpoints
- Database operations
- Authentication flows
- Full application stack behavior
