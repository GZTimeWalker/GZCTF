using GZCTF.Storage.Interface;

namespace GZCTF.Features.Imports.Infrastructure;

public sealed class ImportBlobStaging(IBlobStorage storage)
{
    public async Task<IReadOnlyList<string>> StageAsync(
        LegacyZipPackage package, Guid batchId, CancellationToken token = default)
    {
        var created = new List<string>();
        try
        {
            foreach (var pair in package.Files.Where(pair =>
                         !pair.Key.Equals("manifest.json", StringComparison.OrdinalIgnoreCase)))
            {
                var target = $"legacy-import/{batchId:N}/{pair.Key}";
                await using var content = new MemoryStream(pair.Value, writable: false);
                await storage.WriteAsync(target, content, cancellationToken: token);
                created.Add(target);
            }

            return created;
        }
        catch
        {
            foreach (var path in created)
                await storage.DeleteAsync(path, token);
            throw;
        }
    }

    public async Task CompensateAsync(IEnumerable<string> createdPaths, CancellationToken token = default)
    {
        foreach (var path in createdPaths)
            await storage.DeleteAsync(path, token);
    }
}
