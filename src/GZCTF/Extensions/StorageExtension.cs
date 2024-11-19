using FluentStorage;

namespace GZCTF.Extensions;

public static class StorageExtension
{
    const string DefaultConnectionString = "disk://path=./files";

    public static void AddStorage(this WebApplicationBuilder builder, string? connectionString)
    {
        var isEmpty = string.IsNullOrWhiteSpace(connectionString);
        var useDisk = !isEmpty && connectionString!.StartsWith("disk://");

        // force the path used by the disk storage to avoid unintended behavior
        if (isEmpty || useDisk)
            connectionString = DefaultConnectionString;

        var prefix = connectionString!.Split("://")[0].Trim();

        switch (prefix)
        {
            case "aws.s3":
            case "minio.s3":
                StorageFactory.Modules.UseAwsStorage();
                break;
            case "azure.blobs":
                StorageFactory.Modules.UseAzureBlobStorage();
                break;
        }

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
}
