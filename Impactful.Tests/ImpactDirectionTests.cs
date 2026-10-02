using System.Numerics;
using Impactful.Framework;
using Xunit;

namespace Impactful.Tests;

public sealed class ImpactDirectionTests
{
    [Fact]
    public void CoincidentPositionsStillProduceDamageFeedback()
    {
        var controller = new ShakeController();
        var direction = ImpactDirection.WithFallback(Vector2.Zero, -Vector2.UnitY);
        controller.AddImpulse(12, direction);

        var offset = controller.ConsumeOffset(true, 100);

        Assert.Equal(12, offset.Length(), 3);
        Assert.True(offset.Y < 0);
    }
}
