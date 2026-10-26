using GZCTF.Services.Container.Manager;
using Xunit;

namespace GZCTF.Test.UnitTests.Services.Container;

public class ImagePullTests
{
    [Fact]
    public void LastWaiterLeavingCancelsSharedPull()
    {
        var pull = new DockerManager.ImagePull();

        Assert.True(pull.TryAddWaiter());
        Assert.True(pull.TryAddWaiter());

        pull.RemoveWaiter();
        Assert.False(pull.Cancellation.IsCancellationRequested);

        pull.RemoveWaiter();
        Assert.True(pull.Cancellation.IsCancellationRequested);
    }

    [Fact]
    public void ClosedPullRejectsNewWaiters()
    {
        var pull = new DockerManager.ImagePull();

        Assert.True(pull.TryAddWaiter());
        pull.RemoveWaiter();

        Assert.False(pull.TryAddWaiter());
    }
}
