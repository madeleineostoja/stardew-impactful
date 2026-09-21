using Impactful.Framework;
using Xunit;

namespace Impactful.Tests;

public sealed class HitStopControllerTests
{
    [Fact]
    public void RequestedFramesAreConsumedExactlyOnce()
    {
        var controller = new HitStopController();
        controller.Request(2);

        Assert.True(controller.TryConsumeFrame());
        Assert.True(controller.TryConsumeFrame());
        Assert.False(controller.TryConsumeFrame());
        Assert.False(controller.IsActive);
    }

    [Fact]
    public void OverlappingRequestsUseLongestRemainingStop()
    {
        var controller = new HitStopController();
        controller.Request(2);
        controller.TryConsumeFrame();
        controller.Request(2);

        Assert.True(controller.TryConsumeFrame());
        Assert.True(controller.TryConsumeFrame());
        Assert.False(controller.TryConsumeFrame());
    }

    [Fact]
    public void ClearCancelsPendingStop()
    {
        var controller = new HitStopController();
        controller.Request(2);

        controller.Clear();

        Assert.False(controller.TryConsumeFrame());
        Assert.False(controller.IsActive);
    }
}
