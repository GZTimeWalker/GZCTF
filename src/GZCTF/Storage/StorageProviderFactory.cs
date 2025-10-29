using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Amazon.S3;
using GZCTF.Storage.Interface;

namespace GZCTF.Storage;

/// <summary>
/// Factory helpers for creating storage providers from connection strings.
/// </summary>
public static class StorageProviderFactory
{
    const string DefaultDiskPath = "./files";

    public static IBlobStorage Create(string connectionString)
    {
        var (scheme, parameters) = StorageConnectionString.Parse(connectionString);

        IBlobStorage storage = scheme switch
        {
            "disk" => CreateDiskStorage(parameters),
            "aws.s3" or "minio.s3" or "s3" => CreateS3Storage(parameters, scheme),
            _ => throw new NotSupportedException($"Storage provider '{scheme}' is not supported.")
        };

        storage.EnsureInitializedAsync().GetAwaiter().GetResult();
        return storage;
    }

    static LocalBlobStorage CreateDiskStorage(IReadOnlyDictionary<string, string> parameters)
    {
        var path = parameters.TryGetValue("path", out var configuredPath) && !string.IsNullOrWhiteSpace(configuredPath)
            ? configuredPath
            : DefaultDiskPath;

        return new LocalBlobStorage(path);
    }

    static S3BlobStorage CreateS3Storage(IReadOnlyDictionary<string, string> parameters, string scheme)
    {
        if (!parameters.TryGetValue("bucket", out var bucket) || string.IsNullOrWhiteSpace(bucket))
            throw new InvalidOperationException("S3 storage requires the 'bucket' parameter.");

        parameters.TryGetValue("region", out var region);
        parameters.TryGetValue("endpoint", out var endpoint);

        if (string.IsNullOrWhiteSpace(region) && string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("S3 storage requires either 'region' or 'endpoint' to be specified.");

        var credentials = ResolveCredentials(parameters);

        var config = new AmazonS3Config();

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            config.ServiceURL = endpoint;
            config.DisableS3ExpressSessionAuth = true;
        }

        if (!string.IsNullOrWhiteSpace(region))
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);

        if (parameters.TryGetValue("useHttp", out var useHttpValue) && bool.TryParse(useHttpValue, out var useHttp))
            config.UseHttp = useHttp;

        var forcePathStyle = scheme.Equals("minio.s3", StringComparison.OrdinalIgnoreCase);
        if (parameters.TryGetValue("forcePathStyle", out var fpsValue) && bool.TryParse(fpsValue, out var fps))
            forcePathStyle = fps;
        config.ForcePathStyle = forcePathStyle;

        var client = new AmazonS3Client(credentials, config);
        return new S3BlobStorage(client, bucket);
    }

    static AWSCredentials ResolveCredentials(IReadOnlyDictionary<string, string> parameters)
    {
        if (parameters.TryGetValue("accessKey", out var accessKey) &&
            parameters.TryGetValue("secretKey", out var secretKey) &&
            !string.IsNullOrWhiteSpace(accessKey) &&
            !string.IsNullOrWhiteSpace(secretKey))
        {
            if (parameters.TryGetValue("sessionToken", out var sessionToken) &&
                !string.IsNullOrWhiteSpace(sessionToken))
                return new SessionAWSCredentials(accessKey, secretKey, sessionToken);

            return new BasicAWSCredentials(accessKey, secretKey);
        }

        var resolver = new DefaultAWSCredentialsIdentityResolver();
        return resolver.ResolveIdentity(clientConfig: null);
    }
}
