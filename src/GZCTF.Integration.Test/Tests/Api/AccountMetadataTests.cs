using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Tests for account metadata update flow
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class AccountMetadataTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Update_Persists_Metadata_To_Profile()
    {
        const string password = "P@ssw0rd!";
        var userName = TestDataSeeder.RandomName();
        var email = $"{userName}@example.com";
        var seededUser = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password, email);

        var metadataKey = $"meta_{Guid.NewGuid():N}";
        var metadataValue = "2024-XYZ";

        using (var scope = factory.Services.CreateScope())
        {
            var metadataRepo = scope.ServiceProvider.GetRequiredService<IUserMetadataFieldRepository>();
            await metadataRepo.CreateAsync(new UserMetadataField
            {
                Key = metadataKey,
                DisplayName = "Metadata Field",
                Type = UserMetadataFieldType.Text,
                Required = true,
                Order = 1
            });
        }

        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = seededUser.UserName, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        var updateResponse = await client.PutAsJsonAsync("/api/Account/Update",
            new ProfileUpdateModel
            {
                Metadata = new SortedDictionary<string, JsonDocument?>
                {
                    [metadataKey] = JsonSerializer.SerializeToDocument(metadataValue)
                }
            });
        updateResponse.EnsureSuccessStatusCode();

        var profileResponse = await client.GetAsync("/api/Account/Profile");
        profileResponse.EnsureSuccessStatusCode();

        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileUserInfoModel>();
        Assert.NotNull(profile?.Metadata);
        Assert.True(profile.Metadata!.TryGetValue(metadataKey, out var value));
        Assert.Equal(metadataValue, value?.RootElement.GetString());
    }
}
