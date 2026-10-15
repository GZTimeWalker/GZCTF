using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GZCTF.Controllers;
using GZCTF.Extensions.Startup;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Services.Config;
using GZCTF.Utils;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace GZCTF.Test.UnitTests.Account;

// Exercise the real Identity passkey handler, protected ceremony cookies and controller with a software ES256 authenticator.
public sealed class PasskeyTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly WebApplicationBuilder _builder;
    private readonly Dictionary<string, string> _cookies = [];
    private readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly byte[] _credentialId = RandomNumberGenerator.GetBytes(32);
    private readonly TestUserManager _users;
    private readonly UserInfo _user = new() { UserName = "passkey-test", EmailConfirmed = true };

    public PasskeyTests()
    {
        var builder = WebApplication.CreateBuilder();
        _builder = builder;
        builder.Environment.EnvironmentName = "Development";
        builder.WebHost.UseTestServer();
        builder.ConfigureIdentity();
        builder.AddWebServices();
        builder.AddDevelopmentServices();
        builder.Services.AddControllers().AddApplicationPart(typeof(PasskeyController).Assembly);
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql());
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddLocalization();
        builder.Services.AddSingleton<Serilog.IDiagnosticContext>(new Serilog.Extensions.Hosting.DiagnosticContext(Serilog.Log.Logger));
        builder.Services.AddSingleton<TestUserManager>();
        builder.Services.AddSingleton<UserManager<UserInfo>>(sp => sp.GetRequiredService<TestUserManager>());
        builder.Services.AddOptions<GlobalConfig>();
        builder.Services.AddOptions<ManagedConfig>();
        builder.Services.AddScoped<IConfigService>(sp => new ConfigService(null!, null!,
            NullLogger<ConfigService>.Instance, sp.GetRequiredService<IOptionsSnapshot<GlobalConfig>>(),
            sp.GetRequiredService<IOptionsSnapshot<ManagedConfig>>(), builder.Configuration));
        _services = builder.Services.BuildServiceProvider();
        _users = _services.GetRequiredService<TestUserManager>();
        _users.UsersById.Add(_user.Id.ToString(), _user);
    }

    [Fact]
    public async Task Register_Login_Rename_Remove_WithRealSignedCredential()
    {
        await Register();
        var passkey = Assert.Single(_users.Passkeys[_user.Id]);
        Assert.Equal("My device", passkey.Name);
        Assert.True(passkey.IsUserVerified);

        var options = Assert.IsType<ContentResult>(await Request(c => c.RequestOptions()));
        Assert.IsType<OkResult>(await Request(c => c.LogIn(new() { CredentialJson = Assertion(options.Content!) })));
        Assert.Contains("GZCTF_Token", _cookies.Keys);
        Assert.Equal(1u, passkey.SignCount);
        Assert.True(_user.LastSignedInUtc > DateTimeOffset.UtcNow.AddMinutes(-1));

        var id = Base64(_credentialId);
        Assert.IsType<OkResult>(await Request(c => c.Rename(id, new() { Name = "Renamed" }), _user));
        var list = Assert.IsType<OkObjectResult>(await Request(c => c.List(), _user));
        Assert.Equal("Renamed", Assert.Single(Assert.IsType<PasskeyModel[]>(list.Value)).Name);
        Assert.IsType<OkResult>(await Request(c => c.Remove(id), _user));
        Assert.Empty(_users.Passkeys[_user.Id]);

        options = Assert.IsType<ContentResult>(await Request(c => c.RequestOptions()));
        AssertStatus(401, await Request(c => c.LogIn(new() { CredentialJson = Assertion(options.Content!) })));
    }

    [Theory]
    [InlineData("banned")]
    [InlineData("unconfirmed")]
    [InlineData("locked")]
    [InlineData("save-failed")]
    public async Task Login_DoesNotIssueCookie_WhenAccountOrPersistenceRejectsIt(string reason)
    {
        await Register();
        if (reason == "banned") _user.Role = Role.Banned;
        if (reason == "unconfirmed") _user.EmailConfirmed = false;
        if (reason == "locked") _users.Locked = true;
        if (reason == "save-failed") _users.FailSave = true;
        var options = Assert.IsType<ContentResult>(await Request(c => c.RequestOptions()));
        AssertStatus(401, await Request(c => c.LogIn(new() { CredentialJson = Assertion(options.Content!) })));
        Assert.DoesNotContain("GZCTF_Token", _cookies.Keys);
    }

    [Theory]
    [InlineData("origin")]
    [InlineData("challenge")]
    [InlineData("signature")]
    [InlineData("verification")]
    public async Task Login_RejectsInvalidWebAuthnProof(string failure)
    {
        await Register();
        var options = Assert.IsType<ContentResult>(await Request(c => c.RequestOptions()));
        var credential = Assertion(options.Content!, failure);
        AssertStatus(401, await Request(c => c.LogIn(new() { CredentialJson = credential })));
        Assert.DoesNotContain("GZCTF_Token", _cookies.Keys);
    }

    [Fact]
    public async Task Login_RejectsReusedOrMissingCeremony()
    {
        await Register();
        var options = Assert.IsType<ContentResult>(await Request(c => c.RequestOptions()));
        var credential = Assertion(options.Content!);
        Assert.IsType<OkResult>(await Request(c => c.LogIn(new() { CredentialJson = credential })));
        AssertStatus(401, await Request(c => c.LogIn(new() { CredentialJson = credential })));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not-json")]
    public async Task Login_RejectsMalformedCredential(string credential)
    {
        await Request(c => c.RequestOptions());
        AssertStatus(401, await Request(c => c.LogIn(new() { CredentialJson = credential })));
    }

    [Fact]
    public async Task Registration_CannotBeTransferredToAnotherAccount()
    {
        var options = Assert.IsType<ContentResult>(await Request(c => c.CreationOptions(new() { Password = "correct" }), _user));
        var other = new UserInfo { UserName = "other", EmailConfirmed = true };
        _users.UsersById.Add(other.Id.ToString(), other);
        AssertStatus(400, await Request(c => c.Register(new()
        {
            Name = "Injected", CredentialJson = Attestation(options.Content!)
        }), other));
        Assert.Empty(_users.Passkeys);
    }

    [Fact]
    public async Task Registration_RequiresPasswordAndHonorsLimit()
    {
        AssertStatus(400, await Request(c => c.CreationOptions(new() { Password = "wrong" }), _user));
        Assert.Empty(_cookies);
        await Register();
        var passkey = Assert.Single(_users.Passkeys[_user.Id]);
        _users.Passkeys[_user.Id] = Enumerable.Repeat(passkey, 10).ToList();
        AssertStatus(400, await Request(c => c.CreationOptions(new() { Password = "correct" }), _user));
    }

    [Fact]
    public async Task Management_CannotAccessAnotherUsersPasskeys()
    {
        await Register();
        var other = new UserInfo { UserName = "other" };
        _users.UsersById.Add(other.Id.ToString(), other);
        Assert.IsType<NotFoundResult>(await Request(c => c.Rename(Base64(_credentialId), new() { Name = "Stolen" }), other));
        Assert.IsType<NotFoundResult>(await Request(c => c.Remove(Base64(_credentialId)), other));
        Assert.IsType<NotFoundResult>(await Request(c => c.Remove("invalid!"), _user));
        Assert.Single(_users.Passkeys[_user.Id]);
    }

    [Fact]
    public async Task Antiforgery_RequiresMatchingRequestToken()
    {
        using var scope = _services.CreateScope();
        var context = NewContext(scope.ServiceProvider);
        var antiforgery = scope.ServiceProvider.GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(context);
        StoreCookies(context);

        context = NewContext(scope.ServiceProvider);
        context.Request.Method = "POST";
        await Assert.ThrowsAsync<AntiforgeryValidationException>(() => antiforgery.ValidateRequestAsync(context));
        context = NewContext(scope.ServiceProvider);
        context.Request.Method = "POST";
        context.Request.Headers["X-CSRF-TOKEN"] = tokens.RequestToken;
        await antiforgery.ValidateRequestAsync(context);
        Assert.NotNull(typeof(PasskeyController).GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), true).SingleOrDefault());
    }

    [Fact]
    public async Task HttpEndpoints_RequireAuthenticationAndCsrf()
    {
        await using var app = _builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.UseOpenApi(options => options.Path = "/openapi/{documentName}.json");
        app.MapControllers();
        await app.StartAsync();
        using var client = app.GetTestClient();
        client.BaseAddress = new Uri("https://localhost");

        var anonymous = await client.GetAsync("/api/passkey");
        Assert.True(anonymous.StatusCode == HttpStatusCode.Unauthorized, await anonymous.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/passkey/options", null)).StatusCode);
        var response = await client.GetAsync("/api/passkey/token");
        response.EnsureSuccessStatusCode();
        var csrf = await response.Content.ReadFromJsonAsync<PasskeyAntiforgeryModel>();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
        client.DefaultRequestHeaders.Add("Cookie", string.Join("; ",
            SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToArray()).Select(c => $"{c.Name}={c.Value}")));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/passkey/options", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/passkey/credential")).StatusCode);

        var openApi = await client.GetStringAsync("/openapi/v1.json");
        Assert.Contains("/api/passkey/login", openApi);
    }

    [Fact]
    public void MigrationModel_IncludesPasskeysWithoutChangingExistingIdentityColumns()
    {
        using var context = new DesignTimeAppDbContextFactory().CreateDbContext([]);
        var entity = context.Model.FindEntityType(typeof(IdentityUserPasskey<Guid>));
        Assert.NotNull(entity);
        Assert.Equal("AspNetUserPasskeys", entity.GetTableName());
        Assert.Equal(DeleteBehavior.Cascade, Assert.Single(entity.GetForeignKeys()).DeleteBehavior);
        Assert.Equal("text", context.Model.FindEntityType(typeof(UserInfo))!.FindProperty("PhoneNumber")!.GetColumnType());
        Assert.False(context.Database.HasPendingModelChanges());
    }

    private async Task Register()
    {
        var options = Assert.IsType<ContentResult>(await Request(c => c.CreationOptions(new() { Password = "correct" }), _user));
        Assert.IsType<OkResult>(await Request(c => c.Register(new()
        {
            Name = "My device", CredentialJson = Attestation(options.Content!)
        }), _user));
    }

    private async Task<IActionResult> Request(Func<PasskeyController, Task<IActionResult>> action, UserInfo? user = null)
    {
        using var scope = _services.CreateScope();
        var context = NewContext(scope.ServiceProvider);
        if (user is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], "test"));
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
        var controller = ActivatorUtilities.CreateInstance<PasskeyController>(scope.ServiceProvider);
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        var result = await action(controller);
        StoreCookies(context);
        return result;
    }

    private DefaultHttpContext NewContext(IServiceProvider services)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.Request.Headers.Origin = "https://localhost";
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Request.Headers.Cookie = string.Join("; ", _cookies.Select(c => $"{c.Key}={c.Value}"));
        return context;
    }

    private void StoreCookies(HttpContext context)
    {
        foreach (var cookie in SetCookieHeaderValue.ParseList(context.Response.Headers.SetCookie.Select(c => c!).ToArray()))
        {
            if (cookie.Value.HasValue && cookie.Expires is not { Year: < 2000 })
                _cookies[cookie.Name.ToString()] = cookie.Value.ToString();
            else
                _cookies.Remove(cookie.Name.ToString());
        }
    }

    private string Attestation(string options)
    {
        using var json = JsonDocument.Parse(options);
        var clientData = JsonSerializer.SerializeToUtf8Bytes(new
        {
            type = "webauthn.create", challenge = json.RootElement.GetProperty("challenge").GetString(), origin = "https://localhost"
        });
        var point = _key.ExportParameters(false).Q;
        // COSE EC2 key: kty=2, alg=-7, crv=1, x and y are 32-byte coordinates.
        byte[] cose = [0xa5, 0x01, 0x02, 0x03, 0x26, 0x20, 0x01, 0x21, 0x58, 0x20, ..point.X!, 0x22, 0x58, 0x20, ..point.Y!];
        byte[] authData = [..SHA256.HashData(Encoding.UTF8.GetBytes("localhost")), 0x45, 0, 0, 0, 0,
            ..new byte[16], 0, 32, .._credentialId, ..cose];
        // CBOR { fmt: "none", attStmt: {}, authData: bytes }.
        byte[] attestation = [0xa3, 0x63, ..Encoding.UTF8.GetBytes("fmt"), 0x64, ..Encoding.UTF8.GetBytes("none"),
            0x67, ..Encoding.UTF8.GetBytes("attStmt"), 0xa0, 0x68, ..Encoding.UTF8.GetBytes("authData"),
            0x58, (byte)authData.Length, ..authData];
        return JsonSerializer.Serialize(new
        {
            id = Base64(_credentialId), rawId = Base64(_credentialId), type = "public-key",
            response = new { clientDataJSON = Base64(clientData), attestationObject = Base64(attestation), transports = new[] { "internal" } },
            clientExtensionResults = new { }
        });
    }

    private string Assertion(string options, string? failure = null)
    {
        using var json = JsonDocument.Parse(options);
        var clientData = JsonSerializer.SerializeToUtf8Bytes(new
        {
            type = "webauthn.get",
            challenge = failure == "challenge" ? Base64(new byte[32]) : json.RootElement.GetProperty("challenge").GetString(),
            origin = failure == "origin" ? "https://attacker.example" : "https://localhost"
        });
        var authData = new byte[37];
        SHA256.HashData(Encoding.UTF8.GetBytes("localhost")).CopyTo(authData, 0);
        authData[32] = failure == "verification" ? (byte)0x01 : (byte)0x05;
        BinaryPrimitives.WriteUInt32BigEndian(authData.AsSpan(33), 1);
        var signature = _key.SignData([..authData, ..SHA256.HashData(clientData)], HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);
        if (failure == "signature") signature[^1] ^= 1;
        return JsonSerializer.Serialize(new
        {
            id = Base64(_credentialId), rawId = Base64(_credentialId), type = "public-key",
            response = new
            {
                clientDataJSON = Base64(clientData), authenticatorData = Base64(authData), signature = Base64(signature),
                userHandle = Base64(Encoding.UTF8.GetBytes(_user.Id.ToString()))
            },
            clientExtensionResults = new { }
        });
    }

    private static string Base64(byte[] bytes) => Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(bytes);
    private static void AssertStatus(int status, IActionResult result) => Assert.Equal(status, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);

    public void Dispose()
    {
        _key.Dispose();
        _services.Dispose();
    }

    private sealed class TestUserManager(IServiceProvider services) : UserManager<UserInfo>(new EmptyStore(),
        services.GetRequiredService<IOptions<IdentityOptions>>(), new PasswordHasher<UserInfo>(), [], [],
        new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), services, NullLogger<UserManager<UserInfo>>.Instance)
    {
        public Dictionary<string, UserInfo> UsersById { get; } = [];
        public Dictionary<Guid, List<UserPasskeyInfo>> Passkeys { get; } = [];
        public bool Locked { get; set; }
        public bool FailSave { get; set; }
        public override bool SupportsUserPasskey => true;
        public override bool SupportsUserLockout => true;
        public override bool SupportsUserSecurityStamp => false;
        public override bool SupportsUserEmail => true;
        public override Task<UserInfo?> FindByIdAsync(string userId) => Task.FromResult(UsersById.GetValueOrDefault(userId));
        public override Task<string> GetUserIdAsync(UserInfo user) => Task.FromResult(user.Id.ToString());
        public override Task<string?> GetUserNameAsync(UserInfo user) => Task.FromResult(user.UserName);
        public override Task<string?> GetEmailAsync(UserInfo user) => Task.FromResult(user.Email);
        public override Task<bool> IsEmailConfirmedAsync(UserInfo user) => Task.FromResult(user.EmailConfirmed);
        public override Task<bool> IsLockedOutAsync(UserInfo user) => Task.FromResult(Locked);
        public override Task<bool> CheckPasswordAsync(UserInfo user, string password) => Task.FromResult(password == "correct");
        public override Task<IdentityResult> ResetAccessFailedCountAsync(UserInfo user) => Task.FromResult(IdentityResult.Success);
        public override Task<IdentityResult> AccessFailedAsync(UserInfo user) => Task.FromResult(IdentityResult.Success);
        public override Task<IList<UserPasskeyInfo>> GetPasskeysAsync(UserInfo user) =>
            Task.FromResult<IList<UserPasskeyInfo>>(Passkeys.GetValueOrDefault(user.Id) ?? []);
        public override Task<UserPasskeyInfo?> GetPasskeyAsync(UserInfo user, byte[] credentialId) =>
            Task.FromResult(Passkeys.GetValueOrDefault(user.Id)?.SingleOrDefault(p => p.CredentialId.SequenceEqual(credentialId)));
        public override Task<UserInfo?> FindByPasskeyIdAsync(byte[] credentialId) => Task.FromResult(UsersById.Values
            .SingleOrDefault(u => Passkeys.GetValueOrDefault(u.Id)?.Any(p => p.CredentialId.SequenceEqual(credentialId)) == true));
        public override Task<IdentityResult> AddOrUpdatePasskeyAsync(UserInfo user, UserPasskeyInfo passkey)
        {
            if (FailSave) return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure" }));
            if (!Passkeys.TryGetValue(user.Id, out var list)) Passkeys[user.Id] = list = [];
            list.RemoveAll(p => p.CredentialId.SequenceEqual(passkey.CredentialId));
            list.Add(passkey);
            return Task.FromResult(IdentityResult.Success);
        }
        public override Task<IdentityResult> RemovePasskeyAsync(UserInfo user, byte[] credentialId)
        {
            Passkeys[user.Id].RemoveAll(p => p.CredentialId.SequenceEqual(credentialId));
            return Task.FromResult(IdentityResult.Success);
        }
    }

    private sealed class EmptyStore : IUserStore<UserInfo>
    {
        public void Dispose() { }
        public Task<string> GetUserIdAsync(UserInfo user, CancellationToken token) => Task.FromResult(user.Id.ToString());
        public Task<string?> GetUserNameAsync(UserInfo user, CancellationToken token) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(UserInfo user, string? name, CancellationToken token) => throw new NotSupportedException();
        public Task<string?> GetNormalizedUserNameAsync(UserInfo user, CancellationToken token) => throw new NotSupportedException();
        public Task SetNormalizedUserNameAsync(UserInfo user, string? name, CancellationToken token) => throw new NotSupportedException();
        public Task<IdentityResult> CreateAsync(UserInfo user, CancellationToken token) => throw new NotSupportedException();
        public Task<IdentityResult> UpdateAsync(UserInfo user, CancellationToken token) => throw new NotSupportedException();
        public Task<IdentityResult> DeleteAsync(UserInfo user, CancellationToken token) => throw new NotSupportedException();
        public Task<UserInfo?> FindByIdAsync(string id, CancellationToken token) => throw new NotSupportedException();
        public Task<UserInfo?> FindByNameAsync(string name, CancellationToken token) => throw new NotSupportedException();
    }
}
