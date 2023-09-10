using System.Threading.Tasks;
using GZCTF.Services.Interface;

namespace GZCTF.Test;

public class TestMailSender : IMailSender
{
    public bool SendChangeEmailUrl(string? userName, string? email, string? resetLink) => true;

    public bool SendConfirmEmailUrl(string? userName, string? email, string? confirmLink) => true;

    public Task<bool> SendEmailAsync(string subject, string content, string to) => Task.FromResult(true);

    public bool SendResetPasswordUrl(string? userName, string? email, string? resetLink) => true;

    public Task SendUrlAsync(string? title, string? infomation, string? btnmsg, string? userName, string? email,
        string? url) =>
        Task.CompletedTask;

    public bool SendResetPwdUrl(string? userName, string? email, string? resetLink) => true;
}