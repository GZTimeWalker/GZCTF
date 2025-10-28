using System.Net.Sockets;
using Docker.DotNet;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Services.Container.Provider;
using k8s;
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
    /// Fetch flag from container
    /// NOTE: use `ghcr.io/gzctf/challenge-base/echo:latest`
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    public static async Task<string?> FetchFlag(string entry)
    {
        Console.WriteLine($@"🔍 Fetching flag from container entry: {entry}");

        // Parse the Entry field to get IP and port
        // Entry format is either "proxy-id" or "IP:Port"
        // For test environments, use localhost since Docker containers are accessible locally
        var parts = entry.Split(':');

        if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
            return null;

        // Use localhost for test environment instead of the container IP
        var host = parts[0];

        // Try to connect to the container and retrieve the flag
        string? flag = null;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(host, port);
                await using var stream = client.GetStream();
                // Read the flag from the echo container
                byte[] buffer = new byte[256];
                int bytesRead = await stream.ReadAsync(buffer);
                flag = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                break;
            }
            catch (SocketException) when (attempt < 9)
            {
                // Container might not be ready yet, retry after delay
                await Task.Delay(500);
            }
        }

        // Output the retrieved flag for verification
        Console.WriteLine($@"✅ Successfully retrieved flag from {entry}: {flag}");

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

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
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

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
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
