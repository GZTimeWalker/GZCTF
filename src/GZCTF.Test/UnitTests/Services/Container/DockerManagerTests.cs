using System;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Manager;
using Xunit;

namespace GZCTF.Test.UnitTests.Services.Container;

/// <summary>
/// Tests for the Docker image pull policy and image reference normalization used
/// before a challenge container is created.
/// </summary>
public class DockerManagerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    [Fact]
    public void ShouldPullImage_MissingPolicy_NeverPulls() =>
        Assert.False(DockerManager.ShouldPullImage(ImagePullPolicy.Missing, null, Now, Interval));

    [Fact]
    public void ShouldPullImage_AlwaysPolicy_PullsEvenWhenRecentlyPulled() =>
        Assert.True(DockerManager.ShouldPullImage(ImagePullPolicy.Always, Now, Now, Interval));

    [Fact]
    public void ShouldPullImage_IfStalePolicy_PullsWhenNeverPulled() =>
        Assert.True(DockerManager.ShouldPullImage(ImagePullPolicy.IfStale, null, Now, Interval));

    [Fact]
    public void ShouldPullImage_IfStalePolicy_DoesNotPullWithinInterval()
    {
        var lastPulledAt = Now - Interval + TimeSpan.FromSeconds(1);

        Assert.False(DockerManager.ShouldPullImage(ImagePullPolicy.IfStale, lastPulledAt, Now, Interval));
    }

    [Fact]
    public void ShouldPullImage_IfStalePolicy_PullsAfterInterval()
    {
        var lastPulledAt = Now - Interval;

        Assert.True(DockerManager.ShouldPullImage(ImagePullPolicy.IfStale, lastPulledAt, Now, Interval));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShouldPullImage_IfStalePolicy_PullsWhenIntervalIsNotPositive(int intervalSeconds)
    {
        var interval = TimeSpan.FromSeconds(intervalSeconds);

        Assert.True(DockerManager.ShouldPullImage(ImagePullPolicy.IfStale, Now, Now, interval));
    }

    [Theory]
    [InlineData("nginx@sha256:0123456789abcdef")]
    [InlineData("ghcr.io/team/chal@sha256:0123456789abcdef")]
    public void IsImmutableReference_DigestReferences_ReturnsTrue(string image) =>
        Assert.True(DockerManager.IsImmutableReference(image));

    [Theory]
    [InlineData("nginx")]
    [InlineData("nginx:latest")]
    [InlineData("ghcr.io/team/chal:v1")]
    public void IsImmutableReference_TaggedReferences_ReturnsFalse(string image) =>
        Assert.False(DockerManager.IsImmutableReference(image));

    [Theory]
    [InlineData("nginx", "nginx:latest")]
    [InlineData("nginx:latest", "nginx:latest")]
    [InlineData("nginx:1.27", "nginx:1.27")]
    [InlineData("ghcr.io/team/chal", "ghcr.io/team/chal:latest")]
    [InlineData("ghcr.io/team/chal:v1", "ghcr.io/team/chal:v1")]
    [InlineData("localhost:5000/team/chal", "localhost:5000/team/chal:latest")]
    [InlineData("localhost:5000/team/chal:v1", "localhost:5000/team/chal:v1")]
    [InlineData("nginx@sha256:0123456789abcdef", "nginx@sha256:0123456789abcdef")]
    public void NormalizeImageReference_AddsDefaultTagOnlyWhenMissing(string image, string expected) =>
        Assert.Equal(expected, DockerManager.NormalizeImageReference(image));
}
