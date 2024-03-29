﻿using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using GZCTF.Extensions;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

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
    /// 使用验证码校验
    /// </summary>
    public bool UseCaptcha { get; set; }

    /// <summary>
    /// 注册、更换邮箱、找回密码需要邮件确认
    /// </summary>
    public bool EmailConfirmationRequired { get; set; }

    /// <summary>
    /// 邮箱后缀域名，以逗号分割
    /// </summary>
    public string EmailDomainList { get; set; } = string.Empty;
}

/// <summary>
/// 容器策略
/// </summary>
public class ContainerPolicy
{
    /// <summary>
    /// 是否在达到数量限制时自动销毁最早的容器
    /// </summary>
    public bool AutoDestroyOnLimitReached { get; set; }

    /// <summary>
    /// 用户容器数量限制，用于限制练习题目的容器数量
    /// </summary>
    public int MaxExerciseContainerCountPerUser { get; set; } = 1;
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

    /// <summary>
    /// 邮件模版
    /// </summary>
    // TODO: email template validation for MailContent
    public string EmailTemplate { get; set; } = DefaultEmailTemplate;

    public const string DefaultEmailTemplate = "default";
}

#region Mail Config

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

#endregion

#region Container Provider

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContainerProviderType
{
    Docker,
    Kubernetes
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContainerPortMappingType
{
    // Use default to map the container port to a random port on the host
    Default,

    // Use platform proxy to map the container tcp to wss 
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
    public bool SwarmMode { get; set; } = false;
    public string? ChallengeNetwork { get; set; }
}

public class KubernetesConfig
{
    public string Namespace { get; set; } = "gzctf-challenges";
    public string KubeConfig { get; set; } = "kube-config.yaml";

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Replace)]
    public List<string> AllowCidr { get; set; } = ["10.0.0.0/8"];

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Replace)]
    public List<string> Dns { get; set; } = ["8.8.8.8", "223.5.5.5", "114.114.114.114"];
}

public class RegistryConfig
{
    public string? ServerAddress { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}

#endregion

#region Captcha Provider

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CaptchaProvider
{
    None,
    GoogleRecaptcha,
    CloudflareTurnstile
}

public class CaptchaConfig
{
    public CaptchaProvider Provider { get; set; }
    public string? SecretKey { get; set; }
    public string? SiteKey { get; set; }

    public GoogleRecaptchaConfig GoogleRecaptcha { get; set; } = new();
}

public class GoogleRecaptchaConfig
{
    public string VerifyApiAddress { get; set; } = "https://www.recaptcha.net/recaptcha/api/siteverify";
    public float RecaptchaThreshold { get; set; } = 0.5f;
}

#endregion

public class ForwardedOptions : ForwardedHeadersOptions
{
    public List<string>? TrustedNetworks { get; set; }
    public List<string>? TrustedProxies { get; set; }

    public void ToForwardedHeadersOptions(ForwardedHeadersOptions options)
    {
        // assign the same value to the base class via reflection
        Type type = typeof(ForwardedHeadersOptions);
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo property in properties)
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
                IPAddress.TryParse(parts[0], out IPAddress? prefix) &&
                int.TryParse(parts[1], out var prefixLength))
                options.KnownNetworks.Add(new IPNetwork(prefix, prefixLength));
        });

        TrustedProxies?.ForEach(proxy => proxy.ResolveIP().ToList().ForEach(ip => options.KnownProxies.Add(ip)));
    }
}