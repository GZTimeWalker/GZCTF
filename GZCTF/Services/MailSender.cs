using CTFServer.Models.Internal;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using Microsoft.Extensions.Options;
using System.Reflection;
using MailKit.Net.Smtp;
using MimeKit;

namespace CTFServer.Services;

public class MailSender : IMailSender
{
    private readonly EmailConfig? options;
    private readonly ILogger<MailSender> logger;

    public MailSender(IOptions<EmailConfig> options, ILogger<MailSender> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<bool> SendEmailAsync(string subject, string content, string to)
    {
        if (options?.SendMailAddress is null ||
            this.options?.Smtp?.Host is null ||
            this.options?.Smtp?.Port is null)
            return true;

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(options.SendMailAddress, options.SendMailAddress));
        msg.To.Add(new MailboxAddress(to, to));
        msg.Subject = subject;
        msg.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = content };

        try
        {
            using var client = new SmtpClient();

            await client.ConnectAsync(options.Smtp.Host, options.Smtp.Port.Value);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            await client.AuthenticateAsync(options.UserName, options.Password);
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

    public async Task SendUrl(string? title, string? infomation, string? btnmsg, string? userName, string? email, string? url)
    {
        if (email is null || userName is null || title is null)
        {
            logger.SystemLog("无效的邮件发送调用！", TaskStatus.Fail);
            return;
        }

        string _namespace = MethodBase.GetCurrentMethod()!.DeclaringType!.Namespace!;
        Assembly _assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"{_namespace}.Assets.URLEmailTemplate.html";
        string emailContent = await
            new StreamReader(_assembly.GetManifestResourceStream(resourceName)!)
            .ReadToEndAsync();
        emailContent = emailContent
            .Replace("{title}", title)
            .Replace("{infomation}", infomation)
            .Replace("{btnmsg}", btnmsg)
            .Replace("{email}", email)
            .Replace("{userName}", userName)
            .Replace("{url}", url)
            .Replace("{nowtime}", DateTimeOffset.UtcNow.ToString("u"));
        if (!await SendEmailAsync(title, emailContent, email))
            logger.SystemLog("邮件发送失败！", TaskStatus.Fail);
    }

    private bool SendUrlIfPossible(string? title, string? infomation, string? btnmsg, string? userName, string? email, string? url)
    {
        if (options?.SendMailAddress is null)
            return false;

        var _ = SendUrl(title, infomation, btnmsg, userName, email, url);
        return true;
    }

    public bool SendConfirmEmailUrl(string? userName, string? email, string? confirmLink)
        => SendUrlIfPossible("验证你的注册邮箱",
            "需要验证你的邮箱：" + email,
            "确认邮箱", userName, email, confirmLink);

    public bool SendResetPwdUrl(string? userName, string? email, string? resetLink)
        => SendUrlIfPossible("重置密码",
            "点击下方按钮重置你的密码。",
            "重置密码", userName, email, resetLink);

    public bool SendChangeEmailUrl(string? userName, string? email, string? resetLink)
        => SendUrlIfPossible("更改邮箱",
            "点击下方按钮更改你的邮箱。",
            "更改邮箱", userName, email, resetLink);

    public bool SendResetPasswordUrl(string? userName, string? email, string? resetLink)
        => SendUrlIfPossible("重置密码",
            "点击下方按钮重置你的密码。",
            "重置密码", userName, email, resetLink);
}
