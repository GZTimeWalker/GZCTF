using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace GZCTF.Utils;

public class TranslatedIdentityErrorDescriber(IStringLocalizer<Program> localizer) : IdentityErrorDescriber
{
    public override IdentityError ConcurrencyFailure() => new()
    {
        Code = nameof(ConcurrencyFailure),
        Description = localizer[nameof(Resources.Program.Identity_ConcurrencyFailure)]
    };

    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = localizer[nameof(Resources.Program.Identity_DefaultError)] };

    public override IdentityError DuplicateEmail(string email) => new()
    {
        Code = nameof(DuplicateEmail),
        Description = localizer[nameof(Resources.Program.Identity_DuplicateEmail), email]
    };

    public override IdentityError DuplicateRoleName(string role) => new()
    {
        Code = nameof(DuplicateRoleName),
        Description = localizer[nameof(Resources.Program.Identity_DuplicateRoleName), role]
    };

    public override IdentityError DuplicateUserName(string userName) =>
        new()
        {
            Code = nameof(DuplicateUserName),
            Description = localizer[nameof(Resources.Program.Identity_DuplicateUserName), userName]
        };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = nameof(InvalidEmail),
        Description = localizer[nameof(Resources.Program.Identity_InvalidEmail), email ?? "null"]
    };

    public override IdentityError InvalidRoleName(string? role) => new()
    {
        Code = nameof(InvalidRoleName),
        Description = localizer[nameof(Resources.Program.Identity_InvalidRoleName), role ?? "null"]
    };

    public override IdentityError InvalidToken() =>
        new() { Code = nameof(InvalidToken), Description = localizer[nameof(Resources.Program.Identity_InvalidToken)] };

    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = nameof(InvalidUserName),
        Description = localizer[nameof(Resources.Program.Identity_InvalidUserName), userName ?? "null"]
    };

    public override IdentityError LoginAlreadyAssociated() => new()
    {
        Code = nameof(LoginAlreadyAssociated),
        Description = localizer[nameof(Resources.Program.Identity_LoginAlreadyAssociated)]
    };

    public override IdentityError PasswordMismatch() => new()
    {
        Code = nameof(PasswordMismatch),
        Description = localizer[nameof(Resources.Program.Identity_PasswordMismatch)]
    };

    public override IdentityError PasswordRequiresDigit() => new()
    {
        Code = nameof(PasswordRequiresDigit),
        Description = localizer[nameof(Resources.Program.Identity_PasswordRequiresDigit)]
    };

    public override IdentityError PasswordRequiresLower() => new()
    {
        Code = nameof(PasswordRequiresLower),
        Description = localizer[nameof(Resources.Program.Identity_PasswordRequiresLower)]
    };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new()
        {
            Code = nameof(PasswordRequiresNonAlphanumeric),
            Description = localizer[nameof(Resources.Program.Identity_PasswordRequiresNonAlphanumeric)]
        };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new()
        {
            Code = nameof(PasswordRequiresUniqueChars),
            Description = localizer[nameof(Resources.Program.Identity_PasswordRequiresUniqueChars), uniqueChars]
        };

    public override IdentityError PasswordRequiresUpper() => new()
    {
        Code = nameof(PasswordRequiresUpper),
        Description = localizer[nameof(Resources.Program.Identity_PasswordRequiresUpper)]
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = localizer[nameof(Resources.Program.Identity_PasswordTooShort), length]
    };

    public override IdentityError RecoveryCodeRedemptionFailed() => new()
    {
        Code = nameof(RecoveryCodeRedemptionFailed),
        Description = localizer[nameof(Resources.Program.Identity_RecoveryCodeRedemptionFailed)]
    };

    public override IdentityError UserAlreadyHasPassword() => new()
    {
        Code = nameof(UserAlreadyHasPassword),
        Description = localizer[nameof(Resources.Program.Identity_UserAlreadyHasPassword)]
    };

    public override IdentityError UserAlreadyInRole(string role) => new()
    {
        Code = nameof(UserAlreadyInRole),
        Description = localizer[nameof(Resources.Program.Identity_UserAlreadyInRole), role]
    };

    public override IdentityError UserLockoutNotEnabled() => new()
    {
        Code = nameof(UserLockoutNotEnabled),
        Description = localizer[nameof(Resources.Program.Identity_UserLockoutNotEnabled)]
    };

    public override IdentityError UserNotInRole(string role) => new()
    {
        Code = nameof(UserNotInRole),
        Description = localizer[nameof(Resources.Program.Identity_UserNotInRole), role]
    };
}
