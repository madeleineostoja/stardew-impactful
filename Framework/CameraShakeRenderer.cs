using System.Numerics;
using StardewValley;

namespace Impactful.Framework;

internal sealed class CameraShakeRenderer
{
    private int appliedX;
    private int appliedY;

    internal void Apply(Vector2 offset)
    {
        this.Remove();
        this.appliedX = (int)MathF.Round(offset.X, MidpointRounding.AwayFromZero);
        this.appliedY = (int)MathF.Round(offset.Y, MidpointRounding.AwayFromZero);
        Game1.viewport.X += this.appliedX;
        Game1.viewport.Y += this.appliedY;
    }

    internal void Remove()
    {
        Game1.viewport.X -= this.appliedX;
        Game1.viewport.Y -= this.appliedY;
        this.appliedX = 0;
        this.appliedY = 0;
    }
}
