using System.Reflection;
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

            logger.SystemLog(localizer["MailSender_SendMail", to], TaskStatus.Success, LogLevel.Information);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, localizer["MailSender_MailSendFailed"]);
            return false;
        }
    }

    public async Task SendUrlAsync(string? title, string? information, string? btnmsg, string? userName, string? email,
        string? url)
    {
        if (email is null || userName is null || title is null)
        {
            logger.SystemLog(localizer["MailSender_InvalidRequest"], TaskStatus.Failed);
            return;
        }

        var ns = typeof(MailSender).Namespace ?? "GZCTF.Services";
        Assembly asm = typeof(MailSender).Assembly;
        var resourceName = $"{ns}.Assets.URLEmailTemplate.html";
        var emailContent = await
            new StreamReader(asm.GetManifestResourceStream(resourceName)!)
                .ReadToEndAsync();

        emailContent = new StringBuilder(emailContent)
            .Replace("{title}", title)
            .Replace("{information}", information)
            .Replace("{btnmsg}", btnmsg)
            .Replace("{email}", email)
            .Replace("{userName}", userName)
            .Replace("{url}", url)
            .Replace("{nowtime}", DateTimeOffset.UtcNow.ToString("u"))
            .ToString();

        if (!await SendEmailAsync(title, emailContent, email))
            logger.SystemLog(localizer["MailSender_MailSendFailed"], TaskStatus.Failed);
    }

    public bool SendConfirmEmailUrl(string? userName, string? email, string? confirmLink) =>
        SendUrlIfPossible(localizer["MailSender_VerifyEmailTitle"],
            localizer["MailSender_VerifyEmailContent", email ?? ""],
            localizer["MailSender_VerifyEmailButton"], userName, email, confirmLink);

    public bool SendChangeEmailUrl(string? userName, string? email, string? resetLink) =>
        SendUrlIfPossible(localizer["MailSender_ChangeEmailTitle"],
            localizer["MailSender_ChangeEmailContent"],
            localizer["MailSender_ChangeEmailButton"], userName, email, resetLink);

    public bool SendResetPasswordUrl(string? userName, string? email, string? resetLink) =>
        SendUrlIfPossible(localizer["MailSender_ResetPasswordTitle"],
            localizer["MailSender_ResetPasswordContent"],
            localizer["MailSender_ResetPasswordButton"], userName, email, resetLink);

    bool SendUrlIfPossible(string? title, string? information, string? btnmsg, string? userName, string? email,
        string? url)
    {
        if (_options?.SendMailAddress is null)
            return false;

        Task _ = SendUrlAsync(title, information, btnmsg, userName, email, url);
        return true;
    }
}