﻿using System.Threading.Tasks;
using GZCTF.Services.Interface;

namespace GZCTF.Test;

public class TestMailSender : IMailSender
{
    public bool SendChangeEmailUrl(string? userName, string? email, string? resetLink)
    {
        return true;
    }

    public bool SendConfirmEmailUrl(string? userName, string? email, string? confirmLink)
    {
        return true;
    }

    public Task<bool> SendEmailAsync(string subject, string content, string to)
    {
        return Task.FromResult(true);
    }

    public bool SendResetPasswordUrl(string? userName, string? email, string? resetLink)
    {
        return true;
    }

    public Task SendUrlAsync(string? title, string? infomation, string? btnmsg, string? userName, string? email,
        string? url)
    {
        return Task.CompletedTask;
    }

    public bool SendResetPwdUrl(string? userName, string? email, string? resetLink)
    {
        return true;
    }
}