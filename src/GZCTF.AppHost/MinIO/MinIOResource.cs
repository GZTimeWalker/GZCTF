namespace GZCTF.AppHost.MinIO;

class MinIOBuilder
{
    public int? ApiPort { get; set; }
    public int? ConsolePort { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? DataVolumePath { get; set; }

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
}

class MinIOResource(string name, string? accessKey = null, string? secretKey = null)
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
            $"s3://accessKey={AccessKey};secretKey={SecretKey};bucket=gzctf;useHttp=true;" +
            $"endpoint=http://{(ApiEndpoint.Host is "localhost" ? "127.0.0.1" : ApiEndpoint.Host)}:{ApiEndpoint.Property(EndpointProperty.Port)}");

    public string? AccessKey { get; } = accessKey ?? "minioadmin";
    public string? SecretKey { get; } = secretKey ?? "minioadmin";

    public string ConnectionStringEnvironmentVariable => "ConnectionStrings__Storage";
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

        var resource = new MinIOResource(name, options.AccessKey, options.SecretKey);
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
