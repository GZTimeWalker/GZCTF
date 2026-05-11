using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GZCTF.Test.UnitTests.Services;

public class IpAttributionHelperTest
{
    private static AppDbContext NewDb(string name) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    [Fact]
    public async Task ResolveUserIpAt_NoMatch_ReturnsNull()
    {
        await using var db = NewDb(nameof(ResolveUserIpAt_NoMatch_ReturnsNull));
        var helper = new IpAttributionHelper();

        var ip = await helper.ResolveUserIpAt(db, "alice", DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.Null(ip);
    }

    [Fact]
    public async Task ResolveUserIpAt_SingleMatchInWindow_Resolves()
    {
        await using var db = NewDb(nameof(ResolveUserIpAt_SingleMatchInWindow_Resolves));
        var now = DateTimeOffset.UtcNow;
        db.Logs.Add(new LogModel
        {
            UserName = "alice",
            RemoteIP = IPAddress.Parse("1.2.3.4"),
            TimeUtc = now,
            Level = "Information",
            Logger = "test",
            Message = "x",
        });
        await db.SaveChangesAsync();

        var helper = new IpAttributionHelper();
        var ip = await helper.ResolveUserIpAt(db, "alice", now, TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.NotNull(ip);
        Assert.Equal("1.2.3.4", ip!.ToString());
    }

    [Fact]
    public async Task ResolveUserIpAt_MultipleMatches_MostRecentWins()
    {
        await using var db = NewDb(nameof(ResolveUserIpAt_MultipleMatches_MostRecentWins));
        var now = DateTimeOffset.UtcNow;
        db.Logs.AddRange(
            new LogModel
            {
                UserName = "alice", RemoteIP = IPAddress.Parse("1.1.1.1"),
                TimeUtc = now.AddSeconds(-30), Level = "Information", Logger = "t", Message = "old",
            },
            new LogModel
            {
                UserName = "alice", RemoteIP = IPAddress.Parse("2.2.2.2"),
                TimeUtc = now.AddSeconds(-1), Level = "Information", Logger = "t", Message = "new",
            });
        await db.SaveChangesAsync();

        var helper = new IpAttributionHelper();
        var ip = await helper.ResolveUserIpAt(db, "alice", now, TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.NotNull(ip);
        Assert.Equal("2.2.2.2", ip!.ToString());
    }

    [Fact]
    public async Task ResolveUserIpAt_OutsideWindow_ReturnsNull()
    {
        await using var db = NewDb(nameof(ResolveUserIpAt_OutsideWindow_ReturnsNull));
        var now = DateTimeOffset.UtcNow;
        db.Logs.Add(new LogModel
        {
            UserName = "alice",
            RemoteIP = IPAddress.Parse("1.2.3.4"),
            TimeUtc = now.AddHours(-2),
            Level = "Information", Logger = "t", Message = "x",
        });
        await db.SaveChangesAsync();

        var helper = new IpAttributionHelper();
        var ip = await helper.ResolveUserIpAt(db, "alice", now, TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.Null(ip);
    }

    [Fact]
    public async Task ResolveUserIpAt_EmptyUserName_ReturnsNull()
    {
        await using var db = NewDb(nameof(ResolveUserIpAt_EmptyUserName_ReturnsNull));
        var helper = new IpAttributionHelper();

        var ip = await helper.ResolveUserIpAt(db, "", DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.Null(ip);
    }

    [Fact]
    public async Task ResolveUserIpAt_OnlyOtherUsers_ReturnsNull()
    {
        await using var db = NewDb(nameof(ResolveUserIpAt_OnlyOtherUsers_ReturnsNull));
        var now = DateTimeOffset.UtcNow;
        db.Logs.Add(new LogModel
        {
            UserName = "bob", RemoteIP = IPAddress.Parse("1.2.3.4"),
            TimeUtc = now, Level = "Information", Logger = "t", Message = "x",
        });
        await db.SaveChangesAsync();

        var helper = new IpAttributionHelper();
        var ip = await helper.ResolveUserIpAt(db, "alice", now, TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.Null(ip);
    }
}
