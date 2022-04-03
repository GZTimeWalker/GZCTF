using CTFServer.Services.Interface;
using CTFServer.Utils;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;

namespace CTFServer.Services;

public record SmtpOptions(string Host, ushort Port);
public record EmailOptions(string UserName, string Password, string SendMailAddress, SmtpOptions Smtp);

public class MailSender : IMailSender
{
    private readonly EmailOptions options;
    private readonly ILogger<MailSender> logger;
    private readonly SmtpClient smtp;

    public MailSender(IOptions<EmailOptions> options, ILogger<MailSender> logger)
    {
        this.options = options.Value;
        this.logger = logger;
        smtp = new()
        {
            Host = this.options.Smtp.Host,
            Port = this.options.Smtp.Port,
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(this.options.UserName, this.options.Password)
        };
    }

    public async Task<bool> SendEmailAsync(string subject, string content, string to)
    {
        var msg = new MailMessage
        {
            From = new MailAddress(options.SendMailAddress),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            Body = content,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
        };

        msg.To.Add(to);

        bool isSuccess;
        try
        {
            await smtp.SendMailAsync(msg);

            isSuccess = true;

            logger.SystemLog("发送邮件：" + to, TaskStatus.Success, LogLevel.Information);
        }
        catch (Exception e)
        {
            logger.LogError(e, "邮件发送遇到问题。");
            isSuccess = false;
        }

        return isSuccess;
    }

    public async void SendUrl(string? title, string? infomation, string? btnmsg, string? userName, string? email, string? url)
    {
        if (email is null || userName is null || title is null)
        {
            LogHelper.SystemLog(logger, "无效调用！", TaskStatus.Fail);
            return;
        }
        string _namespace = MethodBase.GetCurrentMethod()!.DeclaringType!.Namespace!;
        Assembly _assembly = Assembly.GetExecutingAssembly();
        string resourceName = _namespace + ".Assets.URLEmailTemplate.html";
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
            LogHelper.SystemLog(logger, "邮件发送失败！", TaskStatus.Fail);
    }

    public void SendConfirmEmailUrl(string? userName, string? email, string? confirmLink)
        => SendUrl("验证你的注册邮箱",
            "需要验证你的邮箱：" + email,
            "确认邮箱", userName, email, confirmLink);

    public void SendResetPwdUrl(string? userName, string? email, string? resetLink)
        => SendUrl("重置密码",
            "点击下方按钮重置你的密码。",
            "重置密码", userName, email, resetLink);

    public void SendChangeEmailUrl(string? userName, string? email, string? resetLink)
        => SendUrl("更改邮箱",
            "点击下方按钮更改你的邮箱。",
            "更改邮箱", userName, email, resetLink);

    public void SendResetPasswordUrl(string? userName, string? email, string? resetLink)
        => SendUrl("重置密码",
            "点击下方按钮重置你的密码。",
            "重置密码", userName, email, resetLink);
}
