using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Minio.DataModel.Args;

namespace GZCTF.AppHost.MinIO;

class MinIOBuilder
{
    public int? ApiPort { get; set; }
    public int? ConsolePort { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? DataVolumePath { get; set; }
    public string? BucketName { get; set; }

    public MinIOBuilder WithPorts(int? apiPort = null, int? consolePort = null)
    {
        ApiPort = apiPort;
        ConsolePort = consolePort;
        return this;
    }

    public MinIOBuilder WithCredentials(string accessKey, string secretKey)
    {
        AccessKey = accessKey;
        SecretKey = secretKey;
        return this;
    }

    public MinIOBuilder WithDataVolume(string path)
    {
        DataVolumePath = path;
        return this;
    }

    public MinIOBuilder WithBucket(string bucketName)
    {
        BucketName = bucketName;
        return this;
    }
}


class MinIOResource(string name, string? accessKey = null, string? secretKey = null, string? bucketName = null)
    : ContainerResource(name), IResourceWithConnectionString
{
    internal const string ApiEndpointName = "api";
    internal const string ConsoleEndpointName = "console";
    internal const int DefaultApiPort = 9000;
    internal const int DefaultConsolePort = 9001;

    private EndpointReference? _apiReference;
    private EndpointReference? _consoleReference;

    private EndpointReference ApiEndpoint =>
        _apiReference ??= new EndpointReference(this, ApiEndpointName);

    private EndpointReference ConsoleEndpoint =>
        _consoleReference ??= new EndpointReference(this, ConsoleEndpointName);

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"s3://accessKey={AccessKey};secretKey={SecretKey};bucket=gzctf;endpoint=http://127.0.0.1:{ApiEndpoint.Property(EndpointProperty.Port)};useHttp=true");

    public string? AccessKey { get; } = accessKey ?? "minioadmin";
    public string? SecretKey { get; } = secretKey ?? "minioadmin";
    public string? BucketName { get; } = bucketName;

    public string ConnectionStringEnvironmentVariable => "ConnectionStrings__Storage";
}
class MinioBucketInitializer : IDistributedApplicationLifecycleHook
{
    public async Task AfterResourcesCreatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var resources = appModel.Resources
            .OfType<MinIOResource>();

        foreach (var resource in resources)
        {
            if (string.IsNullOrEmpty(resource.BucketName))
                continue;

            var endpoint = resource.GetEndpoint(MinIOResource.ApiEndpointName);
            using var minioClient = new MinioClient()
                .WithEndpoint($"{endpoint.Host}:{endpoint.Port}")
                .WithCredentials(resource.AccessKey, resource.SecretKey)
                .WithSSL(false)
                .Build();

            if (!await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(resource.BucketName), cancellationToken))
            {
                await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(resource.BucketName), cancellationToken);
            }
        }
    }
}

static class MinIOResourceBuilderExtensions
{
    internal static IResourceBuilder<MinIOResource> AddMinIO(
        this IDistributedApplicationBuilder builder,
        string name,
        Action<MinIOBuilder>? configure = null)
    {
        var options = new MinIOBuilder();
        configure?.Invoke(options);

        var resource = new MinIOResource(name, options.AccessKey, options.SecretKey, options.BucketName);
        builder.Services.AddSingleton<IDistributedApplicationLifecycleHook, MinioBucketInitializer>();
        return builder.AddResource(resource)
            .WithImage("minio/minio")
            .WithImageRegistry("docker.io")
            .WithImageTag("latest")
            .WithHttpEndpoint(
                targetPort: MinIOResource.DefaultApiPort,
                port: options.ApiPort,
                name: MinIOResource.ApiEndpointName)
            .WithHttpEndpoint(
                targetPort: MinIOResource.DefaultConsolePort,
                port: options.ConsolePort,
                name: MinIOResource.ConsoleEndpointName)
            .ConfigureCredentials(options)
            .ConfigureVolume(options)
            .WithArgs("server", "/data", "--console-address", $":{MinIOResource.DefaultConsolePort}");
    }

    private static IResourceBuilder<MinIOResource> ConfigureCredentials(
        this IResourceBuilder<MinIOResource> builder,
        MinIOBuilder options) => builder
            .WithEnvironment("MINIO_ROOT_USER", options.AccessKey ?? "minioadmin")
            .WithEnvironment("MINIO_ROOT_PASSWORD", options.SecretKey ?? "minioadmin");

    private static IResourceBuilder<MinIOResource> ConfigureVolume(
        this IResourceBuilder<MinIOResource> builder,
        MinIOBuilder options)
    {
        if (!string.IsNullOrEmpty(options.DataVolumePath))
            builder = builder.WithVolume(options.DataVolumePath, "/data");
        return builder;
    }
}
