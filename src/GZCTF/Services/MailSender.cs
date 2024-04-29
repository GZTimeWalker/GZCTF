﻿using System.Collections.Concurrent;
using System.Net.Security;
using System.Text;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace GZCTF.Services;

public sealed class MailSender : IMailSender, IDisposable
{
    readonly CancellationToken _cancellationToken;
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly ILogger<MailSender> _logger;
    readonly ConcurrentQueue<MailContent> _mailQueue = new();
    readonly EmailConfig? _options;
    readonly AsyncManualResetEvent _resetEvent = new();
    readonly SmtpClient? _smtpClient;
    bool _disposed;

    public MailSender(
        IOptions<EmailConfig> options,
        ILogger<MailSender> logger)
    {
        _logger = logger;
        _options = options.Value;
        _cancellationToken = _cancellationTokenSource.Token;

        if (_options is not { SendMailAddress: not null, Smtp.Host: not null, Smtp.Port: not null })
            return;

        _smtpClient = new();
        _smtpClient.AuthenticationMechanisms.Remove("XOAUTH2");

        if (!OperatingSystem.IsWindows())
            // Some systems may not enable old (non-recommend) ciphers in TLS configuration and lead to failures when
            // connecting to some SMTP servers, override the default policy to include all ciphers except MD5, SHA1, and NULL
            _smtpClient.SslCipherSuitesPolicy = new CipherSuitesPolicy(Enum.GetValues<TlsCipherSuite>()
                .Where(cipher =>
                {
                    var cipherName = cipher.ToString();
                    // Exclude MD5, SHA1, and NULL ciphers for security reasons
                    return !cipherName.EndsWith("MD5") && !cipherName.EndsWith("SHA") &&
                           !cipherName.EndsWith("NULL");
                }));

        Task.Factory.StartNew(MailSenderWorker, _cancellationToken, TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cancellationTokenSource.Cancel();
        _smtpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<bool> SendEmailAsync(string subject, string content, string to)
    {
        using var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_options!.SendMailAddress, _options.SendMailAddress));
        msg.To.Add(new MailboxAddress(to, to));
        msg.Subject = subject;
        msg.Body = new TextPart(TextFormat.Html) { Text = content };

        try
        {
            await _smtpClient!.SendAsync(msg, _cancellationToken);

            _logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.MailSender_SendMail), to],
                TaskStatus.Success, LogLevel.Information);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, Program.StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)]);
            return false;
        }
    }

    public async Task SendUrlAsync(MailContent content)
    {
        // TODO: use GlobalConfig.DefaultEmailTemplate
        // TODO: use a string formatter library
        // TODO: update default template with new names
        var emailContent = new StringBuilder(content.Template)
            .Replace("{title}", content.Title)
            .Replace("{information}", content.Information)
            .Replace("{btnmsg}", content.ButtonMessage)
            .Replace("{email}", content.Email)
            .Replace("{userName}", content.UserName)
            .Replace("{url}", content.Url)
            .Replace("{nowtime}", content.Time)
            .Replace("{platform}", content.Platform)
            .ToString();

        var title = $"{content.Title} - {content.Platform}";

        if (!await SendEmailAsync(title, emailContent, content.Email))
            _logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)],
                TaskStatus.Failed);
    }

    public bool SendConfirmEmailUrl(string? userName, string? email, string? confirmLink,
        IStringLocalizer<Program> localizer, IOptionsSnapshot<GlobalConfig> options) =>
        SendUrlIfPossible(userName, email, confirmLink, MailType.ConfirmEmail, localizer, options);

    public bool SendChangeEmailUrl(string? userName, string? email, string? resetLink,
        IStringLocalizer<Program> localizer, IOptionsSnapshot<GlobalConfig> options) =>
        SendUrlIfPossible(userName, email, resetLink, MailType.ChangeEmail, localizer, options);

    public bool SendResetPasswordUrl(string? userName, string? email, string? resetLink,
        IStringLocalizer<Program> localizer, IOptionsSnapshot<GlobalConfig> options) =>
        SendUrlIfPossible(userName, email, resetLink, MailType.ResetPassword, localizer, options);

    async Task MailSenderWorker()
    {
        if (_smtpClient is null)
            return;

        while (!_cancellationToken.IsCancellationRequested)
        {
            await _resetEvent.WaitAsync(_cancellationToken);
            _resetEvent.Reset();

            try
            {
                if (!_smtpClient.IsConnected)
                    await _smtpClient.ConnectAsync(_options!.Smtp!.Host, _options.Smtp.Port!.Value,
                        cancellationToken: _cancellationToken);

                if (!_smtpClient.IsAuthenticated)
                    await _smtpClient.AuthenticateAsync(_options!.UserName, _options.Password,
                        _cancellationToken);

                while (_mailQueue.TryDequeue(out MailContent? content))
                    await SendUrlAsync(content);
            }
            catch (Exception e)
            {
                // Failed to establish SMTP connection, clear the queue
                _mailQueue.Clear();

                _logger.LogError(e, Program.StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)]);
            }
            finally
            {
                await _smtpClient.DisconnectAsync(true, _cancellationToken);
            }
        }
    }

    bool SendUrlIfPossible(string? userName, string? email, string? resetLink, MailType type,
        IStringLocalizer<Program> localizer, IOptionsSnapshot<GlobalConfig> options)
    {
        if (_smtpClient is null)
            return false;

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(resetLink))
        {
            _logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.MailSender_InvalidRequest)],
                TaskStatus.Failed);
            return false;
        }

        var content = new MailContent(userName, email, resetLink, type, localizer, options);

        _mailQueue.Enqueue(content);
        _resetEvent.Set();

        return true;
    }

    ~MailSender()
    {
        Dispose();
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
    // DO NOT use IStringLocalizer<Program> after construction
    IStringLocalizer<Program> localizer,
    IOptionsSnapshot<GlobalConfig> globalConfig)
{
    /// <summary>
    /// 邮件模板
    /// </summary>
    public string Template { get; } = localizer[nameof(Resources.Program.MailSender_Template)];

    /// <summary>
    /// 邮件标题
    /// </summary>
    public string Title { get; } = type switch
    {
        MailType.ConfirmEmail => localizer[nameof(Resources.Program.MailSender_VerifyEmailTitle)],
        MailType.ChangeEmail => localizer[nameof(Resources.Program.MailSender_ChangeEmailTitle)],
        MailType.ResetPassword => localizer[nameof(Resources.Program.MailSender_ResetPasswordTitle)],
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>
    /// 邮件信息
    /// </summary>
    public string Information { get; } = type switch
    {
        MailType.ConfirmEmail => localizer[nameof(Resources.Program.MailSender_VerifyEmailContent), email],
        MailType.ChangeEmail => localizer[nameof(Resources.Program.MailSender_ChangeEmailContent)],
        MailType.ResetPassword => localizer[nameof(Resources.Program.MailSender_ResetPasswordContent)],
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>
    /// 邮件按钮显示内容
    /// </summary>
    public string ButtonMessage { get; } = type switch
    {
        MailType.ConfirmEmail => localizer[nameof(Resources.Program.MailSender_VerifyEmailButton)],
        MailType.ChangeEmail => localizer[nameof(Resources.Program.MailSender_ChangeEmailButton)],
        MailType.ResetPassword => localizer[nameof(Resources.Program.MailSender_ResetPasswordButton)],
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; } = userName;

    /// <summary>
    /// 用户邮箱
    /// </summary>
    public string Email { get; } = email;

    /// <summary>
    /// 邮件链接
    /// </summary>
    public string Url { get; } = resetLink;

    /// <summary>
    /// 发信时间
    /// </summary>
    public string Time { get; } = DateTimeOffset.UtcNow.ToString("u");

    /// <summary>
    /// 平台名称
    /// </summary>
    public string Platform { get; } = $"{globalConfig.Value.Title}::CTF";
}
