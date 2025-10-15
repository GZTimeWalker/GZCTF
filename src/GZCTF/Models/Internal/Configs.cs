using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using GZCTF.Extensions;
using GZCTF.Services.Cache;
using MemoryPack;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.Encoders;
using Serilog.Sinks.Grafana.Loki;

namespace GZCTF.Models.Internal;

/// <summary>
/// Ignore when saving automatically
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AutoSaveIgnoreAttribute : Attribute;

/// <summary>
/// Update cache when this property changes
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class CacheFlushAttribute(string cacheKey) : Attribute
{
    public string CacheKey { get; } = cacheKey;
}

/// <summary>
/// Account policy
/// </summary>
public class AccountPolicy
{
    /// <summary>
    /// Allow user registration
    /// </summary>
    public bool AllowRegister { get; set; } = true;

    /// <summary>
    /// Activate account upon registration
    /// </summary>
    public bool ActiveOnRegister { get; set; } = true;

    /// <summary>
    /// Use captcha verification
    /// </summary>
    [CacheFlush(CacheKey.CaptchaConfig)]
    public bool UseCaptcha { get; set; }

    /// <summary>
    /// Email confirmation required for registration, email change, and password recovery
    /// </summary>
    public bool EmailConfirmationRequired { get; set; }

    /// <summary>
    /// Email domain list, separated by commas
    /// </summary>
    public string EmailDomainList { get; set; } = string.Empty;
}

/// <summary>
/// Container policy
/// </summary>
public class ContainerPolicy
{
    /// <summary>
    /// Automatically destroy the oldest container when the limit is reached
    /// </summary>
    public bool AutoDestroyOnLimitReached { get; set; }

    /// <summary>
    /// User container limit, used to limit the number of exercise containers
    /// </summary>
    public int MaxExerciseContainerCountPerUser { get; set; } = 1;

