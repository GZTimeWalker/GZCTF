using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using GZCTF.Extensions;
using Microsoft.AspNetCore.HttpOverrides;

namespace GZCTF.Models.Internal;

/// <summary>
/// 账户策略
/// </summary>
public class AccountPolicy
{
    /// <summary>
    /// 允许用户注册
    /// </summary>
    public bool AllowRegister { get; set; } = true;

    /// <summary>
    /// 注册时直接激活账户
    /// </summary>
    public bool ActiveOnRegister { get; set; } = true;

    /// <summary>
    /// 使用谷歌验证码校验
    /// </summary>
    public bool UseGoogleRecaptcha { get; set; } = false;

    /// <summary>
    /// 注册、更换邮箱、找回密码需要邮件确认
    /// </summary>
    public bool EmailConfirmationRequired { get; set; } = false;

    /// <summary>
    /// 邮箱后缀域名，以逗号分割
    /// </summary>
    public string EmailDomainList { get; set; } = string.Empty;
}

/// <summary>
/// 比赛策略
/// </summary>
public class GamePolicy
{
    /// <summary>
    /// 是否在达到数量限制时自动销毁最早的容器
    /// </summary>
    public bool AutoDestroyOnLimitReached { get; set; } = false;
}

/// <summary>
/// 全局设置
/// </summary>
public class GlobalConfig
{
    /// <summary>
    /// 平台前缀名称
    /// </summary>
    public string Title { get; set; } = "GZ";

    /// <summary>
    /// 平台标语
    /// </summary>
    public string Slogan { get; set; } = "Hack for fun not for profit";

    /// <summary>
    /// 页脚显示的信息
    /// </summary>
    public string? FooterInfo { get; set; }
}

public class SmtpConfig
{
    public string? Host { get; set; } = "127.0.0.1";
    public int? Port { get; set; } = 587;
}

public class EmailConfig
{
    public string? UserName { get; set; } = string.Empty;
    public string? Password { get; set; } = string.Empty;
    public string? SendMailAddress { get; set; } = string.Empty;
    public SmtpConfig? Smtp { get; set; } = new();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContainerProviderType
{
    Docker,
    Kubernetes
}

public class ContainerProvider
{
    public ContainerProviderType Type { get; set; } = ContainerProviderType.Docker;
    public string PublicEntry { get; set; } = string.Empty;

    public K8sConfig? K8sConfig { get; set; }
    public DockerConfig? DockerConfig { get; set; }
}

public class DockerConfig
{
    public string Uri { get; set; } = string.Empty;
    public bool SwarmMode { get; set; } = false;
}

public class K8sConfig
{
    public string Namespace { get; set; } = "gzctf-challenges";
    public string KubeConfig { get; set; } = "k8sconfig.yaml";

    // TODO：wait for JsonObjectCreationHandling release
    //public List<string> AllowCIDR { get; set; } = new() { "10.0.0.0/8" };
    //public List<string> DNS { get; set; } = new() { "8.8.8.8", "223.5.5.5", "114.114.114.114" };

    public string[]? AllowCIDR { get; set; }
    public string[]? DNS { get; set; }
}

public class RegistryConfig
{
    public string? ServerAddress { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}

public class RecaptchaConfig
{
    public string? Secretkey { get; set; }
    public string? SiteKey { get; set; }
    public string VerifyAPIAddress { get; set; } = "https://www.recaptcha.net/recaptcha/api/siteverify";
    public float RecaptchaThreshold { get; set; } = 0.5f;
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
            if (property.Name == nameof(KnownNetworks) ||
                property.Name == nameof(KnownProxies))
                continue;

            property.SetValue(options, property.GetValue(this));
        }

        TrustedNetworks?.ForEach((network) =>
        {
            // split the network into address and prefix length
            var parts = network.Split('/');
            if (parts.Length == 2 &&
                IPAddress.TryParse(parts[0], out var prefix) &&
                int.TryParse(parts[1], out var prefixLength))
            {
                options.KnownNetworks.Add(new IPNetwork(prefix, prefixLength));
            }
        });

        TrustedProxies?.ForEach((proxy) => proxy.ResolveIP().ToList().ForEach((ip) => options.KnownProxies.Add(ip)));
    }
}
