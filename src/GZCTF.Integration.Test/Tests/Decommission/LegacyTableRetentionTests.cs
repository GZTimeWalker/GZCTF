using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Decommission;

[Collection(nameof(IntegrationTestCollection))]
public sealed class LegacyTableRetentionTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Legacy_entity_sets_remain_available_for_migration_audit()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotNull(db.Games);
        Assert.NotNull(db.Teams);
        Assert.NotNull(db.GameChallenges);
        Assert.NotNull(db.Submissions);
        Assert.NotNull(db.Participations);
    }
}
