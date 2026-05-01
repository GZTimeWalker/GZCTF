using System.Collections.Concurrent;
using System.Net.Security;
using System.Text;
using GZCTF.Models.Internal;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace GZCTF.Services.Mail;

public sealed class MailSender : IMailSender, IDisposable
{
    private readonly CancellationToken _cancellationToken;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ILogger<MailSender> _logger;
    private readonly ConcurrentQueue<MailContent> _mailQueue = new();
    private readonly EmailConfig? _options;
    private readonly AsyncManualResetEvent _resetEvent = new();
    private readonly SmtpClient? _smtpClient;
    private bool _disposed;

    public MailSender(
        IOptions<AccountPolicy> accountPolicy,
        IOptions<EmailConfig> options,
        ILogger<MailSender> logger)
    {
        _logger = logger;
        _options = options.Value;
        _cancellationToken = _cancellationTokenSource.Token;

        if (string.IsNullOrWhiteSpace(_options.SenderAddress) ||
            string.IsNullOrWhiteSpace(_options.Smtp?.Host) || _options.Smtp.Port is not > 0)
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

        _smtpClient.ServerCertificateValidationCallback = (_, _, _, errors)
            => errors is SslPolicyErrors.None || options.Value.Smtp?.BypassCertVerify is true;

        if (!TestSmtpClient())
        {
            if (accountPolicy.Value.EmailConfirmationRequired)
                ExitWithFatalMessage(StaticLocalizer[nameof(Resources.Program.MailSender_InvalidEmailConfig)]);

            _smtpClient.Dispose();
            _smtpClient = null;
            return;
        }

        _logger.SystemLog(StaticLocalizer[nameof(Resources.Program.MailSender_ConnectedToSmtp),
            $"{_options.Smtp.Host}:{_options.Smtp.Port}"], TaskStatus.Success, LogLevel.Debug);

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

    public async Task SendMailContent(MailContent content)
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

        var sender = string.IsNullOrWhiteSpace(_options!.SenderName) ? content.Platform : _options.SenderName;

        // SenderAddress is checked in constructor, so it won't be null here
        var from = new MailboxAddress(sender, _options.SenderAddress!);

        var to = new MailboxAddress(content.UserName, content.Email);

        if (!await SendEmailAsync(title, emailContent, from, to))
            _logger.SystemLog(StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)],
                TaskStatus.Failed);
    }

    public bool SendConfirmEmailUrl(string? userName, string? email, string? confirmLink,
        IStringLocalizer<Program> localizer, IOptionsSnapshot<GlobalConfig> options) =>
        EnqueueMailTask(userName, email, confirmLink, MailType.ConfirmEmail, localizer, options);

    public bool SendChangeEmailUrl(string? userName, string? email, string? resetLink,
        IStringLocalizer<Program> localizer, IOptionsSnapshot<GlobalConfig> options) =>
        EnqueueMailTask(userName, email, resetLink, MailType.ChangeEmail, localizer, options);

    public bool SendResetPasswordUrl(string? userName, string? email, string? resetLink,
        IStringLocalizer<Program> localizer, IOptionsSnapshot<GlobalConfig> options) =>
        EnqueueMailTask(userName, email, resetLink, MailType.ResetPassword, localizer, options);

    public async Task<(int Sent, int Failed)> SendCredentialsBatch(
        IEnumerable<(string UserName, string Email, string Password)> credentials,
        string loginUrl,
        IStringLocalizer<Program> localizer,
        IOptionsSnapshot<GlobalConfig> options,
        CancellationToken token = default)
    {
        var items = credentials.ToList();
        if (items.Count == 0)
            return (0, 0);

        if (_options?.Smtp?.Host is null || !(_options.Smtp.Port > 0) ||
            string.IsNullOrWhiteSpace(_options.SenderAddress))
            return (0, items.Count);

        var template = localizer[nameof(Resources.Program.MailSender_Template)].Value;
        var platform = options.Value.Platform;
        var sender = string.IsNullOrWhiteSpace(_options.SenderName) ? platform : _options.SenderName;
        var from = new MailboxAddress(sender, _options.SenderAddress);
        var nowTime = DateTimeOffset.UtcNow.ToString("u");

        int sent = 0, failed = 0;

        using var client = new SmtpClient();
        client.AuthenticationMechanisms.Remove("XOAUTH2");
        client.ServerCertificateValidationCallback = (_, _, _, errors)
            => errors is SslPolicyErrors.None || _options.Smtp.BypassCertVerify is true;

        if (!OperatingSystem.IsWindows())
            client.SslCipherSuitesPolicy = new CipherSuitesPolicy(Enum.GetValues<TlsCipherSuite>()
                .Where(cipher =>
                {
                    var n = cipher.ToString();
                    return !n.EndsWith("MD5") && !n.EndsWith("SHA") && !n.EndsWith("NULL");
                }));

        try
        {
            await client.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port.Value, cancellationToken: token);
            await client.AuthenticateAsync(_options.UserName, _options.Password, token);

            foreach (var (userName, email, password) in items)
            {
                var info =
                    "<p>Your account has been created. Here are your login credentials:</p>" +
                    $"<p><strong>Username:</strong> <code>{userName}</code><br/>" +
                    $"<strong>Password:</strong> <code>{password}</code></p>" +
                    "<p>Please change your password immediately after your first login.</p>";

                var body = new StringBuilder(template)
                    .Replace("{title}", "Your Account Credentials")
                    .Replace("{information}", info)
                    .Replace("{btnmsg}", "Log In Now")
                    .Replace("{email}", email)
                    .Replace("{userName}", userName)
                    .Replace("{url}", loginUrl)
                    .Replace("{nowtime}", nowTime)
                    .Replace("{platform}", platform)
                    .ToString();

                using var msg = new MimeMessage();
                msg.From.Add(from);
                msg.To.Add(new MailboxAddress(userName, email));
                msg.Subject = $"Your Account Credentials - {platform}";
                msg.Body = new TextPart(TextFormat.Html) { Text = body };

                try
                {
                    await client.SendAsync(msg, token);
                    sent++;
                    _logger.SystemLog(StaticLocalizer[nameof(Resources.Program.MailSender_SendMail), email],
                        TaskStatus.Success, LogLevel.Information);
                }
                catch (Exception e)
                {
                    failed++;
                    _logger.LogErrorMessage(e, StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)]);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogErrorMessage(e, StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)]);
            failed += items.Count - sent - failed;
        }
        finally
        {
            try { await client.DisconnectAsync(true, token); } catch { }
        }

        return (sent, failed);
    }

    private async Task<bool> SendEmailAsync(string subject, string content, MailboxAddress from, MailboxAddress to)
    {
        if (_smtpClient is null)
            return false;

        using var msg = new MimeMessage();
        msg.From.Add(from);
        msg.To.Add(to);
        msg.Subject = subject;
        msg.Body = new TextPart(TextFormat.Html) { Text = content };

        try
        {
            await _smtpClient.SendAsync(msg, _cancellationToken);

            _logger.SystemLog(StaticLocalizer[nameof(Resources.Program.MailSender_SendMail), to],
                TaskStatus.Success, LogLevel.Information);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogErrorMessage(e, StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)]);
            return false;
        }
    }

    private async Task MailSenderWorker()
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

                while (_mailQueue.TryDequeue(out var content))
                    await SendMailContent(content);
            }
            catch (Exception e)
            {
                // Failed to establish SMTP connection, clear the queue
                _mailQueue.Clear();

                _logger.LogErrorMessage(e, StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)]);
            }
            finally
            {
                await _smtpClient.DisconnectAsync(true, _cancellationToken);
            }
        }
    }

    private bool EnqueueMailTask(string? userName, string? email, string? resetLink, MailType type,
        IStringLocalizer<Program> localizer, IOptionsSnapshot<GlobalConfig> options)
    {
        if (_smtpClient is null)
            return false;

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(resetLink))
        {
            _logger.SystemLog(StaticLocalizer[nameof(Resources.Program.MailSender_InvalidRequest)],
                TaskStatus.Failed);
            return false;
        }

        var content = new MailContent(userName, email, resetLink, type, localizer, options);

        _mailQueue.Enqueue(content);
        _resetEvent.Set();

        return true;
    }

    private bool TestSmtpClient(CancellationToken token = default)
    {
        if (_smtpClient is null)
            return false;

        try
        {
            _smtpClient.Connect(_options!.Smtp!.Host, _options.Smtp.Port!.Value, cancellationToken: token);
            _smtpClient.Authenticate(_options.UserName, _options.Password, token);
            _smtpClient.Disconnect(true, token);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "{msg}",
                StaticLocalizer[nameof(Resources.Program.MailSender_MailSendFailed)]);
            return false;
        }
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
    public string Platform { get; } = globalConfig.Value.Platform;
}
