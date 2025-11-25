using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GZCTF.Extensions.Startup;
using GZCTF.Models.Internal;
using GZCTF.Services;
using Xunit;

namespace GZCTF.Test.UnitTests;

public class UserMetadataServiceTests
{
    [Fact]
    public async Task ValidateAsync_AllowsUnlockedUpdates()
    {
        var service = CreateService([
            new UserMetadataField { Key = "department", DisplayName = "Department", Required = true }
        ]);

        var result = await service.ValidateAsync(
            new Dictionary<string, string?> { { "department", "IT" } },
            null,
            allowLockedWrites: false,
            enforceLockedRequirements: true);

        Assert.True(result.IsValid);
        Assert.Equal("IT", result.Values["department"]);
    }

    [Fact]
    public async Task ValidateAsync_IgnoresLockedWhenNotPermitted()
    {
        var service = CreateService([
            new UserMetadataField { Key = "studentId", DisplayName = "Student Id", Required = true, Locked = true }
        ]);

        var result = await service.ValidateAsync(
            new Dictionary<string, string?> { { "studentId", "123" } },
            null,
            allowLockedWrites: false,
            enforceLockedRequirements: false);

        Assert.True(result.IsValid);
        Assert.False(result.Values.ContainsKey("studentId"));
    }

    [Fact]
    public async Task ValidateAsync_FailsWhenRequiredMissing()
    {
        var service = CreateService([
            new UserMetadataField { Key = "role", DisplayName = "Role", Required = true }
        ]);

        var result = await service.ValidateAsync(
            new Dictionary<string, string?>(),
            null,
            allowLockedWrites: false,
            enforceLockedRequirements: true);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    static IUserMetadataService CreateService(IReadOnlyList<UserMetadataField> fields)
        => new UserMetadataService(new TestOAuthProviderManager(fields));

    sealed class TestOAuthProviderManager(IReadOnlyList<UserMetadataField> fields) : IOAuthProviderManager
    {
        public Task<List<UserMetadataField>> GetUserMetadataFieldsAsync(CancellationToken token = default)
            => Task.FromResult(fields.ToList());

        public Task UpdateUserMetadataFieldsAsync(List<UserMetadataField> fields, CancellationToken token = default)
            => Task.CompletedTask;

        public Task<Dictionary<string, OAuthProviderConfig>> GetOAuthProvidersAsync(CancellationToken token = default)
            => Task.FromResult(new Dictionary<string, OAuthProviderConfig>());

        public Task<OAuthProviderConfig?> GetOAuthProviderAsync(string key, CancellationToken token = default)
            => Task.FromResult<OAuthProviderConfig?>(null);

        public Task UpdateOAuthProviderAsync(string key, OAuthProviderConfig config, CancellationToken token = default)
            => Task.CompletedTask;

        public Task DeleteOAuthProviderAsync(string key, CancellationToken token = default)
            => Task.CompletedTask;

        public Task<Dictionary<string, Microsoft.AspNetCore.Authentication.AuthenticationScheme>> GetAvailableProvidersAsync(CancellationToken token = default)
            => Task.FromResult(new Dictionary<string, Microsoft.AspNetCore.Authentication.AuthenticationScheme>());
    }
}
