using System.Numerics;
using Impactful.Framework;
using Xunit;

namespace Impactful.Tests;

public sealed class ShakeControllerTests
{
    [Fact]
    public void ImpulseIsConsumedExactlyOnceWithLateralMovement()
    {
        var controller = new ShakeController();
        controller.AddImpulse(2, Vector2.UnitX);

        var offset = controller.ConsumeOffset(true, 100, ShakeController.ReferenceViewportHeight);

        Assert.Equal(2, offset.Length(), 3);
        Assert.True(offset.X > 0);
        Assert.NotEqual(0, offset.Y);
        Assert.Equal(Vector2.Zero, controller.ConsumeOffset(true, 100, ShakeController.ReferenceViewportHeight));
        Assert.False(controller.IsActive);
    }

    [Fact]
    public void InvalidImpulseIsIgnored()
    {
        var controller = new ShakeController();
        controller.AddImpulse(0, Vector2.UnitX);
        controller.AddImpulse(1, Vector2.Zero);

        Assert.Equal(Vector2.Zero, controller.ConsumeOffset(true, 100, ShakeController.ReferenceViewportHeight));
        Assert.False(controller.IsActive);
    }

    [Fact]
    public void SameFrameImpulsesUseRmsAndOpposingDirectionsStayFinite()
    {
        var controller = new ShakeController();
        controller.AddImpulse(3, Vector2.UnitX);
        controller.AddImpulse(4, Vector2.UnitX);
        Assert.Equal(5, controller.ConsumeOffset(true, 100, ShakeController.ReferenceViewportHeight).Length(), 3);

        controller.AddImpulse(4, Vector2.UnitX);
        controller.AddImpulse(4, -Vector2.UnitX);
        var offset = controller.ConsumeOffset(true, 100, ShakeController.ReferenceViewportHeight);
        Assert.Equal(MathF.Sqrt(32), offset.Length(), 3);
        Assert.False(float.IsNaN(offset.X));
        Assert.False(float.IsNaN(offset.Y));
    }

    [Fact]
    public void StrengthScalingIsHardCapped()
    {
        var controller = new ShakeController();
        controller.AddImpulse(ShakeController.HardMaximumStrength, Vector2.UnitY);

        var offset = controller.ConsumeOffset(true, 200, ShakeController.ReferenceViewportHeight);

        Assert.Equal(ShakeController.HardMaximumStrength, offset.Length(), 3);
    }

    [Fact]
    public void ViewportHeightScalesRenderedStrength()
    {
        var controller = new ShakeController();
        controller.AddImpulse(4, Vector2.UnitX);

        var offset = controller.ConsumeOffset(true, 100, ShakeController.ReferenceViewportHeight / 2);

        Assert.Equal(2, offset.Length(), 3);
    }

    [Fact]
    public void DisabledOrZeroStrengthClearsPendingShake()
    {
        var controller = new ShakeController();
        controller.AddImpulse(2, Vector2.UnitX);

        Assert.Equal(Vector2.Zero, controller.ConsumeOffset(true, 0, ShakeController.ReferenceViewportHeight));
        Assert.False(controller.IsActive);
    }
}
