# GZCTF with .NET Aspire

This document provides a quick start guide for running GZCTF with .NET Aspire.

## What is .NET Aspire?

.NET Aspire is an opinionated, cloud-ready stack for building observable, production-ready distributed applications. It provides:

- **Automatic service discovery** for PostgreSQL and Redis
- **Integrated telemetry** with OpenTelemetry (metrics, traces, logs)
- **Unified dashboard** for monitoring all services
- **Health checks** for all components
- **Production-ready** configuration for Kubernetes

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- Docker Desktop
- Aspire workload: `dotnet workload install aspire`
- pnpm: `npm install -g pnpm`

### Running with Aspire

```bash
cd src
dotnet run --project GZCTF.AppHost
```

This single command will:
1. ✅ Launch the Aspire Dashboard (opens in browser)
2. ✅ Start PostgreSQL with PgAdmin
3. ✅ Start Redis for caching and SignalR
4. ✅ Build and run GZCTF backend
5. ✅ Configure all connection strings automatically
6. ✅ Enable telemetry collection

### Accessing Services

After running the AppHost:

- **Aspire Dashboard**: https://localhost:17143 or http://localhost:15128
- **GZCTF API**: http://localhost:8080
- **Metrics**: http://localhost:3000/metrics
- **Health**: http://localhost:3000/healthz
- **PgAdmin**: Available via Aspire Dashboard

## Features

### Automatic Configuration

No need to manually configure connection strings! Aspire automatically:

- Injects PostgreSQL connection string
- Configures Redis for cache and SignalR backplane
- Sets up OpenTelemetry endpoints
- Manages container lifecycle

### Unified Dashboard

The Aspire Dashboard provides:

- Real-time logs from all services
- Distributed traces across components
- Metrics and performance data
- Service health status
- Resource management

### Enhanced Telemetry

Built-in instrumentation for:

- ✅ ASP.NET Core (requests, middleware, routing)
- ✅ Entity Framework Core (queries, connections)
- ✅ PostgreSQL (Npgsql operations)
- ✅ Redis (cache operations, SignalR)
- ✅ HTTP Client (outbound requests)
- ✅ .NET Runtime (GC, threads, exceptions)

### Health Monitoring

Automatic health checks for:

- Application liveness
- PostgreSQL database connectivity
- Redis cache availability
- Storage backend health

## Configuration

### Development

Aspire uses the default development configuration. Connection strings are automatically injected via service discovery.

### Production

For production deployment, see the full [Aspire Deployment Guide](./aspire-deployment.md).

## Troubleshooting

### Port Conflicts

If ports 17143 or 15128 are in use:

```bash
# Stop the conflicting service or change ports in launchSettings.json
netstat -an | grep -E "17143|15128"
```

### Docker Not Running

Ensure Docker Desktop is running:

```bash
docker ps
```

### Build Errors

If you encounter build errors:

```bash
# Clean and rebuild
cd src
dotnet clean
dotnet restore
dotnet build
```

## Documentation

- 📖 [Full Aspire Deployment Guide](./aspire-deployment.md)
- 🌐 [GZCTF Documentation](https://gzctf.gzti.me/)
- 🔧 [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)

## Benefits

✅ **Single Command**: Start entire stack with one command  
✅ **No Manual Config**: Connection strings auto-configured  
✅ **Observability**: Full telemetry out of the box  
✅ **Development Speed**: Faster iteration with unified dashboard  
✅ **Production Ready**: Same setup for dev and production  
✅ **Container Management**: Automatic lifecycle management

## Comparison

| Feature | Traditional Setup | With Aspire |
|---------|------------------|-------------|
| Setup Time | 15-30 minutes | 2 minutes |
| Connection Config | Manual | Automatic |
| Telemetry Setup | Manual | Built-in |
| Dashboard | Separate tools | Unified |
| Container Lifecycle | Manual | Automatic |
| Service Discovery | Not available | Built-in |
| Health Checks | Manual setup | Automatic |

## Next Steps

1. Try the [Quick Start](#quick-start)
2. Explore the [Aspire Dashboard](https://localhost:17143)
3. Review [telemetry data](http://localhost:3000/metrics)
4. Read the [full deployment guide](./aspire-deployment.md)
5. Deploy to [production](./aspire-deployment.md#production-deployment)

---

**Note**: This is an optional enhancement to GZCTF. The traditional deployment methods still work perfectly. Aspire is recommended for developers who want faster iteration and better observability.
