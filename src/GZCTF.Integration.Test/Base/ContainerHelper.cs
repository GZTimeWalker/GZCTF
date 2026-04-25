using System.Net.Sockets;
using System.Net.WebSockets;
using Docker.DotNet;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Services.Container.Provider;
using k8s;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Base;

/// <summary>
/// Unified container helper for both Docker and Kubernetes environments.
/// Handles container lifecycle monitoring for integration tests.
/// </summary>
public static class ContainerHelper
{
    private const string Namespace = "gzctf-test";
    private const int MaxAttempts = 30;
    private const int DelayMs = 2000;

    /// <summary>
    /// Read environment variables from an admin test container
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, string?>> GetAdminContainerEnvAsync(
        IServiceProvider serviceProvider,
        int challengeId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = await context.GameChallenges
            .AsNoTracking()
            .Include(c => c.TestContainer)
            .FirstOrDefaultAsync(c => c.Id == challengeId);

        if (challenge?.TestContainer is null)
            throw new InvalidOperationException($"Challenge {challengeId} not found");

        return await GetContainerEnvAsync(serviceProvider, challenge.TestContainer);
    }

    /// <summary>
    /// Read environment variables from a user challenge container
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, string?>> GetUserContainerEnvAsync(
        IServiceProvider serviceProvider,
        int challengeId,
        int participationId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var instance = await context.GameInstances
            .AsNoTracking()
            .Include(i => i.Container)
            .FirstOrDefaultAsync(i =>
                i.ChallengeId == challengeId && i.ParticipationId == participationId);

        if (instance?.Container is null)
            throw new InvalidOperationException(
                $"No game instance found for challenge {challengeId}, participation {participationId}");

        return await GetContainerEnvAsync(serviceProvider, instance.Container);
    }

    /// <summary>
    /// Wait for admin test container to be ready
    /// </summary>
    /// <param name="serviceProvider">DI service provider</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="output">Test output helper for logging</param>
    /// <exception cref="InvalidOperationException">Thrown when container fails, times out, or not found</exception>
    public static async Task WaitAdminContainerAsync(IServiceProvider serviceProvider, int challengeId,
        ITestOutputHelper output)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        output.WriteLine($"🔍 Waiting for admin test container for challenge {challengeId}...");

        // Find and validate challenge
        var challenge = await context.GameChallenges
            .AsNoTracking()
            .Include(c => c.TestContainer)
            .FirstOrDefaultAsync(c => c.Id == challengeId);

        if (challenge?.TestContainer is null)
            throw new InvalidOperationException($"Challenge {challengeId} not found");

        output.WriteLine($"✅ Found challenge: {challenge.Title}");

        var container = challenge.TestContainer;

        output.WriteLine($"📦 Found test container: {container.ContainerId}");

