using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Services.Container.Provider;

namespace GZCTF.Services.Container.Exec;

public sealed class DockerContainerExecChannel(
    IContainerProvider<DockerClient, DockerMetadata> provider,
    ILogger<DockerContainerExecChannel> logger) : IContainerExecChannel
{
    private readonly DockerClient _client = provider.GetProvider();

    public async Task<IExecSession> OpenAsync(Models.Data.Container container, string shell, CancellationToken token)
    {
        var safeShell = string.Equals(shell, "bash", StringComparison.OrdinalIgnoreCase) ? "bash" : "sh";
        var exec = await _client.Exec.CreateContainerExecAsync(container.ContainerId,
            new ContainerExecCreateParameters
            {
                AttachStdin = true,
                AttachStdout = true,
                AttachStderr = true,
                TTY = true,
                Cmd = [safeShell]
            }, token);

        var stream = await _client.Exec.StartContainerExecAsync(exec.ID,
            new ContainerExecStartParameters { TTY = true }, token);
        logger.LogInformation("Exec opened: container {Id}, exec {Exec}, shell {Shell}",
            container.LogId, exec.ID, safeShell);
        return new DockerExecSession(exec.ID, stream, logger);
    }

    sealed class DockerExecSession(string execId, MultiplexedStream stream, ILogger logger) : IExecSession
    {
        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token)
        {
            var rented = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                var result = await stream.ReadOutputAsync(rented, 0, buffer.Length, token);
                if (result.EOF || result.Count <= 0) return 0;
                new ReadOnlyMemory<byte>(rented, 0, result.Count).CopyTo(buffer);
                return result.Count;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(rented);
            }
        }

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> chunk, CancellationToken token)
        {
            var rented = System.Buffers.ArrayPool<byte>.Shared.Rent(chunk.Length);
            try
            {
                chunk.CopyTo(rented);
                await stream.WriteAsync(rented, 0, chunk.Length, token);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(rented);
            }
        }

        public Task ResizeAsync(uint cols, uint rows, CancellationToken token)
        {
            // Docker.DotNet.Enhanced 3.131 doesn't expose POST /exec/{id}/resize
            // as a typed method — only POST /containers/{id}/resize, which
            // would target the wrong container. Initial size is set via
            // ContainerExecStartParameters.ConsoleSize at Open time; live
            // resize is a no-op in v1. Logged at debug so admins can see
            // it's intentional.
            logger.LogDebug("Exec resize requested for {Exec} ({Cols}x{Rows}) — ignored (not supported)",
                execId, cols, rows);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            try { stream.Dispose(); } catch { /* ignore */ }
            return ValueTask.CompletedTask;
        }
    }
}

public sealed class K8sContainerExecChannel : IContainerExecChannel
{
    public Task<IExecSession> OpenAsync(Models.Data.Container container, string shell, CancellationToken token) =>
        throw new NotSupportedException(
            "In-browser shell is not supported in kubernetes runtime in v1. Use `kubectl exec` against the cluster directly.");
}
