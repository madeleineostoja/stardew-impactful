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
    public void MissesAndImmuneTargetsDoNotRequestStop()
    {
        var controller = new HitStopController();
        controller.RequestDamage(-1, false);
        controller.RequestDamage(0, true);

        Assert.False(controller.TryConsumeFrame());
        Assert.False(controller.IsActive);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public void SuccessfulDamageUsesWeaponStopDuration(bool isClubAttack, int expectedFrames)
    {
        var controller = new HitStopController();
        controller.RequestDamage(10, isClubAttack);

        for (var i = 0; i < expectedFrames; i++)
            Assert.True(controller.TryConsumeFrame());
        Assert.False(controller.TryConsumeFrame());
    }

    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 4)]
    public void KillDurationSurvivesLaterNonlethalHits(bool isClubAttack, int expectedFrames)
    {
        var controller = new HitStopController();
        controller.RequestDamage(10, isClubAttack);
        controller.RequestKill(isClubAttack);
        Assert.True(controller.TryConsumeFrame());
        controller.RequestDamage(10, false);

        for (var i = 1; i < expectedFrames; i++)
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