    /// <summary>
    /// Default container lifetime in minutes
    /// </summary>
    [CacheFlush(CacheKey.ClientConfig)]
    [Range(1, 7200, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int DefaultLifetime { get; set; } = 120;

    /// <summary>
    /// Extension duration for each renewal in minutes
    /// </summary>
    [CacheFlush(CacheKey.ClientConfig)]
    [Range(1, 7200, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int ExtensionDuration { get; set; } = 120;

    /// <summary>
    /// Renewal window before container stops in minutes
    /// </summary>
    [CacheFlush(CacheKey.ClientConfig)]
    [Range(1, 360, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int RenewalWindow { get; set; } = 10;
}

public class X25519KeyPair
{
    /// <summary>
    /// Public key
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Private key
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    public void RegenerateKeys(byte[] xorKey)
    {
        var kp = CryptoUtils.GenerateX25519KeyPair();
        var privateKey = (X25519PrivateKeyParameters)kp.Private;
        var publicKey = (X25519PublicKeyParameters)kp.Public;
        var privateKeyBytes = Codec.Xor(privateKey.GetEncoded(), xorKey);
        PublicKey = Base64.ToBase64String(publicKey.GetEncoded());
        PrivateKey = Base64.ToBase64String(privateKeyBytes);
    }

    public string? Decrypt(string data, byte[] xorKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(xorKey);

        try
        {
            var encryptedData = Base64.Decode(data);
            var privateKeyBytes = Codec.Xor(Base64.Decode(PrivateKey), xorKey);
            var privateKey = new X25519PrivateKeyParameters(privateKeyBytes);

            return Encoding.UTF8.GetString(CryptoUtils.DecryptData(encryptedData, privateKey));
        }
        catch
        {
            // If decryption fails, return null
            return null;
        }
    }
}

public class Ed25519KeyPair
{
    /// <summary>
    /// Public key
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Private key
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    public void RegenerateKeys(byte[] xorKey)
    {
        var kp = CryptoUtils.GenerateEd25519KeyPair();
        var privateKey = (Ed25519PrivateKeyParameters)kp.Private;
        var publicKey = (Ed25519PublicKeyParameters)kp.Public;
        var privateKeyBytes = Codec.Xor(privateKey.GetEncoded(), xorKey);
        PublicKey = Base64.ToBase64String(publicKey.GetEncoded());
        PrivateKey = Base64.ToBase64String(privateKeyBytes);
    }

    public string Sign(string data, byte[] xorKey, bool useUrlSafeBase64 = false)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(xorKey);

        var privateKeyBytes = Codec.Xor(Base64.Decode(PrivateKey), xorKey);
        var privateKey = new Ed25519PrivateKeyParameters(privateKeyBytes);
        return CryptoUtils.GenerateSignature(data, privateKey, SignAlgorithm.Ed25519, useUrlSafeBase64);
    }

    public bool Verify(string data, string signature, bool useUrlSafeBase64 = false)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(signature);

        try
        {
            var publicKey = new Ed25519PublicKeyParameters(Base64.Decode(PublicKey));
            return CryptoUtils.VerifySignature(data, signature, publicKey, SignAlgorithm.Ed25519, useUrlSafeBase64);
        }
        catch
        {
            // If verification fails, return false
            return false;
        }
    }
}

/// <summary>
/// A context for signature operations, including signing and verifying
/// </summary>
public record SignatureContext(Ed25519KeyPair EncryptedKeyPair, byte[] XorKey)
{
    public string Sign(string data, bool urlSafe = true) =>
        EncryptedKeyPair.Sign(data, XorKey, urlSafe);

    public bool Verify(string data, string signature, bool urlSafe = true) =>
        EncryptedKeyPair.Verify(data, signature, urlSafe);
}

/// <summary>
/// Configs controlled by the backend
/// </summary>
public class ManagedConfig
{
    /// <summary>
    /// Api encryption configuration
    /// </summary>
    public X25519KeyPair ApiEncryption { get; set; } = new();

    /// <summary>
    /// Api token configuration
    /// </summary>
    public Ed25519KeyPair ApiToken { get; set; } = new();
}

/// <summary>
/// Global settings
/// </summary>
public class GlobalConfig
{
    /// <summary>
    /// Default site description
    /// </summary>
    public const string DefaultDescription = "GZ::CTF is an open source CTF platform";

    /// <summary>
    /// Platform prefix name
    /// </summary>
    [CacheFlush(CacheKey.Index)]
    [CacheFlush(CacheKey.ClientConfig)]
    public string Title { get; set; } = "GZ";

    /// <summary>
    /// Platform slogan
    /// </summary>
    [CacheFlush(CacheKey.ClientConfig)]
    public string Slogan { get; set; } = "Hack for fun not for profit";

    /// <summary>
    /// Site description information
    /// </summary>
    [CacheFlush(CacheKey.Index)]
    public string? Description { get; set; } = DefaultDescription;

    /// <summary>
    /// Footer information
    /// </summary>
    [CacheFlush(CacheKey.ClientConfig)]
    public string? FooterInfo { get; set; }

    /// <summary>
    /// Custom theme color
    /// </summary>
    [CacheFlush(CacheKey.ClientConfig)]
    public string? CustomTheme { get; set; }

    /// <summary>
    /// Use asymmetric encryption for API requests
    /// </summary>
    [CacheFlush(CacheKey.ClientConfig)]
    public bool ApiEncryption { get; set; }

    /// <summary>
    /// Platform logo hash
    /// </summary>
    [AutoSaveIgnore]
    public string? LogoHash { get; set; }

    /// <summary>
    /// Platform favicon hash
    /// </summary>
    [AutoSaveIgnore]
    public string? FaviconHash { get; set; }

    [JsonIgnore]
    public string? LogoUrl => string.IsNullOrEmpty(LogoHash) ? null : $"/assets/{LogoHash}/logo";

    /// <summary>
    /// Platform name, used for email and homepage rendering
    /// </summary>
    [JsonIgnore]
    public string Platform => string.IsNullOrEmpty(Title) ? "GZ::CTF" : $"{Title}::CTF";
}

/// <summary>
/// Client configuration
/// </summary>
[MemoryPackable]
public partial class ClientConfig
{
    /// <summary>
    /// Platform prefix name
    /// </summary>
    public string Title { get; set; } = "GZ";

    /// <summary>
    /// Platform slogan
    /// </summary>
    public string Slogan { get; set; } = "Hack for fun not for profit";

    /// <summary>
    /// Footer information
    /// </summary>
    public string? FooterInfo { get; set; }

    /// <summary>
    /// Custom theme color
    /// </summary>
    public string? CustomTheme { get; set; }

    /// <summary>
    /// The public key used for API requests
    /// </summary>
    public string? ApiPublicKey { get; set; }

    /// <summary>
    /// Platform logo URL
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Container port mapping type
    /// </summary>
    public ContainerPortMappingType PortMapping { get; set; } = ContainerPortMappingType.Default;

    /// <summary>
    /// Default container lifetime in minutes
    /// </summary>
    public int DefaultLifetime { get; set; } = 120;

    /// <summary>
    /// Extension duration for each renewal in minutes
    /// </summary>
    public int ExtensionDuration { get; set; } = 120;

    /// <summary>
    /// Renewal window before container stops in minutes
    /// </summary>
    public int RenewalWindow { get; set; } = 10;

    [JsonIgnore]
    public DateTimeOffset UpdateTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    public static ClientConfig FromServiceProvider(IServiceProvider serviceProvider) =>
        FromConfigs(
            serviceProvider.GetRequiredService<IOptionsSnapshot<GlobalConfig>>().Value,
            serviceProvider.GetRequiredService<IOptionsSnapshot<ContainerPolicy>>().Value,
            serviceProvider.GetRequiredService<IOptionsSnapshot<ContainerProvider>>().Value,
            serviceProvider.GetRequiredService<IOptionsSnapshot<ManagedConfig>>().Value);

    static ClientConfig FromConfigs(GlobalConfig globalConfig, ContainerPolicy containerPolicy,
        ContainerProvider containerProvider, ManagedConfig managedConfig) =>
        new()
        {
            Title = globalConfig.Title,
            Slogan = globalConfig.Slogan,
            FooterInfo = globalConfig.FooterInfo,
            CustomTheme = globalConfig.CustomTheme,
            LogoUrl = globalConfig.LogoUrl,
            ApiPublicKey = globalConfig.ApiEncryption ? managedConfig.ApiEncryption.PublicKey : null,
            PortMapping = containerProvider.PortMappingType,
            DefaultLifetime = containerPolicy.DefaultLifetime,
            ExtensionDuration = containerPolicy.ExtensionDuration,
            RenewalWindow = containerPolicy.RenewalWindow
        };
}

#region Mail Config

public class SmtpConfig
{
    public string? Host { get; set; } = "127.0.0.1";
    public int? Port { get; set; } = 587;
    public bool BypassCertVerify { get; set; }
}

public class EmailConfig
{
    public string? UserName { get; set; } = string.Empty;
    public string? Password { get; set; } = string.Empty;
    public string? SenderAddress { get; set; } = string.Empty;
    public string? SenderName { get; set; } = string.Empty;
    public SmtpConfig? Smtp { get; set; } = new();
}

#endregion

#region Container Provider

[JsonConverter(typeof(JsonStringEnumConverter<ContainerProviderType>))]
public enum ContainerProviderType
{
    Docker,
    Kubernetes
}

[JsonConverter(typeof(JsonStringEnumConverter<ContainerPortMappingType>))]
public enum ContainerPortMappingType
{
    /// Use default to map the container port to a random port on the host
    Default,

    /// Use platform proxy to map the container tcp to wss
    PlatformProxy
}

public class ContainerProvider
{
    public ContainerProviderType Type { get; set; } = ContainerProviderType.Docker;
    public ContainerPortMappingType PortMappingType { get; set; } = ContainerPortMappingType.Default;
    public bool EnableTrafficCapture { get; set; }
    public string PublicEntry { get; set; } = string.Empty;
    public KubernetesConfig? KubernetesConfig { get; set; }
    public DockerConfig? DockerConfig { get; set; }
}

public class DockerConfig
{
    public string Uri { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public bool SwarmMode { get; set; } = false;
    public string? ChallengeNetwork { get; set; }
}

public class KubernetesConfig
{
    public string Namespace { get; set; } = "gzctf-challenges";
    public string KubeConfig { get; set; } = "kube-config.yaml";
    public string[]? AllowCidr { get; set; }
    public string[]? Dns { get; set; }
}

public class RegistrySet<T> : Dictionary<string, T>
    where T : class
{
    public T? GetForImage(string image)
    {
        if (string.IsNullOrWhiteSpace(image))
            return null;

        image = image.Contains("://") ? image : $"https://{image}";

        if (!Uri.TryCreate(image, UriKind.Absolute, out var uri) || uri.HostNameType == UriHostNameType.Unknown)
            return null;

        return TryGetValue(uri.Authority, out var cfg) ? cfg :
            TryGetValue(uri.Host, out var cfgHost) ? cfgHost : null;
    }
}

public class RegistryConfig
{
    public string? ServerAddress { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }

    public bool Valid => !string.IsNullOrEmpty(UserName) &&
                         !string.IsNullOrEmpty(Password);
}

#endregion

#region Captcha Provider

[JsonConverter(typeof(JsonStringEnumConverter<CaptchaProvider>))]
public enum CaptchaProvider
{
    None,
    HashPow,
    CloudflareTurnstile
}

public class HashPowConfig
{
    // How many leading zeros the hash should have
    private int _difficulty = 18;

    public int Difficulty
    {
        set => _difficulty = value;
        get => _difficulty = Math.Clamp(_difficulty, 8, 48);
    }
}

public class CaptchaConfig
{
    public CaptchaProvider Provider { get; set; }
    public string? SecretKey { get; set; }
    public string? SiteKey { get; set; }
    public HashPowConfig HashPow { get; set; } = new();
}

#endregion

#region Telemetry

public class TelemetryConfig
{
    public PrometheusConfig Prometheus { get; set; } = new();
    public OpenTelemetryConfig OpenTelemetry { get; set; } = new();
    public AzureMonitorConfig AzureMonitor { get; set; } = new();
    public ConsoleConfig Console { get; set; } = new();

    [JsonIgnore]
    public bool Enable => Prometheus.Enable || OpenTelemetry.Enable || AzureMonitor.Enable || Console.Enable;
}

public class PrometheusConfig
{
    public bool Enable { get; set; }
    public bool TotalNameSuffixForCounters { get; set; }
}

public class OpenTelemetryConfig
{
    public bool Enable { get; set; }
    public OtlpExportProtocol Protocol { get; set; }
    public string? EndpointUri { get; set; }
}

public class AzureMonitorConfig
{
    public bool Enable { get; set; }
    public string? ConnectionString { get; set; }
}

public class ConsoleConfig
{
    public bool Enable { get; set; }
}

#endregion

public class GrafanaLokiOptions
{
    public bool Enable { get; set; }
    public string? EndpointUri { get; set; }
    public LokiLabel[]? Labels { get; set; }
    public string[]? PropertiesAsLabels { get; set; }
    public LokiCredentials? Credentials { get; set; }
    public string? Tenant { get; set; }
    public LogLevel? MinimumLevel { get; set; }
}

public class ForwardedOptions : ForwardedHeadersOptions
{
    public List<string>? TrustedNetworks { get; set; }
    public List<string>? TrustedProxies { get; set; }

    public void ToForwardedHeadersOptions(ForwardedHeadersOptions options)
    {
        // assign the same value to the base class via reflection
        var type = typeof(ForwardedHeadersOptions);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            // skip the properties that are not being set directly
            if (property.Name is nameof(KnownNetworks) or nameof(KnownProxies))
                continue;

            property.SetValue(options, property.GetValue(this));
        }

        TrustedNetworks?.ForEach(network =>
        {
            // split the network into address and prefix length
            var parts = network.Split('/');
            if (parts.Length == 2 &&
                IPAddress.TryParse(parts[0], out var prefix) &&
                int.TryParse(parts[1], out var prefixLength))
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
        });

        TrustedProxies?.ForEach(proxy => proxy.ResolveIP().ToList().ForEach(ip => options.KnownProxies.Add(ip)));
    }
}
