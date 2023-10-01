using System.Text;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace GZCTF.Services;

public class MailSender(IOptions<EmailConfig> options, ILogger<MailSender> logger, IStringLocalizer<Program> localizer) : IMailSender
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

            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.MailSender_SendMail), to], TaskStatus.Success, LogLevel.Information);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, Program.StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)]);
            return false;
        }
    }

    public async Task SendUrlAsync(string? title, string? information, string? btnmsg, string? userName, string? email,
        string? url)
    {
        if (email is null || userName is null || title is null)
        {
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.MailSender_InvalidRequest)], TaskStatus.Failed);
            return;
        }

        var emailContent = new StringBuilder(localizer[nameof(Resources.Program.MailSender_Template)])
            .Replace("{title}", title)
            .Replace("{information}", information)
            .Replace("{btnmsg}", btnmsg)
            .Replace("{email}", email)
            .Replace("{userName}", userName)
            .Replace("{url}", url)
            .Replace("{nowtime}", DateTimeOffset.UtcNow.ToString("u"))
            .ToString();

        if (!await SendEmailAsync(title, emailContent, email))
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)], TaskStatus.Failed);
    }

    public bool SendConfirmEmailUrl(string? userName, string? email, string? confirmLink) =>
        SendUrlIfPossible(localizer[nameof(Resources.Program.MailSender_VerifyEmailTitle)],
            localizer[nameof(Resources.Program.MailSender_VerifyEmailContent), email ?? ""],
            localizer[nameof(Resources.Program.MailSender_VerifyEmailButton)], userName, email, confirmLink);

    public bool SendChangeEmailUrl(string? userName, string? email, string? resetLink) =>
        SendUrlIfPossible(localizer[nameof(Resources.Program.MailSender_ChangeEmailTitle)],
            localizer[nameof(Resources.Program.MailSender_ChangeEmailContent)],
            localizer[nameof(Resources.Program.MailSender_ChangeEmailButton)], userName, email, resetLink);

    public bool SendResetPasswordUrl(string? userName, string? email, string? resetLink) =>
        SendUrlIfPossible(localizer[nameof(Resources.Program.MailSender_ResetPasswordTitle)],
            localizer[nameof(Resources.Program.MailSender_ResetPasswordContent)],
            localizer[nameof(Resources.Program.MailSender_ResetPasswordButton)], userName, email, resetLink);

    bool SendUrlIfPossible(string? title, string? information, string? btnmsg, string? userName, string? email,
        string? url)
    {
        if (_options?.SendMailAddress is null)
            return false;

        Task _ = SendUrlAsync(title, information, btnmsg, userName, email, url);
        return true;
    }
}