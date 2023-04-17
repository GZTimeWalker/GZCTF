﻿using System.Reflection;
using CTFServer.Models.Internal;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
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
            options?.Smtp?.Host is null ||
            options?.Smtp?.Port is null)
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

    public async Task SendUrlAsync(string? title, string? information, string? btnmsg, string? userName, string? email, string? url)
    {
        if (email is null || userName is null || title is null)
        {
            logger.SystemLog("无效的邮件发送调用！", TaskStatus.Fail);
            return;
        }

        string ns = typeof(MailSender).Namespace ?? "CTFServer.Services";
        Assembly asm = typeof(MailSender).Assembly;
        string resourceName = $"{ns}.Assets.URLEmailTemplate.html";
        string emailContent = await
            new StreamReader(asm.GetManifestResourceStream(resourceName)!)
            .ReadToEndAsync();
        emailContent = emailContent
            .Replace("{title}", title)
            .Replace("{information}", information)
            .Replace("{btnmsg}", btnmsg)
            .Replace("{email}", email)
            .Replace("{userName}", userName)
            .Replace("{url}", url)
            .Replace("{nowtime}", DateTimeOffset.UtcNow.ToString("u"));
        if (!await SendEmailAsync(title, emailContent, email))
            logger.SystemLog("邮件发送失败！", TaskStatus.Fail);
    }

    private bool SendUrlIfPossible(string? title, string? information, string? btnmsg, string? userName, string? email, string? url)
    {
        if (options?.SendMailAddress is null)
            return false;

        var _ = SendUrlAsync(title, information, btnmsg, userName, email, url);
        return true;
    }

    public bool SendConfirmEmailUrl(string? userName, string? email, string? confirmLink)
        => SendUrlIfPossible("验证邮箱",
            $"你正在进行账户注册操作，我们需要验证你的注册邮箱：{email}，请点击下方按钮进行验证。",
            "确认验证邮箱", userName, email, confirmLink);

    public bool SendChangeEmailUrl(string? userName, string? email, string? resetLink)
        => SendUrlIfPossible("更改邮箱",
            "你正在进行账户邮箱更换操作，请点击下方按钮验证你的新邮箱。",
            "确认跟换邮箱", userName, email, resetLink);

    public bool SendResetPasswordUrl(string? userName, string? email, string? resetLink)
        => SendUrlIfPossible("重置密码",
            "你正在进行账户密码重置操作，请点击下方按钮重置你的密码。",
            "确认重置密码", userName, email, resetLink);
}
