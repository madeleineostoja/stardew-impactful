using Microsoft.Xna.Framework;
using StardewValley;

namespace Impactful.Framework;

public sealed class CameraShakeRenderer
{
    private int appliedX;
    private int appliedY;

    public void Apply(ShakeController controller)
    {
        this.Remove();
        if (Game1.activeClickableMenu is not null || Game1.dialogueUp || controller.CurrentOffset == System.Numerics.Vector2.Zero)
            return;

        var offset = ShakeController.ToViewportPixels(
            controller.CurrentOffset,
            Game1.viewport.Width,
            Game1.viewport.Height,
            Game1.graphics.GraphicsDevice.Viewport.Width,
            Game1.graphics.GraphicsDevice.Viewport.Height
        );
        if (offset == (0, 0))
            return;

        Game1.viewport.X += offset.X;
        Game1.viewport.Y += offset.Y;
        this.appliedX = offset.X;
        this.appliedY = offset.Y;
    }

    public void Remove()
    {
        if (this.appliedX == 0 && this.appliedY == 0)
            return;

        Game1.viewport.X -= this.appliedX;
        Game1.viewport.Y -= this.appliedY;
        this.appliedX = 0;
        this.appliedY = 0;
    }
}
