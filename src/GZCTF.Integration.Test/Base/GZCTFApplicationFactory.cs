using GZCTF.Models;
using GZCTF.Services.Container.Manager;
using GZCTF.Services.Container.Provider;
using GZCTF.Storage;
using GZCTF.Storage.Interface;
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

        Console.WriteLine($@"[GZCTFApplicationFactory] Test mode: {testMode}");

        // Initialize containers based on mode
        if (_useMinioStorage)
        {
            Console.WriteLine(@"[GZCTFApplicationFactory] Creating MinIO container...");
            _minioContainer = new MinioBuilder()
                .WithImage("minio/minio:latest")
                .WithUsername("minioadmin")
                .WithPassword("minioadmin")
                .WithCleanUp(true)
                .Build();
        }

        if (_useK3sMode)
        {
            Console.WriteLine(@"[GZCTFApplicationFactory] Creating K3s container...");

            var builder = new K3sBuilder()
                .WithImage("rancher/k3s:v1.34.1-k3s1")
                .WithCleanUp(true);

            _k3sContainer = builder.Build();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Console.WriteLine(@"[ConfigureWebHost] Starting web host configuration...");
        Console.WriteLine($@"[ConfigureWebHost] K3s mode: {_useK3sMode}, Kubeconfig path: {_kubeConfigPath ?? "null"}");
        Console.WriteLine(
            $@"[ConfigureWebHost] MinIO mode: {_useMinioStorage}, Storage connection: {(_storageConnectionString != null ? "set" : "null")}");

        // Set environment variable to allow file directory creation
        Environment.SetEnvironmentVariable("YES_I_KNOW_FILES_ARE_NOT_PERSISTED_GO_AHEAD_PLEASE", "true");

        builder.UseEnvironment("Development");

        // Set content root to a unique test directory to avoid file conflicts
        var testProjectDir = Path.Combine(Directory.GetCurrentDirectory(), $"test-{_testId}");
        Directory.CreateDirectory(testProjectDir);

        // Copy index.html to test wwwroot so fallback works
        var wwwrootDir = Path.Combine(testProjectDir, "wwwroot");
        Directory.CreateDirectory(wwwrootDir);
        var indexPath = Path.Combine(wwwrootDir, "index.html");
        // Create a dummy index.html for testing
        File.WriteAllText(indexPath, "<!DOCTYPE html><html><head><title>Test</title></head><body>Test</body></html>");

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
                ["Server:MetricPort"] = "0"
            };

            // Add K3s-specific configuration if in K3s mode
            if (_useK3sMode && _k3sContainer is not null && !string.IsNullOrEmpty(_kubeConfigPath))
            {
                Console.WriteLine(
                    $@"[ConfigureWebHost] Setting ContainerProvider to Kubernetes with kubeconfig: {_kubeConfigPath}");
                configDict["ContainerProvider:Type"] = "Kubernetes";
                configDict["ContainerProvider:PublicEntry"] = _k3sContainer.IpAddress ?? "localhost";
                configDict["ContainerProvider:KubernetesConfig:Namespace"] = "gzctf-test";
                configDict["ContainerProvider:KubernetesConfig:KubeConfig"] = _kubeConfigPath;
            }
            else
            {
                Console.WriteLine(
                    $@"[ConfigureWebHost] Setting ContainerProvider to Docker (K3s mode: {_useK3sMode}, path: {_kubeConfigPath ?? "null"})");
                configDict["ContainerProvider:Type"] = "Docker";
                configDict["ContainerProvider:PublicEntry"] = "localhost";
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

            // Reconfigure container services if in K3s mode
            if (_useK3sMode && !string.IsNullOrEmpty(_kubeConfigPath))
            {
                Console.WriteLine(@"[ConfigureTestServices] Reconfiguring container services for Kubernetes...");

                // Remove existing container services
                services.RemoveAll(typeof(IContainerManager));
                services.RemoveAll(typeof(IContainerProvider<k8s.Kubernetes, KubernetesMetadata>));
                services.RemoveAll(typeof(IContainerProvider<Docker.DotNet.DockerClient, DockerMetadata>));

                // Add Kubernetes provider and manager
                services.AddSingleton<IContainerProvider<k8s.Kubernetes, KubernetesMetadata>, KubernetesProvider>();
                services.AddSingleton<IContainerManager, KubernetesManager>();

                Console.WriteLine(@"[ConfigureTestServices] Kubernetes container services registered");
            }

            // Reconfigure storage services if in MinIO mode
            if (_useMinioStorage && _minioContainer is not null && !string.IsNullOrEmpty(_storageConnectionString))
            {
                services.RemoveAll(typeof(IBlobStorage));

                services.AddSingleton(StorageProviderFactory.Create(_storageConnectionString));

                Console.WriteLine(@"[ConfigureTestServices] MinIO storage service registered");
            }
        });
    }

    async Task InitializeK3sAsync()
    {
        if (_k3sContainer is null)
            throw new InvalidOperationException("K3s container is not initialized");

        await _k3sContainer.StartAsync();

        Console.WriteLine($@"[InitializeK3sAsync] K3s container IP: {_k3sContainer.IpAddress}");

        var kubeConfigContent = await _k3sContainer.GetKubeconfigAsync();
        var tempDir = Path.Combine(Directory.GetCurrentDirectory(), $"test-{_testId}");
        Directory.CreateDirectory(tempDir);
        _kubeConfigPath = Path.Combine(tempDir, "kubeconfig.yaml");

        await File.WriteAllTextAsync(_kubeConfigPath, kubeConfigContent);
    }

    async Task InitializePostgresAsync()
    {
        await _postgresContainer.StartAsync();
        _connectionString = _postgresContainer.GetConnectionString();
    }

    async Task InitializeMinioAsync()
    {
        if (_minioContainer is null)
            throw new InvalidOperationException("MinIO container is not initialized");

        await _minioContainer.StartAsync();
        var minioEndpoint = _minioContainer.GetConnectionString();
        _storageConnectionString =
            $"minio.s3://bucket=gzctf-test;endpoint={minioEndpoint};accessKey=minioadmin;secretKey=minioadmin;forcePathStyle=true;useHttp=true;";
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine(@"[InitializeAsync] Starting container initialization...");

        var tasks = new List<Task>();

        // Start K3s container first if in K3s mode (before PostgreSQL)
        // This ensures kubeconfig is available during ConfigureWebHost
        if (_useK3sMode && _k3sContainer is not null)
            tasks.Add(InitializeK3sAsync());

        // Start MinIO container if in MinIO mode
        if (_useMinioStorage && _minioContainer is not null)
            tasks.Add(InitializeMinioAsync());

        // Start PostgresSQL container
        tasks.Add(InitializePostgresAsync());

        await Task.WhenAll(tasks);

        // Set environment variable so it's available during configuration
        Environment.SetEnvironmentVariable("GZCTF_ConnectionStrings__Database", _connectionString);

        // Ensure the database is created and migrated
        // We do this after container is fully started
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(_connectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
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
    }
}

/// <summary>
/// Collection to ensure tests don't run in parallel (share the same factory instance)
/// </summary>
[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<GZCTFApplicationFactory>;