        // Wait for container readiness
        await WaitContainerReadyAsync(serviceProvider, container, output);
    }

    /// <summary>
    /// Wait for user container to be ready
    /// </summary>
    /// <param name="serviceProvider">DI service provider</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="participationId">Participation ID (team in game)</param>
    /// <param name="output">Test output helper for logging</param>
    /// <exception cref="InvalidOperationException">Thrown when container fails, times out, or not found</exception>
    public static async Task WaitUserContainerAsync(IServiceProvider serviceProvider, int challengeId,
        int participationId, ITestOutputHelper output)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        output.WriteLine(
            $"🔍 Waiting for user container for challenge {challengeId}, participation {participationId}...");

        var instance = await context.GameInstances.FirstOrDefaultAsync(i =>
            i.ChallengeId == challengeId && i.ParticipationId == participationId);

        if (instance?.Container == null)
            throw new InvalidOperationException(
                $"No game instance found for challenge {challengeId}, participation {participationId}");

        var container = instance.Container;

        output.WriteLine($"📦 Found user container: {container.ContainerId}");

        // Wait for container readiness
        await WaitContainerReadyAsync(serviceProvider, container, output);
    }

    /// <summary>
    /// Fetch flag from container, auto-detecting the access method based on entry format.
    /// NOTE: use `ghcr.io/gzctf/challenge-base/echo:latest`
    /// </summary>
    /// <param name="entry">Container entry: a GUID for PlatformProxy, or "IP:Port" for direct TCP</param>
    /// <param name="factory">
    /// The WebApplicationFactory, required when entry is a GUID (PlatformProxy mode)
    /// to create a WebSocket connection through the test server.
    /// </param>
    /// <returns>The flag string, or null if connection failed</returns>
    public static async Task<string?> FetchFlag(string entry, WebApplicationFactory<Program>? factory = null)
    {
        Console.WriteLine($@"Fetching flag from container entry: {entry}");

        // If entry looks like a GUID, use WebSocket proxy; otherwise use direct TCP
        if (Guid.TryParse(entry, out _))
        {
            if (factory is null)
                throw new InvalidOperationException(
                    "WebApplicationFactory is required for PlatformProxy (GUID) entry");
            return await FetchFlagViaProxy(entry, factory);
        }

        return await FetchFlagViaTcp(entry);
    }

    /// <summary>
    /// Fetch flag via direct TCP connection (cloud/k3s mode with exposed ports)
    /// </summary>
    static async Task<string?> FetchFlagViaTcp(string entry)
    {
        var parts = entry.Split(':');

        if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
            return null;

        var host = parts[0];
        string? flag = null;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(host, port);
                await using var stream = client.GetStream();
                var buffer = new byte[256];
                var bytesRead = await stream.ReadAsync(buffer);
                flag = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                break;
            }
            catch (SocketException) when (attempt < 9)
            {
                await Task.Delay(500);
            }
        }

        Console.WriteLine($@"Retrieved flag via TCP from {entry}: {flag}");
        return flag;
    }

    /// <summary>
    /// Fetch flag via WebSocket proxy (local Docker mode with PlatformProxy).
    /// Connects to the test server's /api/Proxy/{guid} WebSocket endpoint.
    /// </summary>
    static async Task<string?> FetchFlagViaProxy(string containerId, WebApplicationFactory<Program> factory)
    {
        string? flag = null;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                // CreateWebSocketClient uses the TestServer's internal transport,
                // no real HTTP listener needed
                var wsClient = factory.Server.CreateWebSocketClient();
                var wsUri = new Uri(factory.Server.BaseAddress, $"api/Proxy/{containerId}");

                // Replace http(s) scheme with ws(s)
                var builder = new UriBuilder(wsUri) { Scheme = wsUri.Scheme == "https" ? "wss" : "ws" };

                using var ws = await wsClient.ConnectAsync(builder.Uri, CancellationToken.None);

                // The echo container sends the flag immediately on connection
                var buffer = new byte[256];
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);

                if (result.Count > 0)
                    flag = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();

                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                break;
            }
            catch (Exception) when (attempt < 9)
            {
                await Task.Delay(500);
            }
        }

        Console.WriteLine($@"Retrieved flag via WebSocket proxy from {containerId}: {flag}");
        return flag;
    }

    /// <summary>
    /// Internal: Unified container readiness polling for both Docker and Kubernetes
    /// </summary>
    private static async Task WaitContainerReadyAsync(
        IServiceProvider serviceProvider,
        Container container,
        ITestOutputHelper output)
    {
        // Try Kubernetes first (if available)
        var k8sProviderService = serviceProvider.GetService<IContainerProvider<Kubernetes, KubernetesMetadata>>();
        if (k8sProviderService != null)
        {
            await WaitK8sContainerReadyAsync(k8sProviderService, container, output);
            return;
        }

        // Fall back to Docker
        var dockerProviderService = serviceProvider.GetService<IContainerProvider<DockerClient, DockerMetadata>>();
        if (dockerProviderService != null)
        {
            await WaitDockerContainerReadyAsync(dockerProviderService, container, output);
            return;
        }

        throw new InvalidOperationException("Neither Kubernetes nor Docker provider is available");
    }

    /// <summary>
    /// Internal: Read container env vars from the active container provider
    /// </summary>
    private static async Task<IReadOnlyDictionary<string, string?>> GetContainerEnvAsync(
        IServiceProvider serviceProvider,
        Container container)
    {
        var k8sProviderService = serviceProvider.GetService<IContainerProvider<Kubernetes, KubernetesMetadata>>();
        if (k8sProviderService != null)
        {
            var pod = await k8sProviderService.GetProvider()
                .CoreV1.ReadNamespacedPodAsync(container.ContainerId, Namespace);

            var envVars = pod.Spec?.Containers
                .SelectMany(c => c.Env ?? [])
                .ToDictionary(env => env.Name, env => (string?)env.Value);

            return envVars ?? new Dictionary<string, string?>();
        }

        var dockerProviderService = serviceProvider.GetService<IContainerProvider<DockerClient, DockerMetadata>>();
        if (dockerProviderService != null)
        {
            var inspection = await dockerProviderService.GetProvider()
                .Containers.InspectContainerAsync(container.ContainerId);

            return (inspection.Config?.Env ?? [])
                .Select(env =>
                {
                    var separatorIndex = env.IndexOf('=');
                    return separatorIndex switch
                    {
                        < 0 => new KeyValuePair<string, string?>(env, null),
                        _ => new KeyValuePair<string, string?>(
                            env[..separatorIndex],
                            env[(separatorIndex + 1)..])
                    };
                })
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        throw new InvalidOperationException("Neither Kubernetes nor Docker provider is available");
    }

    /// <summary>
    /// Internal: Poll Kubernetes pod until ready or failed
    /// </summary>
    private static async Task WaitK8sContainerReadyAsync(
        IContainerProvider<Kubernetes, KubernetesMetadata> k8sProvider,
        Container container,
        ITestOutputHelper output)
    {
        var k8sClient = k8sProvider.GetProvider();
        var podName = container.ContainerId;

        output.WriteLine($"🔍 Waiting for Kubernetes pod '{podName}' (Image: {container.Image}) to be ready...");

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                var pod = await k8sClient.CoreV1.ReadNamespacedPodAsync(podName, Namespace);

                var phase = pod.Status?.Phase ?? "Unknown";
                var conditions = pod.Status?.Conditions ?? [];
                var readyCondition = conditions.FirstOrDefault(c => c.Type == "Ready");
                var readyStatus = readyCondition?.Status ?? "False";

                output.WriteLine($"  Attempt {attempt + 1}/{MaxAttempts}: Phase={phase}, Ready={readyStatus}");

                switch (phase)
                {
                    // Check if pod is running and ready
                    case "Running" when readyStatus == "True":
                        output.WriteLine($"✅ Pod '{podName}' is Ready!");
                        return;
                    // Pod has failed
                    case "Failed":
                        {
                            var reason = pod.Status?.Reason ?? "Unknown";
                            var message = pod.Status?.Message ?? "";
                            throw new InvalidOperationException($"Pod '{podName}' Failed: {reason} - {message}");
                        }
                }

                // Avoid delay on the last attempt
                if (attempt < MaxAttempts - 1)
                {
                    await Task.Delay(DelayMs);
                }
            }
            catch (Exception e) when (!(e is InvalidOperationException))
            {
                output.WriteLine($"⚠️ Error checking pod status: {e.Message}");
                if (attempt < MaxAttempts - 1)
                {
                    await Task.Delay(DelayMs);
                }
            }
        }

        throw new InvalidOperationException(
            $"Pod '{podName}' did not reach Ready state within {MaxAttempts * DelayMs / 1000} seconds");
    }

    /// <summary>
    /// Internal: Poll Docker container until ready or failed
    /// </summary>
    private static async Task WaitDockerContainerReadyAsync(
        IContainerProvider<DockerClient, DockerMetadata> dockerProvider,
        Container container,
        ITestOutputHelper output)
    {
        var dockerClient = dockerProvider.GetProvider();
        var containerId = container.ContainerId;

        output.WriteLine($"🔍 Waiting for Docker container '{containerId}' (Image: {container.Image}) to be ready...");

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                var inspection = await dockerClient.Containers.InspectContainerAsync(containerId);
                var state = inspection.State;

                output.WriteLine(
                    $"  Attempt {attempt + 1}/{MaxAttempts}: Running={state.Running}, Status={state.Status}");

                switch (state.Running)
                {
                    // Check if container is running
                    case true:
                        output.WriteLine($"✅ Docker container '{containerId}' is Running!");
                        return;
                    // Container has exited abnormally
                    case false when state.ExitCode != 0:
                        {
                            var error = state.Error ?? "No error message";
                            throw new InvalidOperationException(
                                $"Docker container '{containerId}' exited abnormally (exit code: {state.ExitCode}): {error}");
                        }
                }

                // Avoid delay on the last attempt
                if (attempt < MaxAttempts - 1)
                {
                    await Task.Delay(DelayMs);
                }
            }
            catch (Exception e) when (!(e is InvalidOperationException))
            {
                output.WriteLine($"⚠️ Error checking container status: {e.Message}");
                if (attempt < MaxAttempts - 1)
                {
                    await Task.Delay(DelayMs);
                }
            }
        }

        throw new InvalidOperationException(
            $"Docker container '{containerId}' did not reach Running state within {MaxAttempts * DelayMs / 1000} seconds");
    }
}
