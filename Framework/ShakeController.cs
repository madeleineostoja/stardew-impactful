using System.Numerics;

namespace Impactful.Framework;

public sealed class ShakeController
{
    public const float HardMaximumPixels = 28f;

    private readonly Random random = new();
    private float sumSquares;
    private Vector2 weightedDirection;
    private ShakeImpulse strongest;

    public bool IsActive => this.strongest.Strength > 0;

    public void AddImpulse(float strength, Vector2 direction)
    {
        var impulse = new ShakeImpulse(strength, direction);
        if (!impulse.IsValid)
            return;

        impulse = impulse with { Direction = Vector2.Normalize(direction) };
        this.sumSquares += strength * strength;
        this.weightedDirection += impulse.Direction * strength;
        if (strength > this.strongest.Strength)
            this.strongest = impulse;
    }

    public Vector2 ConsumeOffset(bool enabled, int strengthPercent)
    {
        if (!enabled || strengthPercent <= 0)
        {
            this.Clear();
            return Vector2.Zero;
        }

        if (!this.IsActive)
            return Vector2.Zero;

        var strength = MathF.Sqrt(this.sumSquares) * Math.Clamp(strengthPercent, 0, 200) / 100f;
        var direction = this.weightedDirection.LengthSquared() > 0.0001f ? Vector2.Normalize(this.weightedDirection) : this.strongest.Direction;
        this.Clear();
        var perpendicular = new Vector2(-direction.Y, direction.X);
        var lateralStrength = 0.25f + this.random.NextSingle() * 0.35f;
        if (this.random.Next(2) == 0)
            lateralStrength = -lateralStrength;
        direction = Vector2.Normalize(direction + perpendicular * lateralStrength);

        return direction * Math.Min(strength, HardMaximumPixels);
    }

    public void Clear()
    {
        this.sumSquares = 0;
        this.weightedDirection = Vector2.Zero;
        this.strongest = default;
    }
}
