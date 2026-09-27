using System.Text.Json;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Manager;
using GZCTF.Utils;

namespace GZCTF.Features.ChallengeRuntime.Infrastructure;

public sealed record CanonicalContainerRequest(
    Guid UserId,
    Guid ChallengeId,
    string Image,
    int ExposedPort,
    int Cpu,
    int MemoryMb,
    int StorageMb,
    string NetworkMode,
    string? Flag);

public interface ILegacyContainerRuntimeAdapter
{
    Task<Container?> StartAsync(CanonicalContainerRequest request, CancellationToken token);
    Task StopAsync(Container container, CancellationToken token);
}

public sealed class LegacyContainerRuntimeAdapter(IContainerManager manager) : ILegacyContainerRuntimeAdapter
{
    public Task<Container?> StartAsync(CanonicalContainerRequest request, CancellationToken token)
    {
        var networkMode = Enum.TryParse<NetworkMode>(request.NetworkMode, true, out var parsed)
            ? parsed
            : NetworkMode.Open;
        return manager.CreateContainerAsync(new ContainerConfig
        {
            Image = request.Image,
            TeamId = request.UserId.ToString("N"),
            ChallengeId = StableChallengeId(request.ChallengeId),
            UserId = request.UserId,
            ExposedPort = request.ExposedPort,
            Flag = request.Flag,
            CPUCount = request.Cpu,
            MemoryLimit = request.MemoryMb,
            StorageLimit = request.StorageMb,
            NetworkMode = networkMode
        }, token);
    }

    public Task StopAsync(Container container, CancellationToken token) =>
        manager.DestroyContainerAsync(container, token);

    private static int StableChallengeId(Guid challengeId) =>
        BitConverter.ToInt32(challengeId.ToByteArray(), 0) & int.MaxValue;
}
