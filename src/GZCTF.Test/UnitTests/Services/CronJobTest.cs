using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GZCTF.Models.Data;
using GZCTF.Services.CronJob;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Test.UnitTests.Services;

public class CronJobTest
{
    private readonly ITestOutputHelper _output;

    public CronJobTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task RemoveUnactivatedUsers_ShouldDeleteOldUnverifiedUsers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Prepare test data
        var now = DateTimeOffset.UtcNow;
        var oldDate = now.AddHours(-50);
        var newDate = now.AddHours(-2);

        var users = new List<UserInfo>
        {
            new() { UserName = "VerifiedNew", Email = "v_new@test.com", EmailConfirmed = true, RegisterTimeUtc = newDate },
            new() { UserName = "VerifiedOld", Email = "v_old@test.com", EmailConfirmed = true, RegisterTimeUtc = oldDate },
            new() { UserName = "UnverifiedNew", Email = "u_new@test.com", EmailConfirmed = false, RegisterTimeUtc = newDate },
            new() { UserName = "UnverifiedOld", Email = "u_old@test.com", EmailConfirmed = false, RegisterTimeUtc = oldDate }
        };

        // Register fake UserManager
        services.AddSingleton<UserManager<UserInfo>>(sp => 
            new FakeUserManager(users, sp.GetRequiredService<ILogger<UserManager<UserInfo>>>()));

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        await using var scope = scopeFactory.CreateAsyncScope();
        
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CronJobService>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserInfo>>();
        // Ensure the fake user manager is wired up correctly
        Assert.Equal(4, userManager.Users.Count());

        // Act
        await RuntimeCronJobs.RemoveUnactivatedUsers(scope, logger);

        // Assert
        var remainingUsers = userManager.Users.ToList();
        _output.WriteLine($"Remaining users: {string.Join(", ", remainingUsers.Select(u => u.UserName))}");

        Assert.Contains(remainingUsers, u => u.UserName == "VerifiedNew");
        Assert.Contains(remainingUsers, u => u.UserName == "VerifiedOld");
        Assert.Contains(remainingUsers, u => u.UserName == "UnverifiedNew");
        Assert.DoesNotContain(remainingUsers, u => u.UserName == "UnverifiedOld");
        
        Assert.Equal(3, remainingUsers.Count);
    }

    public class FakeUserManager : UserManager<UserInfo>
    {
        private readonly List<UserInfo> _users;

        public FakeUserManager(List<UserInfo> users, ILogger<UserManager<UserInfo>> logger)
            : base(new FakeUserStore(),
                  Microsoft.Extensions.Options.Options.Create(new IdentityOptions()), // options
                  null, // passwordHasher
                  null, // userValidators
                  null, // passwordValidators
                  null, // keyNormalizer
                  null, // errors
                  null, // services
                  logger)
        {
            _users = users;
        }

        public override IQueryable<UserInfo> Users => _users.AsQueryable();

        public override Task<IdentityResult> DeleteAsync(UserInfo user)
        {
            _users.Remove(user);
            return Task.FromResult(IdentityResult.Success);
        }
    }

    public class FakeUserStore : IUserStore<UserInfo>, IQueryableUserStore<UserInfo>
    {
        public IQueryable<UserInfo> Users => throw new NotImplementedException();
        public Task<IdentityResult> CreateAsync(UserInfo user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IdentityResult> DeleteAsync(UserInfo user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public void Dispose() { }
        public Task<UserInfo?> FindByIdAsync(string userId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<UserInfo?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string?> GetNormalizedUserNameAsync(UserInfo user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string> GetUserIdAsync(UserInfo user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string?> GetUserNameAsync(UserInfo user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SetNormalizedUserNameAsync(UserInfo user, string? normalizedName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SetUserNameAsync(UserInfo user, string? userName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IdentityResult> UpdateAsync(UserInfo user, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
