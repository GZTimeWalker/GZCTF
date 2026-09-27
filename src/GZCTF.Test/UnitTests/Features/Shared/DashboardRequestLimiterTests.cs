using System;
using GZCTF.Features.Dashboard.Application;
using Xunit;

namespace GZCTF.Test.UnitTests.Features.Shared;

public sealed class DashboardRequestLimiterTests
{
    [Fact]
    public void Expired_keys_are_removed_during_periodic_cleanup()
    {
        var limiter = new DashboardRequestLimiter(
            limit: 2,
            window: TimeSpan.FromMinutes(1),
            cleanupInterval: 1);
        var start = new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero);

        Assert.True(limiter.Allow("expired", start));
        Assert.True(limiter.Allow("current", start.AddMinutes(2)));

        Assert.Equal(1, limiter.EntryCount);
    }

    [Fact]
    public void Active_key_is_limited_within_the_window()
    {
        var limiter = new DashboardRequestLimiter(
            limit: 2,
            window: TimeSpan.FromMinutes(1),
            cleanupInterval: 128);
        var start = new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero);

        Assert.True(limiter.Allow("member", start));
        Assert.True(limiter.Allow("member", start.AddSeconds(1)));
        Assert.False(limiter.Allow("member", start.AddSeconds(2)));
        Assert.True(limiter.Allow("member", start.AddMinutes(2)));
    }
}
