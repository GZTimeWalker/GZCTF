using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Manager;
using GZCTF.Services.Container.Provider;
using GZCTF.Services.Container.ThirdParty;
using GZCTF.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Test.UnitTests.Services;

/// <summary>
/// Tests for ThirdParty container provider and manager
/// </summary>
public class ThirdPartyProviderTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly MockV1Server _mockServer;
    private readonly HttpClient _httpClient;
    private readonly int _serverPort;

    public ThirdPartyProviderTests(ITestOutputHelper output)
    {
        _output = output;
        _mockServer = new MockV1Server();
        _serverPort = _mockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{_serverPort}") };
    }

    public void Dispose()
    {
        _mockServer.Dispose();
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ThirdPartyProvider_WithMissingBaseUrl_ThrowsException()
    {
        // Arrange
        var options = Options.Create(new ContainerProvider
        {
            Type = ContainerProviderType.ThirdParty,
            ThirdPartyConfig = new ThirdPartyConfig { BaseUrl = "" }
        });

        var logger = new TestLogger<ThirdPartyProvider>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ThirdPartyProvider(options, logger));

        Assert.Contains("base URL", exception.Message);
    }

    [Fact]
    public void ThirdPartyProvider_WithValidConfig_InitializesSuccessfully()
    {
        // Arrange
        var options = Options.Create(new ContainerProvider
        {
            Type = ContainerProviderType.ThirdParty,
            PortMappingType = ContainerPortMappingType.Default,
            PublicEntry = "test.example.com",
            ThirdPartyConfig = new ThirdPartyConfig
            {
                BaseUrl = $"http://localhost:{_serverPort}",
                ApiVersion = ThirdPartyApiVersion.V1,
                Timeout = 30,
                VerifyTls = false
            }
        });

        var logger = new TestLogger<ThirdPartyProvider>();

        // Act
        var provider = new ThirdPartyProvider(options, logger);
        var metadata = provider.GetMetadata();
        var client = provider.GetProvider();

        // Assert
        Assert.NotNull(metadata);
        Assert.NotNull(client);
        Assert.Equal(ThirdPartyApiVersion.V1, client.ApiVersion);
        Assert.Equal(ContainerPortMappingType.Default, metadata.PortMappingType);
        Assert.Equal("test.example.com", metadata.PublicEntry);
    }

    [Fact]
    public async Task ThirdPartyClient_CreateContainer_SuccessfullyCreatesContainer()
    {
        // Arrange
        var options = CreateDefaultOptions(exposePort: false);
        var providerLogger = new TestLogger<ThirdPartyProvider>();
        var managerLogger = new TestLogger<ThirdPartyManager>();

        var provider = new ThirdPartyProvider(options, providerLogger);
        var manager = new ThirdPartyManager(provider, managerLogger);

        var config = new ContainerConfig
        {
            Image = "test-image:latest",
            ExposedPort = 8080,
            CPUCount = 1,
            MemoryLimit = 512,
            StorageLimit = 1024,
            TeamId = "team-123",
            UserId = Guid.NewGuid(),
            ChallengeId = 42,
            Flag = "flag{test_flag_12345}",
            NetworkMode = NetworkMode.Open
        };

        // Act
        var container = await manager.CreateContainerAsync(config);

        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.ContainerId);
        Assert.Equal(config.Image, container.Image);
        Assert.Equal("192.168.1.100", container.IP);
        Assert.Equal(8080, container.Port);
        Assert.True(container.IsProxy);
        Assert.Equal(ContainerStatus.Running, container.Status);

        _output.WriteLine($"Container created: {container.ContainerId}");
        _output.WriteLine($"Container IP: {container.IP}:{container.Port}");
    }

    [Fact]
    public async Task ThirdPartyClient_CreateContainer_WithExposedPort_ReturnsPublicAddress()
    {
        // Arrange
        var options = CreateDefaultOptions(exposePort: true);
        var providerLogger = new TestLogger<ThirdPartyProvider>();
        var managerLogger = new TestLogger<ThirdPartyManager>();

        var provider = new ThirdPartyProvider(options, providerLogger);
        var manager = new ThirdPartyManager(provider, managerLogger);

        var config = new ContainerConfig
        {
            Image = "test-image:latest",
            ExposedPort = 8080,
            CPUCount = 1,
            MemoryLimit = 512,
            StorageLimit = 1024,
            TeamId = "team-456",
            UserId = Guid.NewGuid(),
            ChallengeId = 43,
            Flag = "flag{test_flag_67890}",
            NetworkMode = NetworkMode.Open
        };

        // Act
        var container = await manager.CreateContainerAsync(config);

        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.ContainerId);
        Assert.Equal("203.0.113.10", container.PublicIP);
        Assert.Equal(30080, container.PublicPort);
        Assert.False(container.IsProxy);

        _output.WriteLine($"Container public address: {container.PublicIP}:{container.PublicPort}");
    }

    [Fact]
    public async Task ThirdPartyClient_CreateContainer_WithoutExternalAddress_WhenExposePort_ReturnsNull()
    {
        // Arrange
        _mockServer.SetExternalAddressEnabled(false); // Disable external address in mock server

        var options = CreateDefaultOptions(exposePort: true);
        var providerLogger = new TestLogger<ThirdPartyProvider>();
        var managerLogger = new TestLogger<ThirdPartyManager>();

        var provider = new ThirdPartyProvider(options, providerLogger);
        var manager = new ThirdPartyManager(provider, managerLogger);

        var config = new ContainerConfig
        {
            Image = "test-image:latest",
            ExposedPort = 8080,
            CPUCount = 1,
            MemoryLimit = 512,
            StorageLimit = 1024,
            TeamId = "team-789",
            UserId = Guid.NewGuid(),
            ChallengeId = 44,
            NetworkMode = NetworkMode.Open
        };

        // Act
        var container = await manager.CreateContainerAsync(config);

        // Assert
        Assert.Null(container);

        _mockServer.SetExternalAddressEnabled(true); // Re-enable for other tests
    }

    [Fact]
    public async Task ThirdPartyClient_CreateContainer_WithFailedState_ReturnsNull()
    {
        // Arrange
        _mockServer.SetContainerState(ThirdPartyContainerState.Failed);

        var options = CreateDefaultOptions(exposePort: false);
        var providerLogger = new TestLogger<ThirdPartyProvider>();
        var managerLogger = new TestLogger<ThirdPartyManager>();

        var provider = new ThirdPartyProvider(options, providerLogger);
        var manager = new ThirdPartyManager(provider, managerLogger);

        var config = new ContainerConfig
        {
            Image = "test-image:latest",
            ExposedPort = 8080,
            CPUCount = 1,
            MemoryLimit = 512,
            StorageLimit = 1024,
            TeamId = "team-error",
            UserId = Guid.NewGuid(),
            ChallengeId = 45,
            NetworkMode = NetworkMode.Open
        };

        // Act
        var container = await manager.CreateContainerAsync(config);

        // Assert
        Assert.Null(container);

        _mockServer.SetContainerState(ThirdPartyContainerState.Running); // Reset for other tests
    }

    [Fact]
    public async Task ThirdPartyClient_CreateContainer_WithHttpError_ReturnsNull()
    {
        // Arrange
        _mockServer.SetHttpErrorCode(HttpStatusCode.InternalServerError);

        var options = CreateDefaultOptions(exposePort: false);
        var providerLogger = new TestLogger<ThirdPartyProvider>();
        var managerLogger = new TestLogger<ThirdPartyManager>();

        var provider = new ThirdPartyProvider(options, providerLogger);
        var manager = new ThirdPartyManager(provider, managerLogger);

        var config = new ContainerConfig
        {
            Image = "test-image:latest",
            ExposedPort = 8080,
            CPUCount = 1,
            MemoryLimit = 512,
            StorageLimit = 1024,
            TeamId = "team-http-error",
            UserId = Guid.NewGuid(),
            ChallengeId = 46,
            NetworkMode = NetworkMode.Open
        };

        // Act
        var container = await manager.CreateContainerAsync(config);

        // Assert
        Assert.Null(container);

        _mockServer.SetHttpErrorCode(null); // Reset for other tests
    }

    [Fact]
    public async Task ThirdPartyClient_DestroyContainer_SuccessfullyDestroysContainer()
    {
        // Arrange
        var options = CreateDefaultOptions(exposePort: false);
        var providerLogger = new TestLogger<ThirdPartyProvider>();
        var managerLogger = new TestLogger<ThirdPartyManager>();

        var provider = new ThirdPartyProvider(options, providerLogger);
        var manager = new ThirdPartyManager(provider, managerLogger);

        var container = new GZCTF.Models.Data.Container
        {
            ContainerId = "test-container-id",
            Image = "test-image:latest",
            IP = "192.168.1.100",
            Port = 8080,
            Status = ContainerStatus.Running
        };

        // Act
        await manager.DestroyContainerAsync(container);

        // Assert
        Assert.Equal(ContainerStatus.Destroyed, container.Status);

        _output.WriteLine($"Container destroyed: {container.ContainerId}");
    }

    [Fact]
    public async Task ThirdPartyClient_DestroyContainer_WithNonExistentContainer_SetsDestroyedStatus()
    {
        // Arrange
        _mockServer.SetHttpErrorCode(HttpStatusCode.NotFound);

        var options = CreateDefaultOptions(exposePort: false);
        var providerLogger = new TestLogger<ThirdPartyProvider>();
        var managerLogger = new TestLogger<ThirdPartyManager>();

        var provider = new ThirdPartyProvider(options, providerLogger);
        var manager = new ThirdPartyManager(provider, managerLogger);

        var container = new GZCTF.Models.Data.Container
        {
            ContainerId = "non-existent-container",
            Image = "test-image:latest",
            IP = "192.168.1.100",
            Port = 8080,
            Status = ContainerStatus.Running
        };

        // Act
        await manager.DestroyContainerAsync(container);

        // Assert
        Assert.Equal(ContainerStatus.Destroyed, container.Status);

        _mockServer.SetHttpErrorCode(null); // Reset for other tests
    }

    [Fact]
    public async Task ThirdPartyClient_RequestId_IsSentToServer()
    {
        // Arrange
        var options = CreateDefaultOptions(exposePort: false);
        var providerLogger = new TestLogger<ThirdPartyProvider>();

        var provider = new ThirdPartyProvider(options, providerLogger);
        var client = provider.GetProvider();

        var config = new ContainerConfig
        {
            Image = "test-image:latest",
            ExposedPort = 8080,
            CPUCount = 1,
            MemoryLimit = 512,
            StorageLimit = 1024,
            TeamId = "team-request-id",
            UserId = Guid.NewGuid(),
            ChallengeId = 47,
            NetworkMode = NetworkMode.Open
        };

        var requestId = "custom-request-id-12345";

        // Act
        var response = await client.CreateAsync(config, requestId);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(requestId, _mockServer.LastRequestId);

        _output.WriteLine($"Request ID sent: {requestId}");
    }

    [Fact]
    public void ThirdPartyClient_WithMismatchedApiVersion_ThrowsException()
    {
        // Arrange
        var options = Options.Create(new ContainerProvider
        {
            Type = ContainerProviderType.ThirdParty,
            ThirdPartyConfig = new ThirdPartyConfig
            {
                BaseUrl = $"http://localhost:{_serverPort}",
                ApiVersion = (ThirdPartyApiVersion)999, // Invalid version
                Timeout = 30,
                VerifyTls = false
            }
        });

        var logger = new TestLogger<ThirdPartyProvider>();

        // Act & Assert
        // The provider should throw during initialization when creating the client
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ThirdPartyProvider(options, logger));

        Assert.Contains("version", exception.Message.ToLower());
    }

    private IOptions<ContainerProvider> CreateDefaultOptions(bool exposePort) =>
        Options.Create(new ContainerProvider
        {
            Type = ContainerProviderType.ThirdParty,
            PortMappingType = exposePort ? ContainerPortMappingType.Default : ContainerPortMappingType.PlatformProxy,
            PublicEntry = "203.0.113.10",
            ThirdPartyConfig = new ThirdPartyConfig
            {
                BaseUrl = $"http://localhost:{_serverPort}",
                ApiVersion = ThirdPartyApiVersion.V1,
                Timeout = 30,
                VerifyTls = false
            }
        });
}

