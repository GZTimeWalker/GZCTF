using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using CTFServer.Middlewares;
using CTFServer.Models;
using CTFServer.Models.Request;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using NLog;
using System.Net.Mime;

namespace CTFServer.Controllers;

/// <summary>
/// 用户账户相关接口
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
[Produces(MediaTypeNames.Application.Json)]
public class AccountController : ControllerBase
{
    private static readonly Logger logger = LogManager.GetLogger("AccountController");
    private readonly IMailSender mailSender;
    private readonly UserManager<UserInfo> userManager;
    private readonly SignInManager<UserInfo> signInManager;
    private readonly IMemoryCache cache;

    public AccountController(
        IMailSender _mailSender,
        IMemoryCache memoryCache,
        UserManager<UserInfo> _userManager,
        SignInManager<UserInfo> _signInManager)
    {
        cache = memoryCache;
        mailSender = _mailSender;
        userManager = _userManager;
        signInManager = _signInManager;
    }

    /// <summary>
    /// 用户注册接口
    /// </summary>
    /// <remarks>
    /// 使用此接口注册新用户，邮件URL：/verify
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">注册成功</response>
    /// <response code="400">校验失败或用户已存在</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var user = new UserInfo
        {
            UserName = model.UserName,
            Email = model.Email,
            Privilege = Privilege.User,
        };

        user.UpdateByHttpContext(HttpContext);

        var result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            var current = await userManager.FindByEmailAsync(model.Email);

            if (current is null)
                return BadRequest(new RequestResponse(result.Errors.FirstOrDefault()?.Description ?? "Unknown"));

            if (await userManager.IsEmailConfirmedAsync(current))
                return BadRequest(new RequestResponse("此账户已存在。"));

