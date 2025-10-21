# GZCTF Integration Tests

This project contains integration tests for GZCTF that simulate a real environment with a PostgreSQL database and the full application stack.

## Overview

The integration tests use:
- **WebApplicationFactory**: Runs the GZCTF application in-process for testing
- **Testcontainers**: Automatically starts a PostgreSQL container for database testing
- **xUnit**: Test framework with collection fixtures to manage test execution

## Prerequisites

- .NET 9 SDK
- Docker (for Testcontainers)
- pnpm (for frontend asset building)

## Running Tests

### Local Development

```bash
# From the repository root
cd src
dotnet test GZCTF.Integration.Test/GZCTF.Integration.Test.csproj
```

### With Coverage

```bash
dotnet test GZCTF.Integration.Test/GZCTF.Integration.Test.csproj /p:CollectCoverage=true
```

### Specific Test

```bash
dotnet test GZCTF.Integration.Test/GZCTF.Integration.Test.csproj --filter "FullyQualifiedName~BasicApiTests"
```

## Test Structure

### Directory Organization

**Unit Tests (GZCTF.Test)**:
```
GZCTF.Test/
├── UnitTests/
│   └── Utils/
│       └── CryptoUtilsTests.cs  (11 tests)
├── ConfigServiceTest.cs         (6 tests)
└── SignatureTest.cs             (2 tests)
```

**Integration Tests (GZCTF.Integration.Test)**:
```
GZCTF.Integration.Test/
├── Fixtures/                    # Shared test infrastructure
│   ├── GZCTFApplicationFactory.cs
│   └── IntegrationTestCollection.cs
├── Tests/
│   ├── Api/                     # API endpoint tests
│   │   ├── BasicApiTests.cs
│   │   ├── AuthenticationTests.cs
│   │   ├── RoutingTests.cs
│   │   └── OpenApiTests.cs
│   └── Database/                # Database operation tests
│       └── DatabaseContextTests.cs
├── README.md
└── IMPLEMENTATION_SUMMARY.md
```

### Test Categories

- **Unit Tests (GZCTF.Test)**: Fast, isolated tests with no external dependencies (crypto, services, signatures)
- **Integration Tests (GZCTF.Integration.Test)**: Tests requiring real PostgreSQL database and HTTP server

### Test Collections

All tests use the `IntegrationTestCollection` to ensure they don't run in parallel. This is necessary because:
- They share the same database instance
- They share file system resources
- PostgreSQL container is created once per test run

### Current Test Coverage

1. **BasicApiTests** (Tests/Api/): Tests for core API endpoints
   - Server configuration retrieval
   - Authentication/authorization
   - Registration validation
   - OpenAPI specification availability

2. **RoutingTests** (Tests/Api/): Verifies routing and endpoint availability

## Configuration

All test configuration is provided programmatically by the `GZCTFApplicationFactory`:
- Database: PostgreSQL 17 (via Testcontainers with dynamic connection string)
- Storage: Disk storage in temporary test directory
- Rate limiting: Disabled
- Captcha: Disabled
- Environment: Development (enables OpenAPI)
- Email confirmation: Disabled for testing

## Adding New Tests

1. Create a new test class in the appropriate `Tests/` subdirectory
   - API tests go in `Tests/Api/`
   - Infrastructure tests go in `Tests/Infrastructure/`
2. Add the appropriate namespace (e.g., `GZCTF.Integration.Test.Tests.Api`)
3. Import fixtures: `using GZCTF.Integration.Test.Fixtures;`
4. Add `[Collection(nameof(IntegrationTestCollection))]` attribute
5. Inject `GZCTFApplicationFactory` in constructor
6. Create HttpClient: `_client = factory.CreateClient()`

Example:

```csharp
using GZCTF.Integration.Test.Fixtures;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class MyNewTests
{
    private readonly HttpClient _client;
    
    public MyNewTests(GZCTFApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task MyTest()
    {
        var response = await _client.GetAsync("/api/endpoint");
        response.EnsureSuccessStatusCode();
    }
}
```

## Troubleshooting

### Docker Issues

If tests fail with Docker connection errors:
- Ensure Docker is running
- Check Docker socket is accessible: `docker ps`

### Build Issues

If pnpm errors occur:
- Install pnpm: `npm install -g pnpm`
- The tests use Development environment which requires frontend assets

### Port Conflicts

Testcontainers automatically assigns random ports, so conflicts are rare. If they occur, the test run will fail with a port binding error.

## CI/CD

Integration tests run automatically on:
- Push to `develop`, `ci/*`, or `v*` branches
- Pull requests to `develop`
- Manual workflow dispatch

See `.github/workflows/integration-tests.yml` for CI configuration.