/// <summary>
/// Mock V1 Server for testing ThirdParty API
/// </summary>
internal class MockV1Server : IDisposable
{
    private readonly HttpListener _listener = new();
    private int _port;
    private bool _externalAddressEnabled = true;
    private ThirdPartyContainerState _containerState = ThirdPartyContainerState.Running;
    private HttpStatusCode? _httpErrorCode;

    public string? LastRequestId { get; private set; }

    public int Start()
    {
        // Find an available port by letting the OS assign one
        using var tempListener = new TcpListener(IPAddress.Loopback, 0);
        tempListener.Start();
        _port = ((IPEndPoint)tempListener.LocalEndpoint).Port;
        tempListener.Stop();

        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();

        Task.Run(HandleRequests);
        return _port;
    }

    public void SetExternalAddressEnabled(bool enabled) => _externalAddressEnabled = enabled;
    public void SetContainerState(ThirdPartyContainerState state) => _containerState = state;
    public void SetHttpErrorCode(HttpStatusCode? code) => _httpErrorCode = code;

    private async Task HandleRequests()
    {
        while (_listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                await ProcessRequest(context);
            }
            catch (Exception)
            {
                // Listener stopped
                break;
            }
        }
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        // Extract request ID from header
        LastRequestId = request.Headers["X-Request-Id"];

