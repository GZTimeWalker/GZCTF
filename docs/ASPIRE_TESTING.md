# Testing .NET Aspire Integration

This document describes how to test the .NET Aspire integration for GZCTF.

## Prerequisites

- .NET 9.0 SDK
- Docker Desktop (running)
- Aspire workload: `dotnet workload install aspire`
- pnpm: `npm install -g pnpm`

## Build Verification

Test that the solution builds with Aspire projects:

```bash
cd src
dotnet build GZCTF.sln
```

Expected: Build succeeds with zero errors.

## Local Development Testing

### Starting with Aspire Dashboard

```bash
cd src
dotnet run --project GZCTF.AppHost
```

This will:
1. Start the Aspire Dashboard (opens in browser automatically)
2. Launch PostgreSQL container
3. Launch Redis container  
4. Build and run GZCTF backend
5. Configure all services with automatic discovery

**Access Points:**
- Aspire Dashboard: https://localhost:17143 (or http://localhost:15128)
- GZCTF API: http://localhost:8080
- Metrics: http://localhost:3000/metrics
- Health: http://localhost:3000/healthz

### Testing Telemetry Collection

1. **Start the AppHost** (as shown above)
2. **Open Aspire Dashboard** at https://localhost:17143
3. **Navigate to the following tabs:**
   - **Resources**: Verify all services (postgres, redis, gzctf) are running
   - **Console Logs**: Check application logs from all services
   - **Structured Logs**: View structured logging with correlation IDs
   - **Traces**: See distributed traces across services
   - **Metrics**: View real-time metrics from all components

4. **Test API and verify telemetry:**
   ```bash
   # Make API request
   curl http://localhost:8080/api/healthz
   
   # Check metrics endpoint
   curl http://localhost:3000/metrics
   ```

5. **Verify in Aspire Dashboard:**
   - Traces tab should show the HTTP request trace
   - Metrics tab should show request counts and latencies
   - Logs tab should show request processing logs

### Testing Service Discovery

Service discovery automatically configures connection strings. Verify:

1. **Check PostgreSQL connection:**
   ```bash
   # View environment variables in Aspire Dashboard
   # Should see: ConnectionStrings__Database with postgres service URL
   ```

2. **Check Redis connection:**
   ```bash
   # Should see: ConnectionStrings__RedisCache with redis service URL
   ```

3. **Verify GZCTF can access services:**
   - Check logs for successful database connection
   - Check logs for successful Redis connection

## CI/CD Testing

### Limitations in CI Environment

The full Aspire Dashboard requires **DCP (Developer Control Plane)**, which is a local Kubernetes implementation. This is typically not available in CI environments like GitHub Actions.

### CI Test Approach

For CI/CD, we test the following:

1. **Build Verification:**
   ```bash
   dotnet build GZCTF.sln
   ```

2. **Project Structure:**
   - Verify AppHost project exists
   - Verify ServiceDefaults project exists
   - Verify proper project references

3. **Configuration Validation:**
   - Check Aspire packages are configured
   - Verify OpenTelemetry instrumentation
   - Validate service configurations

4. **Integration Test Script:**
   ```bash
   # Use the provided integration test script
   ./test-aspire-integration.sh
   ```

### Automated Test Script

A test script is provided for CI validation:

```bash
#!/bin/bash
cd src

# 1. Build test
dotnet build GZCTF.sln -c Release

# 2. Verify projects exist
test -f GZCTF.AppHost/GZCTF.AppHost.csproj
test -f GZCTF.ServiceDefaults/GZCTF.ServiceDefaults.csproj

# 3. Check references
grep -q "ServiceDefaults" GZCTF/GZCTF.csproj

# 4. Verify Aspire packages
grep -q "Aspire.Hosting" Directory.Packages.props

# 5. Check telemetry configuration  
grep -q "OpenTelemetry" GZCTF.ServiceDefaults/Extensions.cs
```

## Manual Testing Checklist

