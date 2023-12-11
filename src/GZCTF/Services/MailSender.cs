using System.Text;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace GZCTF.Services;

public class MailSender(
    IOptions<EmailConfig> options,
    IOptionsSnapshot<GlobalConfig> globalConfig,
    ILogger<MailSender> logger,
    IStringLocalizer<Program> localizer) : IMailSender
{
    readonly EmailConfig? _options = options.Value;

    public async Task<bool> SendEmailAsync(string subject, string content, string to)
    {
        if (_options?.SendMailAddress is null ||
            _options?.Smtp?.Host is null ||
            _options?.Smtp?.Port is null)
            return true;

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_options.SendMailAddress, _options.SendMailAddress));
        msg.To.Add(new MailboxAddress(to, to));
        msg.Subject = subject;
        msg.Body = new TextPart(TextFormat.Html) { Text = content };

        try
        {
            using var client = new SmtpClient();

            await client.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port.Value);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            await client.AuthenticateAsync(_options.UserName, _options.Password);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);

            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.MailSender_SendMail), to],
                TaskStatus.Success, LogLevel.Information);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, Program.StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)]);
            return false;
        }
    }

    public async Task SendUrlAsync(MailContent content)
    {
        var template = globalConfig.Value.EmailTemplate switch
        {
            GlobalConfig.DefaultEmailTemplate => localizer[nameof(Resources.Program.MailSender_Template)],
            _ => globalConfig.Value.EmailTemplate
        };

        // TODO: use a string formatter library
        // TODO: update default template with new names
        var emailContent = new StringBuilder(template)
            .Replace("{title}", content.Title)
            .Replace("{information}", content.Information)
            .Replace("{btnmsg}", content.ButtonMessage)
            .Replace("{email}", content.Email)
            .Replace("{userName}", content.UserName)
            .Replace("{url}", content.Url)
            .Replace("{nowtime}", content.Time)
            .ToString();

        if (!await SendEmailAsync(content.Title, emailContent, content.Email))
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)],
                TaskStatus.Failed);
    }

    public bool SendConfirmEmailUrl(string? userName, string? email, string? confirmLink) =>
        SendUrlIfPossible(userName, email, confirmLink, MailType.ConfirmEmail);

    public bool SendChangeEmailUrl(string? userName, string? email, string? resetLink) =>
        SendUrlIfPossible(userName, email, resetLink, MailType.ChangeEmail);

    public bool SendResetPasswordUrl(string? userName, string? email, string? resetLink) =>
        SendUrlIfPossible(userName, email, resetLink, MailType.ResetPassword);

    bool SendUrlIfPossible(string? userName, string? email, string? resetLink, MailType type)
    {
        if (_options?.SendMailAddress is null)
            return false;

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(resetLink))
        {
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.MailSender_InvalidRequest)],
                TaskStatus.Failed);
            return false;
        }

        var content = new MailContent(userName, email, resetLink, type, localizer);

        // do not await
        Task _ = SendUrlAsync(content);

        return true;
    }
}

/// <summary>
/// 邮件类型
/// </summary>
public enum MailType
{
    ConfirmEmail,
    ChangeEmail,
    ResetPassword
}

/// <summary>
/// 邮件内容
/// </summary>
public class MailContent(
    string userName,
    string email,
    string resetLink,
    MailType type,
    IStringLocalizer<Program> localizer)
{
    /// <summary>
    /// 邮件标题
    /// </summary>
    public string Title { get; set; } = type switch
    {
        MailType.ConfirmEmail => localizer[nameof(Resources.Program.MailSender_VerifyEmailTitle)],
        MailType.ChangeEmail => localizer[nameof(Resources.Program.MailSender_ChangeEmailTitle)],
        MailType.ResetPassword => localizer[nameof(Resources.Program.MailSender_ResetPasswordTitle)],
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>
    /// 邮件信息
    /// </summary>
    public string Information { get; set; } = type switch
    {
        MailType.ConfirmEmail => localizer[nameof(Resources.Program.MailSender_VerifyEmailContent), email],
        MailType.ChangeEmail => localizer[nameof(Resources.Program.MailSender_ChangeEmailContent)],
        MailType.ResetPassword => localizer[nameof(Resources.Program.MailSender_ResetPasswordContent)],
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>
    /// 邮件按钮显示内容
    /// </summary>
    public string ButtonMessage { get; set; } = type switch
    {
        MailType.ConfirmEmail => localizer[nameof(Resources.Program.MailSender_VerifyEmailButton)],
        MailType.ChangeEmail => localizer[nameof(Resources.Program.MailSender_ChangeEmailButton)],
        MailType.ResetPassword => localizer[nameof(Resources.Program.MailSender_ResetPasswordButton)],
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = userName;

    /// <summary>
    /// 用户邮箱
    /// </summary>
    public string Email { get; set; } = email;

    /// <summary>
    /// 邮件链接
    /// </summary>
    public string Url { get; set; } = resetLink;

    /// <summary>
    /// 发信时间
    /// </summary>
    public string Time { get; set; } = DateTimeOffset.UtcNow.ToString("u");
}