        // Return error if configured
        if (_httpErrorCode.HasValue)
        {
            response.StatusCode = (int)_httpErrorCode.Value;
            response.Close();
            return;
        }

        // Handle different endpoints
        if (request.Url?.AbsolutePath == "/v1/containers" && request.HttpMethod == "POST")
        {
            await HandleCreateContainer(request, response);
        }
        else if (request.Url?.AbsolutePath.StartsWith("/v1/containers/") == true &&
                 request.HttpMethod == "DELETE")
        {
            await HandleDestroyContainer(response);
        }
        else
        {
            response.StatusCode = 404;
            response.Close();
        }
    }

    private async Task HandleCreateContainer(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        await reader.ReadToEndAsync();

        var createResponse = new ThirdPartyV1.CreateResponse
        {
            Id = $"container-{Guid.NewGuid():N}",
            State = _containerState,
            InternalAddress = new ThirdPartyAddress { Ip = "192.168.1.100", Port = 8080 },
            ExternalAddress = _externalAddressEnabled
                ? new ThirdPartyAddress { Ip = "203.0.113.10", Port = 30080 }
                : null,
            StartedAt = DateTimeOffset.UtcNow,
            ExpectStopAt = DateTimeOffset.UtcNow.AddHours(2)
        };

        var json = JsonSerializer.Serialize(createResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        response.StatusCode = 200;
        response.ContentType = "application/json";
        await using var writer = new StreamWriter(response.OutputStream);
        await writer.WriteAsync(json);
    }

    private async Task HandleDestroyContainer(HttpListenerResponse response)
    {
        response.StatusCode = 200;
        await Task.CompletedTask;
        response.Close();
    }

    public void Dispose()
    {
        _listener.Stop();
        _listener.Close();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Simple test logger for capturing log messages in tests
/// </summary>
internal class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // In a real implementation, this could store log messages for assertion
        // For now, we just ensure the logger doesn't throw
    }
}
