# .NET Aspire Integration Summary

This document summarizes the .NET Aspire integration implemented for GZCTF.

## Changes Made

### 1. New Projects

#### GZCTF.AppHost
- **Location**: `src/GZCTF.AppHost/`
- **Purpose**: Orchestrates all services and resources using .NET Aspire
- **Key Features**:
  - PostgreSQL database with automatic volume management
  - Redis cache and SignalR backplane with persistence
  - PgAdmin (development only)
  - Automatic service discovery and connection string injection
  - Development and production environment support

#### GZCTF.ServiceDefaults
- **Location**: `src/GZCTF.ServiceDefaults/`
- **Purpose**: Shared telemetry, health checks, and service discovery configuration
- **Key Features**:
  - OpenTelemetry integration (metrics, traces, logs)
  - Enhanced instrumentation for:
    - ASP.NET Core
    - Entity Framework Core
    - PostgreSQL (Npgsql)
    - Redis
    - HTTP Client
    - .NET Runtime
  - Automatic health checks
  - Service discovery support
  - HTTP client resilience patterns

### 2. Modified Projects

#### GZCTF (Main Backend)
- **Changes**:
  - Added reference to `GZCTF.ServiceDefaults`
  - Integrated Aspire service defaults when running under Aspire
  - Conditional activation via `DOTNET_RUNNING_IN_ASPIRE` environment variable
  - Maintains backward compatibility with traditional deployment

#### Solution File
- **Changes**:
  - Added `GZCTF.AppHost` project
  - Added `GZCTF.ServiceDefaults` project
  - Updated project references and dependencies

#### Directory.Packages.props
- **Added Packages**:
  - `Aspire.Hosting.AppHost` (8.2.2)
  - `Aspire.Hosting.PostgreSQL` (8.2.2)
  - `Aspire.Hosting.Redis` (8.2.2)
  - `Microsoft.Extensions.Http.Resilience` (9.10.0)
  - `Microsoft.Extensions.ServiceDiscovery` (8.2.2)

### 3. Documentation

#### docs/ASPIRE.md
- Quick start guide for .NET Aspire
- Feature overview
- Benefits comparison
- Troubleshooting guide

#### docs/aspire-deployment.md
- Comprehensive deployment guide
- Development environment setup
- Production deployment scenarios
- Kubernetes configuration examples
- OpenTelemetry collector setup
- Monitoring and observability guide

### 4. Configuration Files

#### .gitignore
- Updated to allow AppHost configuration files
- Maintains exclusion of other appsettings.json files

#### global.json
- Updated SDK version to 9.0.305 for compatibility

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  Aspire Dashboard                       │
│  (Logs, Traces, Metrics, Resource Management)          │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                   GZCTF.AppHost                         │
│  (Service Orchestration & Configuration)                │
└─────────────────────────────────────────────────────────┘
        │                   │                    │
        ▼                   ▼                    ▼
┌───────────────┐  ┌────────────────┐  ┌─────────────────┐
│   PostgreSQL  │  │     Redis      │  │  GZCTF Backend  │
│  (Database)   │  │ (Cache/SignalR)│  │   + Frontend    │
└───────────────┘  └────────────────┘  └─────────────────┘
        │                   │                    │
        └───────────────────┴────────────────────┘
                            │
                            ▼
                  ┌─────────────────┐
                  │ ServiceDefaults │
                  │   (Telemetry)   │
                  └─────────────────┘
```

## Integration Points

### 1. Service Discovery
- PostgreSQL connection string automatically injected via `ConnectionStrings__Database`
- Redis connection string automatically injected via `ConnectionStrings__RedisCache`
- No manual configuration required in development

### 2. Telemetry
- OpenTelemetry automatically configured when `OTEL_EXPORTER_OTLP_ENDPOINT` is set
- Metrics available at `/metrics` (port 3000)
- Traces sent to Aspire Dashboard or configured OTLP endpoint
- Enhanced instrumentation for database, cache, and HTTP operations

### 3. Health Checks
- Built-in health checks at `/healthz` (port 3000)
- Monitors:
  - Application liveness
  - PostgreSQL connectivity
  - Redis availability
  - Storage backend health

### 4. Environment Detection
- Aspire features activate when:
  - `DOTNET_RUNNING_IN_ASPIRE=true` environment variable is set
  - OR environment name contains "Aspire"
- Existing telemetry configuration is preserved and enhanced

## Usage

### Development (Aspire)
```bash
cd src
dotnet run --project GZCTF.AppHost
```

### Development (Traditional)
```bash
cd src/GZCTF
dotnet run
```

### Production (Container)
```bash
cd src
dotnet publish GZCTF/GZCTF.csproj -c Release -o publish
docker build -t gzctf:aspire -f GZCTF/Dockerfile .
```

## Benefits

### For Developers
- ✅ Single command starts entire stack
- ✅ No manual service configuration
- ✅ Unified dashboard for all logs and metrics
- ✅ Faster iteration and debugging
- ✅ Automatic container lifecycle management

### For Operations
- ✅ Production-ready telemetry out of the box
- ✅ Comprehensive health monitoring
- ✅ Service discovery eliminates hard-coded URLs
- ✅ Kubernetes manifest generation support
- ✅ Consistent configuration across environments

### For Observability
- ✅ Distributed tracing across all components
- ✅ Metrics for database, cache, HTTP, runtime
- ✅ Structured logging with correlation IDs
- ✅ Performance profiling capabilities
- ✅ OpenTelemetry standard compliance

## Backward Compatibility

The Aspire integration is **100% backward compatible**:

1. **Traditional deployment continues to work** without any changes
2. **Existing configuration files** are respected
3. **Aspire features are opt-in** via environment variables
4. **No breaking changes** to the API or frontend
5. **Tests continue to run** without modification

## Testing

### Build Verification
```bash
cd src
dotnet build GZCTF.sln
```

### AppHost Verification
```bash
cd src
dotnet run --project GZCTF.AppHost
```

### Traditional Launch Verification
```bash
cd src/GZCTF
dotnet run
```

## Known Limitations

1. **Kubernetes Hosting Package**: The `Aspire.Hosting.Kubernetes` package is in preview and not yet stable (8.2.2). Manual Kubernetes manifest generation is documented as an alternative.

2. **Container Requirements**: Running AppHost requires Docker Desktop to be running.

3. **Port Requirements**: Aspire Dashboard requires ports 17143 (HTTPS) or 15128 (HTTP) to be available.

## Future Enhancements

Potential future improvements:

1. **Azure Container Apps** deployment templates
2. **AWS ECS** deployment templates
3. **Helm charts** generated from Aspire configuration
4. **Custom health check** implementations
5. **Advanced resilience** patterns (circuit breakers, retries)
6. **Service mesh** integration

## Support

For questions or issues:

- 📖 Review [docs/ASPIRE.md](./ASPIRE.md) for quick start
- 📚 Read [docs/aspire-deployment.md](./aspire-deployment.md) for deployment
- 💬 Join [Telegram Group](https://telegram.dog/gzctf)
- 🐛 File issues on [GitHub](https://github.com/GZTimeWalker/GZCTF/issues)

## References

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [GZCTF Documentation](https://gzctf.gzti.me/)
- [ASP.NET Core](https://learn.microsoft.com/aspnet/core)

---

**Implementation Date**: October 2025  
**Aspire Version**: 8.2.2  
**Target Framework**: .NET 9.0
