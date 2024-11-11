using FluentStorage;
using FluentStorage.Blobs;

namespace GZCTF.Extensions;

public static class StorageExtension
{
    const string DefaultConnectionString = "disk://path=files";

    public static void AddStorage(this WebApplicationBuilder builder, string? connectionString)
    {
        var isEmpty = string.IsNullOrWhiteSpace(connectionString);
        var useDisk = !isEmpty && connectionString!.StartsWith("disk://");

        if (isEmpty || useDisk)
            connectionString = DefaultConnectionString;

        try
        {
            var storage = StorageFactory.Blobs.FromConnectionString(connectionString);
            builder.Services.AddSingleton(storage);
        }
        catch (Exception e)
        {
            Program.ExitWithFatalMessage(Program.StaticLocalizer[nameof(Resources.Program.Init_StorageInitFailed),
                e.Message]);
        }
    }

    internal static async Task<FileRecord[]> GetFileRecordsAsync(this IBlobStorage storage, string prefix,
        CancellationToken token = default)
    {
        var blobs = await storage.ListAsync(prefix, cancellationToken: token);
        return blobs.Select(blob => new FileRecord
        {
            FileName = blob.Name,
            Size = blob.Size ?? 0,
            UpdateTime = blob.LastModificationTime ?? DateTimeOffset.MinValue
        }).ToArray();
    }
}