### Basic Functionality

- [ ] Solution builds without errors
- [ ] AppHost starts successfully
- [ ] Aspire Dashboard is accessible
- [ ] PostgreSQL container starts
- [ ] Redis container starts
- [ ] GZCTF backend starts

### Telemetry Verification

- [ ] Metrics are collected and visible in dashboard
- [ ] Traces are captured for HTTP requests
- [ ] Logs appear in structured format
- [ ] Database queries are instrumented
- [ ] Redis operations are instrumented
- [ ] HTTP client calls are traced

### Service Discovery

- [ ] PostgreSQL connection string auto-configured
- [ ] Redis connection string auto-configured
- [ ] GZCTF successfully connects to database
- [ ] GZCTF successfully connects to Redis
- [ ] SignalR backplane uses Redis

### Health Checks

- [ ] `/healthz` endpoint returns healthy status
- [ ] Database health check passes
- [ ] Redis health check passes
- [ ] Health checks visible in Aspire Dashboard

### Backward Compatibility

- [ ] GZCTF runs without Aspire (traditional mode)
- [ ] Existing configuration files still work
- [ ] No breaking changes to API
- [ ] Frontend still functions correctly

## Docker-Only Testing (Alternative)

If Aspire Dashboard isn't available, you can test with direct Docker:

```bash
# Start PostgreSQL
docker run -d --name postgres -e POSTGRES_DB=gzctf -e POSTGRES_PASSWORD=test -p 5432:5432 postgres:16

# Start Redis
docker run -d --name redis -p 6379:6379 redis:7

# Configure and run GZCTF
export ConnectionStrings__Database="Host=localhost;Database=gzctf;Username=postgres;Password=test"
export ConnectionStrings__RedisCache="localhost:6379"
export DOTNET_RUNNING_IN_ASPIRE=false

cd src/GZCTF
dotnet run
```

Then verify:
- Application starts successfully
- Can access http://localhost:8080
- Metrics available at http://localhost:3000/metrics
- Health checks at http://localhost:3000/healthz

## Troubleshooting

### Aspire Dashboard won't start

**Error:** DCP connection failed

**Solution:** Ensure Docker Desktop is running and you have the Aspire workload installed:
```bash
dotnet workload install aspire
```

### Ports already in use

**Error:** Port 8080 or 3000 already in use

**Solution:** Stop conflicting services or change ports in AppHost configuration.

### Services won't start

**Error:** Container creation failed

**Solution:** 
1. Check Docker Desktop is running
2. Ensure you have internet connectivity for pulling images
3. Check Docker has sufficient resources

### Telemetry not appearing

**Issue:** No metrics or traces in dashboard

**Solution:**
1. Ensure services are actually handling requests
2. Check OTEL_EXPORTER_OTLP_ENDPOINT is configured
3. Verify OpenTelemetry instrumentation is enabled
4. Check for errors in console logs

## Production Deployment Testing

For production Kubernetes deployments:

1. **Build container:**
   ```bash
   dotnet publish -c Release -o publish
   docker build -t gzctf:aspire .
   ```

2. **Deploy to test cluster:**
   ```bash
   kubectl apply -f k8s-manifests/
   ```

3. **Verify services:**
   ```bash
   kubectl get pods
   kubectl get services
   ```

4. **Test telemetry export:**
   - Configure OTLP endpoint to your observability platform
   - Verify metrics are exported to Prometheus
   - Verify traces are exported to Jaeger/Tempo
   - Check logs are collected properly

## Documentation

- [Quick Start Guide](../docs/ASPIRE.md)
- [Deployment Guide](../docs/aspire-deployment.md)
- [Implementation Summary](../docs/ASPIRE_SUMMARY.md)
- [Security Analysis](../docs/ASPIRE_SECURITY.md)

## Support

For issues or questions:
- Check documentation first
- Review error messages in console
- Look for similar issues in GitHub
- Ask in Telegram/Discord community
