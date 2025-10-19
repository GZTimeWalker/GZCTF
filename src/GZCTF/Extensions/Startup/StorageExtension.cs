using GZCTF.Storage;

namespace GZCTF.Extensions.Startup;

static class StorageExtension
{
    const string DefaultConnectionString = "disk://path=./files";

    public static void ConfigureStorage(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Storage");

        var isEmpty = string.IsNullOrWhiteSpace(connectionString);
        var useDisk = !isEmpty && connectionString!.StartsWith("disk://", StringComparison.OrdinalIgnoreCase);

        // force the path used by the disk storage to avoid unintended behavior
        if (isEmpty || useDisk)
            connectionString = DefaultConnectionString;

        try
        {
            var storage = StorageProviderFactory.Create(connectionString!);
            builder.Services.AddSingleton(storage);
        }
        catch (Exception e)
        {
            ExitWithFatalMessage(StaticLocalizer[nameof(Resources.Program.Init_StorageInitFailed),
                e.Message]);
        }
    }
}
