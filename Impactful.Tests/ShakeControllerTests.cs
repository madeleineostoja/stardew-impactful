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

        var offset = controller.ConsumeOffset(true, 100);

        Assert.Equal(2, offset.Length(), 3);
        Assert.True(offset.X > 0);
        Assert.NotEqual(0, offset.Y);
        Assert.Equal(Vector2.Zero, controller.ConsumeOffset(true, 100));
        Assert.False(controller.IsActive);
    }

    [Fact]
    public void InvalidImpulseIsIgnored()
    {
        var controller = new ShakeController();
        controller.AddImpulse(0, Vector2.UnitX);
        controller.AddImpulse(1, Vector2.Zero);

        Assert.Equal(Vector2.Zero, controller.ConsumeOffset(true, 100));
        Assert.False(controller.IsActive);
    }

    [Fact]
    public void SameFrameImpulsesUseRmsAndOpposingDirectionsStayFinite()
    {
        var controller = new ShakeController();
        controller.AddImpulse(3, Vector2.UnitX);
        controller.AddImpulse(4, Vector2.UnitX);
        Assert.Equal(5, controller.ConsumeOffset(true, 100).Length(), 3);

        controller.AddImpulse(4, Vector2.UnitX);
        controller.AddImpulse(4, -Vector2.UnitX);
        var offset = controller.ConsumeOffset(true, 100);
        Assert.Equal(MathF.Sqrt(32), offset.Length(), 3);
        Assert.False(float.IsNaN(offset.X));
        Assert.False(float.IsNaN(offset.Y));
    }

    [Fact]
    public void StrengthScalingIsHardCapped()
    {
        var controller = new ShakeController();
        controller.AddImpulse(ShakeController.HardMaximumPixels, Vector2.UnitY);

        var offset = controller.ConsumeOffset(true, 200);

        Assert.Equal(ShakeController.HardMaximumPixels, offset.Length(), 3);
    }

    [Fact]
    public void BurstDoesNotAllocateOrCarryStrengthIntoNextFrame()
    {
        var controller = new ShakeController();
        controller.AddImpulse(1, Vector2.UnitX);
        controller.Clear();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
            controller.AddImpulse(1, Vector2.UnitX);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Equal(ShakeController.HardMaximumPixels, controller.ConsumeOffset(true, 100).Length(), 3);

        controller.AddImpulse(1, Vector2.UnitY);
        controller.AddImpulse(1, -Vector2.UnitY);
        var next = controller.ConsumeOffset(true, 100);
        Assert.Equal(MathF.Sqrt(2), next.Length(), 3);
        Assert.True(MathF.Abs(next.Y) > MathF.Abs(next.X));
    }

    [Fact]
    public void RenderImpulseStartsAtFullStrengthThenReboundsAndExpires()
    {
        var controller = new ShakeController();
        controller.AddImpulse(10, Vector2.UnitY);

        controller.Advance(1000, true, 100);
        var initial = controller.CurrentOffset;
        Assert.Equal(10, initial.Length(), 3);
        Assert.True(controller.IsActive);

        controller.Advance(18, true, 100);
        var firstStep = controller.CurrentOffset.Length();
        Assert.InRange(firstStep, 0.1f, 9.9f);
        Assert.True(Vector2.Dot(initial, controller.CurrentOffset) > 0);

        controller.Advance(18, true, 100);
        var secondStep = controller.CurrentOffset.Length();
        Assert.True(initial.Length() - firstStep < firstStep - secondStep);

        controller.Advance(99, true, 100);
        Assert.True(Vector2.Dot(initial, controller.CurrentOffset) < 0);

        controller.Advance(60, true, 100);
        Assert.Equal(Vector2.Zero, controller.CurrentOffset);
        Assert.False(controller.IsActive);
        controller.Advance(16, true, 100);
        Assert.Equal(Vector2.Zero, controller.CurrentOffset);
    }

    [Fact]
    public void HiddenWorldPreservesPendingAndActiveKicksUntilVisible()
    {
        var controller = new ShakeController();
        controller.AddImpulse(12, Vector2.UnitY);

        controller.Advance(250, true, 100, visible: false);
        Assert.Equal(Vector2.Zero, controller.CurrentOffset);
        Assert.True(controller.IsActive);

        controller.Advance(16, true, 100);
        var initial = controller.CurrentOffset;
        Assert.Equal(12, initial.Length(), 3);

        controller.Advance(250, true, 100, visible: false);
        Assert.Equal(initial, controller.CurrentOffset);

        controller.Advance(16, true, 100);
        Assert.InRange(controller.CurrentOffset.Length(), 0.1f, 11.99f);
    }

    [Fact]
    public void OverlappingRenderImpulsesRespectTheHardCap()
    {
        var controller = new ShakeController();
        controller.AddImpulse(ShakeController.HardMaximumPixels, Vector2.UnitX);
        controller.Advance(16, true, 100);
        controller.AddImpulse(ShakeController.HardMaximumPixels, Vector2.UnitX);
        controller.Advance(16, true, 100);

        Assert.Equal(ShakeController.HardMaximumPixels, controller.CurrentOffset.Length(), 3);
    }

    [Theory]
    [InlineData(false, 100)]
    [InlineData(true, 0)]
    public void DisablingClearsActiveAndPendingShake(bool enabled, int strengthPercent)
    {
        var controller = new ShakeController();
        controller.AddImpulse(2, Vector2.UnitX);
        controller.Advance(16, true, 100);
        controller.AddImpulse(2, Vector2.UnitY);

        controller.Advance(16, enabled, strengthPercent, visible: false);
        Assert.Equal(Vector2.Zero, controller.CurrentOffset);
        Assert.False(controller.IsActive);

        controller.Advance(16, true, 100);
        Assert.Equal(Vector2.Zero, controller.CurrentOffset);
    }
}
