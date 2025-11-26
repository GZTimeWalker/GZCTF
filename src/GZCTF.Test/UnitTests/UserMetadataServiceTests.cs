using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GZCTF.Models.Data;
using GZCTF.Repositories.Interface;
using GZCTF.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;
using InternalUserMetadataField = GZCTF.Models.Internal.UserMetadataField;
using OAuthProviderConfig = GZCTF.Models.Internal.OAuthProviderConfig;

namespace GZCTF.Test.UnitTests;

public class UserMetadataServiceTests
{
    [Fact]
    public async Task ValidateAsync_AllowsUnlockedUpdates()
    {
        var service = CreateService([
            new InternalUserMetadataField { Key = "department", DisplayName = "Department", Required = true }
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
            new InternalUserMetadataField
            {
                Key = "studentId", DisplayName = "Student Id", Required = true, Locked = true
            }
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
            new InternalUserMetadataField { Key = "role", DisplayName = "Role", Required = true }
        ]);

        var result = await service.ValidateAsync(
            new Dictionary<string, string?>(),
            null,
            allowLockedWrites: false,
            enforceLockedRequirements: true);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    static IUserMetadataService CreateService(IReadOnlyList<InternalUserMetadataField> fields)
        => new UserMetadataService(new TestOAuthProviderRepository(fields));

    sealed class TestOAuthProviderRepository(IReadOnlyList<InternalUserMetadataField> fields) : IOAuthProviderRepository
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default)
            => throw new NotSupportedException();

        public void Add(object item) => throw new NotSupportedException();

        public Task<int> CountAsync(CancellationToken token = default)
            => Task.FromResult(0);

        public Task SaveAsync(CancellationToken token = default)
            => Task.CompletedTask;

        public Task<OAuthProvider?> FindByKeyAsync(string key, CancellationToken token = default)
            => Task.FromResult<OAuthProvider?>(null);

        public Task<List<OAuthProvider>> ListAsync(CancellationToken token = default)
            => Task.FromResult(new List<OAuthProvider>());

        public Task<Dictionary<string, OAuthProviderConfig>> GetConfigMapAsync(CancellationToken token = default)
            => Task.FromResult(new Dictionary<string, OAuthProviderConfig>());

        public Task<OAuthProviderConfig?> GetConfigAsync(string key, CancellationToken token = default)
            => Task.FromResult<OAuthProviderConfig?>(null);

        public Task UpsertAsync(string key, OAuthProviderConfig config, CancellationToken token = default)
            => Task.CompletedTask;

        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.CompletedTask;

        public Task<List<InternalUserMetadataField>> GetMetadataFieldsAsync(CancellationToken token = default)
            => Task.FromResult(fields.ToList());

        public Task UpdateMetadataFieldsAsync(List<InternalUserMetadataField> fields, CancellationToken token = default)
            => Task.CompletedTask;
    }
}
