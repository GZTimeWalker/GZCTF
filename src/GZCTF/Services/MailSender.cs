using System.Reflection;
using System.Text;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace GZCTF.Services;

public class MailSender(IOptions<EmailConfig> options, ILogger<MailSender> logger) : IMailSender
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

            logger.SystemLog("发送邮件：" + to, TaskStatus.Success, LogLevel.Information);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "邮件发送遇到问题");
            return false;
        }
    }

    public async Task SendUrlAsync(string? title, string? information, string? btnmsg, string? userName, string? email,
        string? url)
    {
        if (email is null || userName is null || title is null)
        {
            logger.SystemLog("无效的邮件发送调用！", TaskStatus.Failed);
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
            logger.SystemLog("邮件发送失败！", TaskStatus.Failed);
    }

    public bool SendConfirmEmailUrl(string? userName, string? email, string? confirmLink)
    {
        return SendUrlIfPossible("验证邮箱",
            $"你正在进行账户注册操作，我们需要验证你的注册邮箱：{email}，请点击下方按钮进行验证。",
            "确认验证邮箱", userName, email, confirmLink);
    }

    public bool SendChangeEmailUrl(string? userName, string? email, string? resetLink)
    {
        return SendUrlIfPossible("更换邮箱",
            "你正在进行账户邮箱更换操作，请点击下方按钮验证你的新邮箱。",
            "确认更换邮箱", userName, email, resetLink);
    }

    public bool SendResetPasswordUrl(string? userName, string? email, string? resetLink)
    {
        return SendUrlIfPossible("重置密码",
            "你正在进行账户密码重置操作，请点击下方按钮重置你的密码。",
            "确认重置密码", userName, email, resetLink);
    }

    bool SendUrlIfPossible(string? title, string? information, string? btnmsg, string? userName, string? email,
        string? url)
    {
        if (_options?.SendMailAddress is null)
            return false;

        Task _ = SendUrlAsync(title, information, btnmsg, userName, email, url);
        return true;
    }
}