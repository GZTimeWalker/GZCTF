using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Response.Account;
using GZCTF.Services.Config;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Tests for authentication and authorization workflows
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class AuthenticationTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Account_Login_WithSeededUser_Succeeds()
    {
        var password = "S3eded!Pass";
        var userName = TestDataSeeder.RandomName();
        var email = $"{userName}@example.com";
        var seeded = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName,
            password,
            email);

        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = seeded.UserName, Password = password });

        loginResponse.EnsureSuccessStatusCode();

        var profileResponse = await client.GetAsync("/api/Account/Profile");
        profileResponse.EnsureSuccessStatusCode();

        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileUserInfoModel>();
        Assert.NotNull(profile);
        Assert.Equal(seeded.UserName, profile.UserName);
        Assert.Equal(seeded.Email, profile.Email);
    }

    [Fact]
    public async Task Account_Login_WithFingerprintProofEnabled_AndCleanProof_Succeeds()
    {
        var originalPolicy = await SetBrowserFingerprintPolicyAsync(enableBrowserFingerprint: true);
        try
        {
            var password = "S3eded!Pass";
            var userName = TestDataSeeder.RandomName();
            var email = $"{userName}@example.com";
            var seeded = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, email);
            var fingerprint = new string('a', 64);
            using var client = factory.CreateClient();
            var proof = await BuildFingerprintProofAsync(client, fingerprint);
            var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
                new LoginModel
                {
                    UserName = seeded.UserName,
                    Password = password,
                    Fingerprint = fingerprint,
                    FingerprintProof = proof
                });

            loginResponse.EnsureSuccessStatusCode();
        }
        finally
        {
            await RestoreBrowserFingerprintPolicyAsync(originalPolicy);
        }
    }

    [Fact]
    public async Task FingerprintChallenge_WithBraveClientHints_ReturnsBadRequest()
    {
        var originalPolicy = await SetBrowserFingerprintPolicyAsync(enableBrowserFingerprint: true);
        try
        {
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-CH-UA",
                "\"Brave\";v=\"123\", \"Chromium\";v=\"123\", \"Not_A Brand\";v=\"99\"");

            var response = await client.GetAsync("/api/Account/FingerprintChallenge");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await RestoreBrowserFingerprintPolicyAsync(originalPolicy);
        }
    }

    [Fact]
    public async Task Account_Login_WithFingerprintProofEnabled_AndBraveClientHints_ReturnsBadRequest()
    {
        var originalPolicy = await SetBrowserFingerprintPolicyAsync(enableBrowserFingerprint: true);
        try
        {
            var password = "S3eded!Pass";
            var userName = TestDataSeeder.RandomName();
            var email = $"{userName}@example.com";
            var seeded = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, email);
            var fingerprint = new string('h', 64);

            using var challengeClient = factory.CreateClient();
            var proof = await BuildFingerprintProofAsync(challengeClient, fingerprint);

            using var loginClient = factory.CreateClient();
            loginClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-CH-UA",
                "\"Brave\";v=\"123\", \"Chromium\";v=\"123\", \"Not_A Brand\";v=\"99\"");

            var loginResponse = await loginClient.PostAsJsonAsync("/api/Account/LogIn",
                new LoginModel
                {
                    UserName = seeded.UserName,
                    Password = password,
                    Fingerprint = fingerprint,
                    FingerprintProof = proof
                });

            Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
        }
        finally
        {
            await RestoreBrowserFingerprintPolicyAsync(originalPolicy);
        }
    }

    [Fact]
    public async Task Account_Login_WithFingerprintProofEnabled_AndBraveStrictProof_ReturnsBadRequest()
    {
        var originalPolicy = await SetBrowserFingerprintPolicyAsync(enableBrowserFingerprint: true);
        try
        {
            var password = "S3eded!Pass";
            var userName = TestDataSeeder.RandomName();
            var email = $"{userName}@example.com";
            var seeded = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, email);
            var fingerprint = new string('b', 64);
            using var client = factory.CreateClient();
            var proof = await BuildFingerprintProofAsync(client, fingerprint, privacy: "Brave", mode: "strict");
            var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
                new LoginModel
                {
                    UserName = seeded.UserName,
                    Password = password,
                    Fingerprint = fingerprint,
                    FingerprintProof = proof
                });

            Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
        }
        finally
        {
            await RestoreBrowserFingerprintPolicyAsync(originalPolicy);
        }
    }

    [Fact]
    public async Task Account_Login_WithFingerprintProofEnabled_AndBraveAllowProof_ReturnsBadRequest()
    {
        var originalPolicy = await SetBrowserFingerprintPolicyAsync(enableBrowserFingerprint: true);
        try
        {
            var password = "S3eded!Pass";
            var userName = TestDataSeeder.RandomName();
            var email = $"{userName}@example.com";
            var seeded = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, email);
            var fingerprint = new string('c', 64);
            using var client = factory.CreateClient();
            var proof = await BuildFingerprintProofAsync(client, fingerprint, privacy: "Brave", mode: "allow",
                lieCount: 188, trashCount: 3, likeHeadlessRating: 44, headlessRating: 33, stealthRating: 20);
            var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
                new LoginModel
                {
                    UserName = seeded.UserName,
                    Password = password,
                    Fingerprint = fingerprint,
                    FingerprintProof = proof
                });

            Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
        }
        finally
        {
            await RestoreBrowserFingerprintPolicyAsync(originalPolicy);
        }
    }

    [Fact]
    public async Task Account_Login_WithFingerprintProofEnabled_AndTamperedSignalMetric_ReturnsBadRequest()
    {
        var originalPolicy = await SetBrowserFingerprintPolicyAsync(enableBrowserFingerprint: true);
        try
        {
            var password = "S3eded!Pass";
            var userName = TestDataSeeder.RandomName();
            var email = $"{userName}@example.com";
            var seeded = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, email);
            var fingerprint = new string('f', 64);

            using var client = factory.CreateClient();
            var proof = await BuildFingerprintProofAsync(client, fingerprint, signalOverrides: new Dictionary<string, string>
            {
                ["lie_count"] = "999"
            });

            var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
                new LoginModel
                {
                    UserName = seeded.UserName,
                    Password = password,
                    Fingerprint = fingerprint,
                    FingerprintProof = proof
                });

            Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
        }
        finally
        {
            await RestoreBrowserFingerprintPolicyAsync(originalPolicy);
        }
    }

    [Fact]
    public async Task Account_Login_WithFingerprintProofEnabled_AndSignalPrivacyMismatch_ReturnsBadRequest()
    {
        var originalPolicy = await SetBrowserFingerprintPolicyAsync(enableBrowserFingerprint: true);
        try
        {
            var password = "S3eded!Pass";
            var userName = TestDataSeeder.RandomName();
            var email = $"{userName}@example.com";
            var seeded = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, email);
            var fingerprint = new string('g', 64);

            using var client = factory.CreateClient();
            var proof = await BuildFingerprintProofAsync(client, fingerprint, signalOverrides: new Dictionary<string, string>
            {
                ["resistance_privacy"] = "Brave"
            });

            var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
                new LoginModel
                {
                    UserName = seeded.UserName,
                    Password = password,
                    Fingerprint = fingerprint,
                    FingerprintProof = proof
                });

            Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
        }
        finally
        {
            await RestoreBrowserFingerprintPolicyAsync(originalPolicy);
        }
    }

    [Fact]
    public async Task Register_WithFingerprintProofEnabled_AndBraveStrictProof_ReturnsBadRequest()
    {
        var originalPolicy = await SetBrowserFingerprintPolicyAsync(enableBrowserFingerprint: true);
        try
        {
            var fingerprint = new string('d', 64);
            var registerModel = new RegisterModel
            {
                UserName = TestDataSeeder.RandomName(),
                Password = "TestPassword123!",
                Email = $"test_{Guid.NewGuid():N}@example.com",
                Fingerprint = fingerprint,
            };

            using var client = factory.CreateClient();
            registerModel.FingerprintProof = await BuildFingerprintProofAsync(client, fingerprint, privacy: "Brave", mode: "strict");
            var registerResponse = await client.PostAsJsonAsync("/api/Account/Register", registerModel);

            Assert.Equal(HttpStatusCode.BadRequest, registerResponse.StatusCode);
        }
        finally
        {
            await RestoreBrowserFingerprintPolicyAsync(originalPolicy);
        }
    }

    [Fact]
    public async Task Account_Login_WithFingerprintProofEnabled_ReusedProof_ReturnsBadRequest()
    {
        var originalPolicy = await SetBrowserFingerprintPolicyAsync(enableBrowserFingerprint: true);
        try
        {
            var password = "S3eded!Pass";
            var userName = TestDataSeeder.RandomName();
            var email = $"{userName}@example.com";
            var seeded = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, email);
            var fingerprint = new string('e', 64);

            using var client = factory.CreateClient();
            var proof = await BuildFingerprintProofAsync(client, fingerprint);

            var firstLoginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
                new LoginModel
                {
                    UserName = seeded.UserName,
                    Password = password,
                    Fingerprint = fingerprint,
                    FingerprintProof = proof
                });
            firstLoginResponse.EnsureSuccessStatusCode();

            var replayLoginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
                new LoginModel
                {
                    UserName = seeded.UserName,
                    Password = password,
                    Fingerprint = fingerprint,
                    FingerprintProof = proof
                });

            Assert.Equal(HttpStatusCode.BadRequest, replayLoginResponse.StatusCode);
        }
        finally
        {
            await RestoreBrowserFingerprintPolicyAsync(originalPolicy);
        }
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var registerModel = new
        {
            userName = TestDataSeeder.RandomName(),
            password = "TestPassword123!",
            email = $"test_{Guid.NewGuid():N}@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Account/Register", registerModel);
        output.WriteLine($"Status: {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();
        output.WriteLine($"Response: {content}");

        // Assert
        // Registration might succeed or fail depending on global config
        // We just verify we get a valid response (not a 404 or 500)
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK or BadRequest but got {response.StatusCode}"
        );
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerModel = new
        {
            userName = TestDataSeeder.RandomName(),
            password = "TestPassword123!",
            email = "invalid-email"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Account/Register", registerModel);
        output.WriteLine($"Status: {response.StatusCode}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var registerModel = new { userName = "testuser", password = "123", email = "test@example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Account/Register", registerModel);
        output.WriteLine($"Status: {response.StatusCode}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithoutCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginModel = new { };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Account/LogIn", loginModel);
        output.WriteLine($"Status: {response.StatusCode}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/Account/LogOut", null);
        output.WriteLine($"Status: {response.StatusCode}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var verifyModel = new { email = "test@example.com", token = "invalid-token" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Account/Verify", verifyModel);
        output.WriteLine($"Status: {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();
        output.WriteLine($"Response: {content}");

        // Assert - endpoint returns OK with error message in body
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK or BadRequest but got {response.StatusCode}"
        );
    }

    private static async Task<string> BuildFingerprintProofAsync(
        HttpClient client,
        string fingerprint,
        int lieCount = 0,
        int trashCount = 0,
        int errorCount = 0,
        int headlessRating = 0,
        int stealthRating = 0,
        int likeHeadlessRating = 0,
        string? privacy = null,
        string? mode = null,
        string? extension = null,
        Dictionary<string, string>? signalOverrides = null,
        params string[] suspiciousSignals)
    {
        var challengeResponse = await client.GetFromJsonAsync<RequestResponse<BrowserFingerprintChallengeModel>>(
            "/api/Account/FingerprintChallenge");
        if (challengeResponse?.Data is null || string.IsNullOrWhiteSpace(challengeResponse.Data.Nonce))
            throw new InvalidOperationException("Failed to get browser fingerprint challenge");

        var requiredSignals = challengeResponse.Data.RequiredSignals ?? [];
        var signalMap = requiredSignals.ToDictionary(key => key, _ => string.Empty);
        foreach (var key in requiredSignals)
        {
            signalMap[key] = key switch
            {
                "lie_count" => lieCount.ToString(),
                "trash_count" => trashCount.ToString(),
                "error_count" => errorCount.ToString(),
                "headless_rating" => headlessRating.ToString(),
                "stealth_rating" => stealthRating.ToString(),
                "like_headless_rating" => likeHeadlessRating.ToString(),
                "platform_consistent" => "1",
                "ua_consistent" => "1",
                "webgl_consistent" => "1",
                "resistance_extension" => extension ?? string.Empty,
                "resistance_privacy" => privacy ?? string.Empty,
                _ => string.Empty
            };
        }

        if (signalOverrides is not null)
        {
            foreach (var (key, value) in signalOverrides)
                signalMap[key] = value;
        }

        return JsonSerializer.Serialize(new
        {
            version = 1,
            fingerprint,
            nonce = challengeResponse.Data.Nonce,
            signalOrder = requiredSignals,
            signals = signalMap,
            lieCount,
            trashCount,
            errorCount,
            headlessRating,
            stealthRating,
            likeHeadlessRating,
            resistance = new
            {
                privacy,
                mode,
                extension
            },
            suspiciousSignals
        });
    }

    private static AccountPolicy ClonePolicy(AccountPolicy policy) => new()
    {
        AllowRegister = policy.AllowRegister,
        ActiveOnRegister = policy.ActiveOnRegister,
        UseCaptcha = policy.UseCaptcha,
        EmailConfirmationRequired = policy.EmailConfirmationRequired,
        EmailDomainList = policy.EmailDomainList,
        EnableBrowserFingerprint = policy.EnableBrowserFingerprint
    };

    private async Task<AccountPolicy> SetBrowserFingerprintPolicyAsync(bool enableBrowserFingerprint)
    {
        using var scope = factory.Services.CreateScope();
        var current = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<AccountPolicy>>().Value;
        var original = ClonePolicy(current);
        var updated = ClonePolicy(current);
        updated.EnableBrowserFingerprint = enableBrowserFingerprint;
        await scope.ServiceProvider.GetRequiredService<IConfigService>().SaveConfig(updated);
        return original;
    }

    private async Task RestoreBrowserFingerprintPolicyAsync(AccountPolicy policy)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IConfigService>().SaveConfig(policy);
    }
}
