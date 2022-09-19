using System.Text.Json.Serialization;

namespace CTFServer.Models.Internal;

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
/// 全局设置
/// </summary>
public class GlobalConfig
{
    /// <summary>
    /// 平台前缀名称
    /// </summary>
    public string Title { get; set; } = "GZ";
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

    public DockerConfig? DockerConfig { get; set; }
}

public class DockerConfig
{
    public string Uri { get; set; } = string.Empty;
    public bool SwarmMode { get; set; } = false;
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