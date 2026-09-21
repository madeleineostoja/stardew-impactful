using System.Numerics;
using Impactful.Framework;
using Xunit;

namespace Impactful.Tests;

public sealed class ShakeControllerTests
{
    [Fact]
    public void ImpulseKicksRecoversAndSettles()
    {
        var controller = new ShakeController();
        controller.AddImpulse(2, 100, Vector2.UnitX);

        controller.Advance(16, true, 100);
        Assert.True(controller.CurrentOffset.X > 0);
        controller.Advance(50, true, 100);
        Assert.True(controller.CurrentOffset.X < 0);
        controller.Advance(40, true, 100);

        Assert.Equal(Vector2.Zero, controller.CurrentOffset);
        Assert.False(controller.IsActive);
    }

    [Fact]
    public void InvalidImpulseIsIgnored()
    {
        var controller = new ShakeController();
        controller.AddImpulse(0, 20, Vector2.UnitX);
        controller.AddImpulse(1, 0, Vector2.UnitX);
        controller.Advance(10, true, 100);

        Assert.False(controller.IsActive);
        Assert.Equal(Vector2.Zero, controller.CurrentOffset);
    }

    [Fact]
    public void SameFrameImpulsesUseRmsAndOpposingDirectionsUseTheStrongestDirection()
    {
        var controller = new ShakeController();
        controller.AddImpulse(3, 100, Vector2.UnitX);
        controller.AddImpulse(4, 100, Vector2.UnitX);
        controller.Advance(16, true, 100);
        Assert.Equal(5, controller.CurrentOffset.X, 3);

        controller.Clear();
        controller.AddImpulse(4, 100, Vector2.UnitX);
        controller.AddImpulse(4, 100, -Vector2.UnitX);
        controller.Advance(16, true, 100);
        Assert.Equal(MathF.Sqrt(32), controller.CurrentOffset.X, 3);
        Assert.False(float.IsNaN(controller.CurrentOffset.X));
    }

    [Fact]
    public void OverlappingImpulsesAreHardCappedAndExpire()
    {
        var controller = new ShakeController();
        controller.AddImpulse(6, 100, Vector2.UnitY);
        controller.Advance(16, true, 200);
        controller.AddImpulse(6, 100, Vector2.UnitY);
        controller.Advance(1, true, 200);

        Assert.Equal(ShakeController.HardMaximumPixels, controller.CurrentOffset.Length(), 3);
        controller.Advance(200, true, 200);
        Assert.False(controller.IsActive);
    }

    [Fact]
    public void MiningImpulseProducesAnIntegerViewportOffsetAtNormalFrameTime()
    {
        var controller = new ShakeController();
        controller.AddImpulse(ImpactTuning.MiningHit, 55, Vector2.UnitY);
        controller.Advance(16.67f, true, 100);

        var offset = ShakeController.ToViewportPixels(controller.CurrentOffset, 1280, 720, 1280, 720);
        Assert.NotEqual(0, offset.Y);
    }

    [Fact]
    public void ViewportConversionRoundsAndUsesIndependentAxes()
    {
        Assert.Equal((2, -2), ShakeController.ToViewportPixels(new Vector2(1.5f, -1.5f), 1280, 720, 960, 480));
        Assert.Equal((0, 0), ShakeController.ToViewportPixels(Vector2.One, 0, 720, 960, 480));
    }

    [Fact]
    public void DisabledOrZeroStrengthClearsAllShake()
    {
        var controller = new ShakeController();
        controller.AddImpulse(2, 100, Vector2.UnitX);
        controller.Advance(16, true, 0);

        Assert.False(controller.IsActive);
        Assert.Equal(Vector2.Zero, controller.CurrentOffset);
    }
}
