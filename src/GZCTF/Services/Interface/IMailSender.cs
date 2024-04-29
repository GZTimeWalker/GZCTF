using GZCTF.Models.Internal;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Services.Interface;

public interface IMailSender
{
    /// <summary>
    /// 发送邮件
    /// </summary>
    /// <param name="subject">主题</param>
    /// <param name="content">HTML内容</param>
    /// <param name="to">收件人</param>
    /// <returns>发送是否成功</returns>
    public Task<bool> SendEmailAsync(string subject, string content, string to);

    /// <summary>
    /// 发送带有链接的邮件
    /// </summary>
    /// <param name="content">邮件内容</param>
    public Task SendUrlAsync(MailContent content);

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
