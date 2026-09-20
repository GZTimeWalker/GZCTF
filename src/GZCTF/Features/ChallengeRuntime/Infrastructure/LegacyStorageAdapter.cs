using GZCTF.Storage.Interface;

namespace GZCTF.Features.ChallengeRuntime.Infrastructure;

public interface ILegacyStorageAdapter
{
    Task<bool> ExistsAsync(string storageKey, CancellationToken token);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken token);
}

public sealed class LegacyStorageAdapter(IBlobStorage storage) : ILegacyStorageAdapter
{
    public Task<bool> ExistsAsync(string storageKey, CancellationToken token) =>
        storage.ExistsAsync(storageKey, token);

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken token) =>
        storage.OpenReadAsync(storageKey, token);
}
