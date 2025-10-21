# .NET Aspire Deployment Guide

This guide covers deploying GZCTF using .NET Aspire for both development and production environments.

## Overview

.NET Aspire provides a cloud-ready stack for building distributed applications with built-in support for:

- **Service Discovery**: Automatic service-to-service communication
- **Telemetry**: Integrated metrics, tracing, and logging with OpenTelemetry
- **Health Checks**: Automated health monitoring for all services
- **Resilience**: Built-in retry policies and circuit breakers

## Architecture

The GZCTF Aspire setup consists of:

1. **GZCTF.AppHost**: Orchestrates all services and resources
2. **GZCTF.ServiceDefaults**: Shared telemetry and service discovery configuration
3. **GZCTF**: Main backend API with integrated ServiceDefaults
4. **PostgreSQL**: Database with volume persistence
5. **Redis**: Cache and SignalR backplane with volume persistence

## Prerequisites

- .NET 9.0 SDK or later
- Docker Desktop (for local development)
- Aspire workload: `dotnet workload install aspire`
- pnpm (for frontend builds): `npm install -g pnpm`

## Development Environment

### Running with Aspire Dashboard

The Aspire Dashboard provides a unified view of all services, logs, traces, and metrics.

```bash
cd src
dotnet run --project GZCTF.AppHost
```

This will:
1. Start the Aspire Dashboard (browser will open automatically)
2. Launch PostgreSQL container with PgAdmin
3. Launch Redis container
4. Build and run the GZCTF backend
5. Configure automatic service discovery and telemetry

**Default URLs:**
- Aspire Dashboard: `https://localhost:17143` or `http://localhost:15128`
- GZCTF API: `http://localhost:8080`
- GZCTF Metrics: `http://localhost:3000`
- PgAdmin: Available through Aspire Dashboard resources

### Environment Variables

Aspire automatically configures these environment variables:

- `ConnectionStrings__Database`: PostgreSQL connection (from Aspire service discovery)
- `ConnectionStrings__RedisCache`: Redis connection (from Aspire service discovery)
- `DOTNET_RUNNING_IN_ASPIRE`: Set to `true` to enable Aspire features
- `OTEL_EXPORTER_OTLP_ENDPOINT`: OpenTelemetry endpoint for the Aspire Dashboard

## Monitoring and Observability

### Available Endpoints

- **Health Check**: `http://gzctf:3000/healthz` - Application health status
- **Metrics**: `http://gzctf:3000/metrics` - Prometheus metrics
- **Traces**: Sent to OTLP endpoint configured via `OTEL_EXPORTER_OTLP_ENDPOINT`

### Monitored Components

The Aspire integration provides telemetry for:

- ✅ ASP.NET Core (requests, routing, middleware)
- ✅ Entity Framework Core (database queries, connections)
- ✅ PostgreSQL (Npgsql instrumentation)
- ✅ Redis (cache operations, SignalR backplane)
- ✅ HTTP Client (outbound requests)
- ✅ Runtime (GC, threads, exceptions)

### Health Checks

Built-in health checks monitor:

- Application liveness
- PostgreSQL database connectivity
- Redis cache availability
- Storage backend accessibility

## Troubleshooting

### Aspire Dashboard Not Opening

Check that ports 17143 (HTTPS) or 15128 (HTTP) are available:

```bash
netstat -an | grep -E "17143|15128"
```

### Service Discovery Issues

Verify environment variables are set:

```bash
dotnet run --project GZCTF.AppHost --launch-profile http
```

Check the Aspire Dashboard for service status and connection strings.

### Database Connection Failures

Ensure PostgreSQL is ready:

```bash
docker logs <postgres-container-id>
```

Check connection string configuration in Aspire Dashboard.

### Telemetry Not Appearing

Verify OTLP endpoint is configured:

```bash
echo $OTEL_EXPORTER_OTLP_ENDPOINT
```

Check OpenTelemetry collector logs if using external collector.

## Benefits of Aspire Integration

1. **Unified Development Experience**: Single command starts all dependencies
2. **Automatic Service Discovery**: No manual configuration of connection strings
3. **Integrated Telemetry**: Zero-config observability with OpenTelemetry
4. **Health Monitoring**: Built-in health checks for all services
5. **Production Ready**: Same configuration works in development and production
6. **Container Orchestration**: Simplified deployment to Kubernetes

## Migration from Traditional Deployment

If migrating from a traditional deployment:

1. Keep existing configuration files - they still work
2. Aspire overrides connection strings via environment variables
3. Existing telemetry configuration is enhanced, not replaced
4. Health checks are additive to existing `/healthz` endpoint

## Additional Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [GZCTF Configuration Guide](https://gzctf.gzti.me/config/appsettings)
- [GZCTF Kubernetes Deployment](https://gzctf.gzti.me/guide/deployment/k8s-only)
- [OpenTelemetry Best Practices](https://opentelemetry.io/docs/collector/deployment/)
