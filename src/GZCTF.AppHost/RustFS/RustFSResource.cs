namespace GZCTF.AppHost.RustFS;

internal class RustFSBuilder
{
    public int? ApiPort { get; set; }
    public int? ConsolePort { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? DataVolumePath { get; set; }

    public RustFSBuilder WithPorts(int? apiPort = null, int? consolePort = null)
    {
        ApiPort = apiPort;
        ConsolePort = consolePort;
        return this;
    }

    public RustFSBuilder WithCredentials(string accessKey, string secretKey)
    {
        AccessKey = accessKey;
        SecretKey = secretKey;
        return this;
    }

    public RustFSBuilder WithDataVolume(string path)
    {
        DataVolumePath = path;
        return this;
    }
}

internal class RustFSResource(string name, string? accessKey = null, string? secretKey = null)
    : ContainerResource(name), IResourceWithConnectionString
{
    internal const string ApiEndpointName = "api";
    internal const string ConsoleEndpointName = "console";
    internal const int DefaultApiPort = 9000;
    internal const int DefaultConsolePort = 9001;

    private EndpointReference ApiEndpoint =>
        field ??= new EndpointReference(this, ApiEndpointName);

    private EndpointReference ConsoleEndpoint =>
        field ??= new EndpointReference(this, ConsoleEndpointName);

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"s3://accessKey={AccessKey};secretKey={SecretKey};bucket=gzctf;forcePathStyle=true;useHttp=true;" +
            $"endpoint=http://{(ApiEndpoint.Host is "localhost" ? "127.0.0.1" : ApiEndpoint.Host)}:{ApiEndpoint.Property(EndpointProperty.Port)}");

    public string AccessKey { get; } = accessKey ?? "gzctf-apphost";
    public string SecretKey { get; } = secretKey ?? Guid.NewGuid().ToString("N");

    public string ConnectionStringEnvironmentVariable => "ConnectionStrings__Storage";
}

internal static class RustFSResourceBuilderExtensions
{
    internal static IResourceBuilder<RustFSResource> AddRustFS(
        this IDistributedApplicationBuilder builder,
        string name,
        Action<RustFSBuilder>? configure = null)
    {
        var options = new RustFSBuilder();
        configure?.Invoke(options);

        var resource = new RustFSResource(name, options.AccessKey, options.SecretKey);
        return builder.AddResource(resource)
            .WithImage("rustfs/rustfs")
            .WithImageRegistry("docker.io")
            .WithImageTag("1.0.0")
            .WithHttpEndpoint(
                targetPort: RustFSResource.DefaultApiPort,
                port: options.ApiPort,
                name: RustFSResource.ApiEndpointName)
            .WithHttpEndpoint(
                targetPort: RustFSResource.DefaultConsolePort,
                port: options.ConsolePort,
                name: RustFSResource.ConsoleEndpointName)
            .WithEnvironment("RUSTFS_ACCESS_KEY", resource.AccessKey)
            .WithEnvironment("RUSTFS_SECRET_KEY", resource.SecretKey)
            .WithEnvironment("RUSTFS_CONSOLE_ADDRESS", ":" + RustFSResource.DefaultConsolePort)
            .WithHttpHealthCheck("/health/ready", endpointName: RustFSResource.ApiEndpointName)
            .ConfigureVolume(options)
            .WithArgs("/data");
    }

    extension(IResourceBuilder<RustFSResource> builder)
    {
        private IResourceBuilder<RustFSResource> ConfigureVolume(RustFSBuilder options)
        {
            if (!string.IsNullOrEmpty(options.DataVolumePath))
                builder = builder.WithVolume(options.DataVolumePath, "/data");
            return builder;
        }
    }
}
