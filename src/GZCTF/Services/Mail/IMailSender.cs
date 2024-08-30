using GZCTF.Models.Internal;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Services.Mail;

public interface IMailSender
{
    /// <summary>
    /// 发送带邮件内容
    /// </summary>
    /// <param name="content">邮件内容</param>
    public Task SendMailContent(MailContent content);

    /// <summary>
    /// 发送新用户验证URL
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="email">用户新注册的Email</param>
    /// <param name="confirmLink">确认链接</param>
    /// <param name="localizer">本地化</param>
    /// <param name="options">全局配置</param>
    public bool SendConfirmEmailUrl(string? userName, string? email, string? confirmLink,
        IStringLocalizer<Program> localizer, IOptionsSnapshot<GlobalConfig> options);

    /// <summary>
    /// 发送邮箱重置邮件
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="email">用户的电子邮件</param>
    /// <param name="resetLink">重置链接</param>
    /// <param name="localizer">本地化</param>
    /// <param name="options">全局配置</param>
    public bool SendChangeEmailUrl(string? userName, string? email, string? resetLink,
        IStringLocalizer<Program> localizer, IOptionsSnapshot<GlobalConfig> options);

    /// <summary>
    /// 发送密码重置邮件
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="email">用户的电子邮件</param>
    /// <param name="resetLink">重置链接</param>
    /// <param name="localizer">本地化</param>
    /// <param name="options">全局配置</param>
    public bool SendResetPasswordUrl(string? userName, string? email, string? resetLink,
        IStringLocalizer<Program> localizer, IOptionsSnapshot<GlobalConfig> options);
}
