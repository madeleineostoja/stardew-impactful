using Xunit;

namespace Impactful.Tests;

public sealed class ImpactTuningTests
{
    [Theory]
    [InlineData(3, ImpactTuning.CherryBomb)]
    [InlineData(5, ImpactTuning.Bomb)]
    [InlineData(6, ImpactTuning.MegaBomb)]
    public void ExplosionAtCenterUsesPeakStrengthForBombSize(int radius, float expected)
    {
        Assert.Equal(expected, ImpactTuning.GetExplosionStrength(radius, 0));
    }

    [Fact]
    public void BombRemainsAtFullStrengthJustOutsideBlastRadius()
    {
        Assert.Equal(ImpactTuning.Bomb, ImpactTuning.GetExplosionStrength(5, 6));
    }

    [Fact]
    public void BombFallsOffOutsideItsBlastRadius()
    {
        Assert.Equal(ImpactTuning.Bomb / 2f, ImpactTuning.GetExplosionStrength(5, 8.5f), 3);
    }

    [Fact]
    public void ExplosionStopsBeyondItsFeedbackRange()
    {
        Assert.Equal(0, ImpactTuning.GetExplosionStrength(5, 11));
    }
}
