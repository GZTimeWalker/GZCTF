using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Request.Account;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Account workflow tests covering login with seeded data
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class AccountWorkflowTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Api_Account_Login_WithSeededUser_Succeeds()
    {
        var password = "S3eded!Pass";
        var userName = TestDataSeeder.RandomName();
        var email = $"{userName}@example.com";
        var seeded = await TestDataSeeder.CreateUserAsync(factory.Services,
            userName,
            email,
            password);

        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn", new LoginModel
        {
            UserName = seeded.UserName,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        var profileResponse = await client.GetAsync("/api/Account/Profile");
        profileResponse.EnsureSuccessStatusCode();

        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileUserInfoModel>();
        Assert.NotNull(profile);
        Assert.Equal(seeded.UserName, profile!.UserName);
        Assert.Equal(seeded.Email, profile.Email);
    }
}
