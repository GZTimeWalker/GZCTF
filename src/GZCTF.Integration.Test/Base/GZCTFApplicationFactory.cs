using GZCTF.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.K3s;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;
using Xunit;

namespace GZCTF.Integration.Test.Base;

/// <summary>
/// Test application factory for integration tests with PostgresSQL test container
/// Supports two modes via GZCTF_INTEGRATION_TEST_MODE environment variable:
/// - "local" (default): Docker + local disk storage
/// - "cloud": K3s + MinIO (for cloud-native testing)
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable once InconsistentNaming
public class GZCTFApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("gzctf_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    private readonly MinioContainer? _minioContainer;
    private readonly K3sContainer? _k3sContainer;

    private readonly bool _useK3sMode;
    private readonly bool _useMinioStorage;

    private string? _connectionString;
    private string? _storageConnectionString;
    private string? _kubeConfigPath;
    private readonly string _testId = Guid.NewGuid().ToString("N")[..8];

    public GZCTFApplicationFactory()
    {
        // Check environment variable to determine test mode
        var testMode = Environment.GetEnvironmentVariable("GZCTF_INTEGRATION_TEST_MODE")?.ToLowerInvariant() ?? "local";

        _useK3sMode = testMode == "cloud" || testMode == "k3s";
        _useMinioStorage = testMode == "cloud" || testMode == "minio";

        Console.WriteLine($"[GZCTFApplicationFactory] Test mode: {testMode}");

        // Initialize containers based on mode
        if (_useMinioStorage)
        {
            Console.WriteLine("[GZCTFApplicationFactory] Creating MinIO container...");
            _minioContainer = new MinioBuilder()
                .WithImage("minio/minio:latest")
                .WithUsername("minioadmin")
                .WithPassword("minioadmin")
                .WithCleanUp(true)
                .Build();
        }

        if (_useK3sMode)
        {
            Console.WriteLine("[GZCTFApplicationFactory] Creating K3s container...");
            _k3sContainer = new K3sBuilder()
                .WithImage("rancher/k3s:v1.34.1-k3s1")
                .WithCleanUp(true)
                .Build();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Console.WriteLine("[ConfigureWebHost] Starting web host configuration...");
        Console.WriteLine($"[ConfigureWebHost] K3s mode: {_useK3sMode}, Kubeconfig path: {_kubeConfigPath ?? "null"}");
        Console.WriteLine($"[ConfigureWebHost] MinIO mode: {_useMinioStorage}, Storage connection: {(_storageConnectionString != null ? "set" : "null")}");

        // Set environment variable to allow file directory creation
        Environment.SetEnvironmentVariable("YES_I_KNOW_FILES_ARE_NOT_PERSISTED_GO_AHEAD_PLEASE", "true");

        builder.UseEnvironment("Development");

        // Set content root to a unique test directory to avoid file conflicts
        var testProjectDir = Path.Combine(Directory.GetCurrentDirectory(), $"test-{_testId}");
        Directory.CreateDirectory(testProjectDir);

        builder.UseContentRoot(testProjectDir);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Clear existing sources and rebuild with our connection string first
            // This ensures it's available during ConfigureDatabase()
            var sources = config.Sources.ToList();
            config.Sources.Clear();

            // Build base configuration
            var configDict = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _connectionString,
                ["ConnectionStrings:Storage"] = _storageConnectionString ?? "disk://path=./files/test",
                ["DisableRateLimit"] = "true",
                ["CaptchaConfig:Provider"] = "None",
                ["XorKey"] = "integration-test-xor-key",
                ["AccountPolicy:AllowRegister"] = "true",
                ["AccountPolicy:RequireEmailConfirmation"] = "false",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning",
                ["Server:MetricPort"] = "0",
                ["ContainerProvider:PublicEntry"] = "localhost",
            };

            // Add K3s-specific configuration if in K3s mode
            if (_useK3sMode && !string.IsNullOrEmpty(_kubeConfigPath))
            {
                Console.WriteLine($"[ConfigureWebHost] Setting ContainerProvider to Kubernetes with kubeconfig: {_kubeConfigPath}");
                configDict["ContainerProvider:Type"] = "Kubernetes";
                configDict["ContainerProvider:KubernetesConfig:Namespace"] = "gzctf-test";
                configDict["ContainerProvider:KubernetesConfig:KubeConfig"] = _kubeConfigPath;
            }
            else
            {
                Console.WriteLine($"[ConfigureWebHost] Setting ContainerProvider to Docker (K3s mode: {_useK3sMode}, path: {_kubeConfigPath ?? "null"})");
                configDict["ContainerProvider:Type"] = "Docker";
            }
            config.AddInMemoryCollection(configDict);

            // Then add back all the original sources
            foreach (var source in sources)
            {
                config.Sources.Add(source);
            }
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace the DbContext with our test connection string
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_connectionString,
                    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("[InitializeAsync] Starting container initialization...");

        // Start K3s container first if in K3s mode (before PostgreSQL)
        // This ensures kubeconfig is available during ConfigureWebHost
        if (_useK3sMode && _k3sContainer is not null)
        {
            Console.WriteLine("[InitializeAsync] Starting K3s container...");
            await _k3sContainer.StartAsync();
            Console.WriteLine("[InitializeAsync] K3s container started successfully");

            // Get kubeconfig and save to a temporary file
            var kubeConfigContent = await _k3sContainer.GetKubeconfigAsync();
            var tempDir = Path.Combine(Directory.GetCurrentDirectory(), $"test-{_testId}");
            Directory.CreateDirectory(tempDir);
            _kubeConfigPath = Path.Combine(tempDir, "kubeconfig.yaml");
            await File.WriteAllTextAsync(_kubeConfigPath, kubeConfigContent);
            Console.WriteLine($"[InitializeAsync] Kubeconfig saved to: {_kubeConfigPath}");
        }

        // Start PostgresSQL container
        Console.WriteLine("[InitializeAsync] Starting PostgreSQL container...");
        await _postgresContainer.StartAsync();
        Console.WriteLine("[InitializeAsync] PostgreSQL container started successfully");

        // Get and cache connection string
        _connectionString = _postgresContainer.GetConnectionString();

        // Set environment variable so it's available during configuration
        Environment.SetEnvironmentVariable("GZCTF_ConnectionStrings__Database", _connectionString);

        // Start MinIO container if in MinIO mode
        if (_useMinioStorage && _minioContainer is not null)
        {
            Console.WriteLine("[InitializeAsync] Starting MinIO container...");
            await _minioContainer.StartAsync();
            Console.WriteLine("[InitializeAsync] MinIO container started successfully");

            // Build MinIO connection string in S3 format
            var minioEndpoint = _minioContainer.GetConnectionString();
            _storageConnectionString = $"minio.s3://bucket=gzctf-test;endpoint={minioEndpoint};accessKey=minioadmin;secretKey=minioadmin;forcePathStyle=true;useHttp=true;";
            Console.WriteLine($"[InitializeAsync] MinIO endpoint: {minioEndpoint}");
        }

        // Ensure the database is created and migrated
        // We do this after container is fully started
        Console.WriteLine("[InitializeAsync] Running database migrations...");
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(_connectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();
        Console.WriteLine("[InitializeAsync] Initialization complete!");
    }

    public new async Task DisposeAsync()
    {
        Console.WriteLine("[DisposeAsync] Cleaning up containers...");

        // Dispose containers
        await _postgresContainer.DisposeAsync();

        if (_minioContainer is not null)
            await _minioContainer.DisposeAsync();

        if (_k3sContainer is not null)
            await _k3sContainer.DisposeAsync();

        await base.DisposeAsync();

        // Clean up test directory
        var testProjectDir = Path.Combine(Directory.GetCurrentDirectory(), $"test-{_testId}");
        if (Directory.Exists(testProjectDir))
        {
            try
            {
                Directory.Delete(testProjectDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        Console.WriteLine("[DisposeAsync] Cleanup complete");
    }
}

/// <summary>
/// Collection to ensure tests don't run in parallel (share the same factory instance)
/// </summary>
[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<GZCTFApplicationFactory>;