            user = current;
        }

        LogHelper.Log(logger, "发送用户邮箱验证邮件。", user, TaskStatus.Pending);

        mailSender.SendConfirmEmailUrl(user.UserName, user.Email,
            "https://" + HttpContext.Request.Host.ToString()
            + "/verify?token=" + Codec.Base64.Encode(await userManager.GenerateEmailConfirmationTokenAsync(user))
            + "&email=" + Codec.Base64.Encode(model.Email));

        return Ok();
    }

    /// <summary>
    /// 用户找回密码请求接口
    /// </summary>
    /// <remarks>
    /// 使用此接口请求找回密码，向用户邮箱发送邮件，邮件URL：/reset
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">用户密码重置邮件发送成功</response>
    /// <response code="400">校验失败</response>
    /// <response code="404">用户不存在</response>
    [HttpPost]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Recovery([FromBody] RecoveryModel model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
            return NotFound(new RequestResponse("用户不存在",404));

        LogHelper.Log(logger, "发送用户密码重置邮件。", user.UserName, HttpContext, TaskStatus.Pending);

        mailSender.SendResetPasswordUrl(user.UserName, user.Email,
            "https://" + HttpContext.Request.Host.ToString()
            + "/reset?token=" + Codec.Base64.Encode(await userManager.GeneratePasswordResetTokenAsync(user))
            + "&email=" + Codec.Base64.Encode(model.Email));

        return Ok(new RequestResponse("邮件发送成功", 200));
    }

    /// <summary>
    /// 用户重置密码接口
    /// </summary>
    /// <remarks>
    /// 使用此接口重置密码，需要邮箱验证码
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">用户成功重置密码</response>
    /// <response code="400">校验失败</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PasswordReset([FromBody] PasswordResetModel model)
    {
        var user = await userManager.FindByEmailAsync(Codec.Base64.Decode(model.Email));
        if (user is null)
            return BadRequest(new RequestResponse("无效的邮件地址"));

        user.UpdateByHttpContext(HttpContext);
        
        var result = await userManager.ResetPasswordAsync(user, Codec.Base64.Decode(model.RToken), model.Password);

        if (!result.Succeeded)
            return BadRequest(new RequestResponse(result.Errors.FirstOrDefault()?.Description ?? "Unknown"));

        LogHelper.Log(logger, "用户成功重置密码。", user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// 用户邮箱确认接口
    /// </summary>
    /// <remarks>
    /// 使用此接口通过邮箱验证码确认邮箱
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">用户通过邮箱验证</response>
    /// <response code="400">校验失败</response>
    /// <response code="401">邮箱验证失败</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verify([FromBody] AccountVerifyModel model)
    {
        var user = await userManager.FindByEmailAsync(Codec.Base64.Decode(model.Email));
        if (user is null)
            return BadRequest(new RequestResponse("无效的邮件地址"));

        if (user.EmailConfirmed)
            return Ok();

        var result = await userManager.ConfirmEmailAsync(user, Codec.Base64.Decode(model.Token));

        if (!result.Succeeded)
            return Unauthorized(new RequestResponse("邮箱验证失败", 401));

        LogHelper.Log(logger, "通过邮箱验证。", user, TaskStatus.Success);
        await signInManager.SignInAsync(user, true);

        user.LastSignedInUTC = DateTimeOffset.UtcNow;
        user.LastVisitedUTC = DateTimeOffset.UtcNow;
        user.RegisterTimeUTC = DateTimeOffset.UtcNow;
        //user.Rank = new()
        //{
        //    UpdateTimeUTC = DateTimeOffset.UtcNow,
        //};

        result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return BadRequest(new RequestResponse(result.Errors.FirstOrDefault()?.Description ?? "Unknown"));

        return Ok();
    }

    /// <summary>
    /// 用户登录接口
    /// </summary>
    /// <remarks>
    /// 使用此接口登录账户
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">用户成功登录</response>
    /// <response code="400">校验失败</response>
    /// <response code="401">用户名或密码错误</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogIn([FromBody] LoginModel model)
    {
        var user = await userManager.FindByNameAsync(model.UserName);
        if (user is null)
            user = await userManager.FindByEmailAsync(model.UserName);

        if (user is null)
            return Unauthorized(new RequestResponse("用户名或密码错误", 401));

        user.LastSignedInUTC = DateTimeOffset.UtcNow;
        user.UpdateByHttpContext(HttpContext);

        await signInManager.SignOutAsync();

        var result = await signInManager.PasswordSignInAsync(user, model.Password, true, false);

        if (!result.Succeeded)
            return Unauthorized(new RequestResponse("用户名或密码错误", 401));

        LogHelper.Log(logger, "用户成功登录。", user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// 用户登出接口
    /// </summary>
    /// <remarks>
    /// 使用此接口登出账户，需要SignedIn权限
    /// </remarks>
    /// <response code="200">用户已登出</response>
    /// <response code="401">无权访问</response>
    [HttpPost]
    [RequireSignedIn]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LogOut()
    {
        await signInManager.SignOutAsync();

        return Ok();
    }

    /// <summary>
    /// 用户数据更新接口
    /// </summary>
    /// <remarks>
    /// 使用此接口更新用户用户名和描述，需要SignedIn权限
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">用户数据成功更新</response>
    /// <response code="400">校验失败或用户数据更新失败</response>
    /// <response code="401">无权访问</response>
    [HttpPut]
    [RequireSignedIn]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] ProfileUpdateModel model)
    {
        var user = await userManager.GetUserAsync(User);
        var oname = user.UserName;

        user.UserName = model.UserName ?? user.UserName;
        user.Bio = model.Descr ?? user.Bio;
        user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;

        var result = await userManager.UpdateAsync(user);

        if(model.Descr is not null)
            cache.Remove(CacheKey.ScoreBoard);

        if (!result.Succeeded)
            return BadRequest(new RequestResponse(result.Errors.FirstOrDefault()?.Description ?? "Unknown"));

        if (oname != model.UserName)
            LogHelper.Log(logger, "用户更新：" + oname + "=>" + model.UserName, user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// 用户密码更改接口
    /// </summary>
    /// <remarks>
    /// 使用此接口更新用户密码，需要SignedIn权限
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">用户成功更新密码</response>
    /// <response code="400">校验失败或用户密码更新失败</response>
    /// <response code="401">无权访问</response>
    [HttpPut]
    [RequireSignedIn]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeModel model)
    {
        var user = await userManager.GetUserAsync(User);
        var result = await userManager.ChangePasswordAsync(user, model.Old, model.New);

        if (!result.Succeeded)
            return BadRequest(new RequestResponse(result.Errors.FirstOrDefault()?.Description ?? "Unknown"));

        LogHelper.Log(logger, "用户更新密码。", user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// 用户邮箱更改接口
    /// </summary>
    /// <remarks>
    /// 使用此接口更改用户邮箱，需要SignedIn权限，邮件URL：/confirm
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">成功发送用户邮箱更改邮件</response>
    /// <response code="400">校验失败或邮箱已经被占用</response>
    /// <response code="401">无权访问</response>
    [HttpPut]
    [RequireSignedIn]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeEmail([FromBody] MailChangeModel model)
    {
        if (await userManager.FindByEmailAsync(model.NewMail) is not null)
            return BadRequest(new RequestResponse("邮箱已经被占用。"));

        var user = await userManager.GetUserAsync(User);
        LogHelper.Log(logger, "发送用户邮箱更改邮件。", user, TaskStatus.Pending);

        mailSender.SendChangeEmailUrl(user.UserName, model.NewMail,
            "https://" + HttpContext.Request.Host.ToString()
            + "/confirm?token=" + Codec.Base64.Encode(await userManager.GenerateChangeEmailTokenAsync(user, model.NewMail))
            + "&email=" + Codec.Base64.Encode(model.NewMail));

        return Ok();
    }

    /// <summary>
    /// 用户邮箱更改确认接口
    /// </summary>
    /// <remarks>
    /// 使用此接口确认更改用户邮箱，需要邮箱验证码，需要SignedIn权限
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">用户成功更改邮箱</response>
    /// <response code="400">校验失败或无效邮箱</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">无权访问</response>
    [HttpPost]
    [RequireSignedIn]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MailChangeConfirm([FromBody] AccountVerifyModel model)
    {
        var user = await userManager.GetUserAsync(User);
        var result = await userManager.ChangeEmailAsync(user, Codec.Base64.Decode(model.Email), Codec.Base64.Decode(model.Token));

        if (!result.Succeeded)
            return BadRequest(new RequestResponse("无效邮箱。"));

        result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return BadRequest(new RequestResponse(result.Errors.FirstOrDefault()?.Description ?? "Unknown"));

        LogHelper.Log(logger, "更改邮箱成功。", user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// 获取用户信息接口
    /// </summary>
    /// <remarks>
    /// 使用此接口获取用户信息，需要SignedIn权限
    /// </remarks>
    /// <response code="200">用户成功获取信息</response>
    /// <response code="401">未授权用户</response>
    [HttpPost]
    [RequireSignedIn]
    [ProducesResponseType(typeof(ClientUserInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var user = await userManager.GetUserAsync(User);
        return Ok(new ClientUserInfoModel()
        {
                Description = user.Bio,
                Email = user.Email, 
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber
        });
    }

    /// <summary>
    /// 删除一个用户
    /// </summary>
    /// <remarks>
    /// 使用此接口删除用户，需要Admin权限，不可删除自己
    /// </remarks>
    /// <param name="Id">用户Id</param>
    /// <response code="200">用户成功被删除</response>
    /// <response code="400">删除失败</response>
    [HttpDelete("{Id:guid}")]
    [RequireAdmin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Remove(string Id)
    {
        var user = await userManager.GetUserAsync(User);

        if(Id == user.Id)
            return BadRequest(new RequestResponse("正尝试删除自己的账号。"));

        user = await userManager.FindByIdAsync(Id);

        if(user is null)
            return BadRequest(new RequestResponse("未找到对应用户。"));

        var result = await userManager.DeleteAsync(user);
        if(!result.Succeeded)
            return BadRequest(new RequestResponse("删除失败。"));

        return Ok();
    }
}
