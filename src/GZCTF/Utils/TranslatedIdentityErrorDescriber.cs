using Microsoft.AspNetCore.Identity;

namespace GZCTF.Utils;

public class TranslatedIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError ConcurrencyFailure() => new() { Code = nameof(ConcurrencyFailure), Description = "发生并发错误，请稍后再试" };

    public override IdentityError DefaultError() => new() { Code = nameof(DefaultError), Description = "发生错误，请稍后再试" };

    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = $"邮箱地址 {email} 已存在" };

    public override IdentityError DuplicateRoleName(string role) => new() { Code = nameof(DuplicateRoleName), Description = $"角色名 {role} 已存在" };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"用户名 {userName} 已存在" };

    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = $"邮箱地址 {email} 无效" };

    public override IdentityError InvalidRoleName(string? role) => new() { Code = nameof(InvalidRoleName), Description = $"角色名 {role} 无效" };

    public override IdentityError InvalidToken() => new() { Code = nameof(InvalidToken), Description = "验证码无效" };

    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = $"用户名 {userName} 无效" };

    public override IdentityError LoginAlreadyAssociated() => new() { Code = nameof(LoginAlreadyAssociated), Description = "登录已经关联" };

    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "密码输入错误" };

    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "密码中需要数字" };

    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "密码中需要小写字母" };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "密码中需要符号" };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"密码中至少需要 {uniqueChars} 种不同的字符" };

    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "密码中需要大写字母" };

    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"密码长度 {length} 太短" };

    public override IdentityError RecoveryCodeRedemptionFailed() => new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "恢复代码找回失败" };

    public override IdentityError UserAlreadyHasPassword() => new() { Code = nameof(UserAlreadyHasPassword), Description = "用户密码已存在" };

    public override IdentityError UserAlreadyInRole(string role) => new() { Code = nameof(UserAlreadyInRole), Description = $"用户已在角色 {role} 中" };

    public override IdentityError UserLockoutNotEnabled() => new() { Code = nameof(UserLockoutNotEnabled), Description = "用户锁定未启用" };

    public override IdentityError UserNotInRole(string role) => new() { Code = nameof(UserNotInRole), Description = $"用户在角色 {role} 中不存在" };
}