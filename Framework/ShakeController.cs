using System.Numerics;

namespace Impactful.Framework;

public sealed class ShakeController
{
    public const float HardMaximumPixels = 28f;

    private readonly List<ShakeImpulse> pending = new(4);
    private readonly Random random = new();

    public bool IsActive => this.pending.Count > 0;

    public void AddImpulse(float strength, Vector2 direction)
    {
        var impulse = new ShakeImpulse(strength, direction);
        if (!impulse.IsValid)
            return;

        this.pending.Add(impulse with { Direction = Vector2.Normalize(direction) });
    }

    public Vector2 ConsumeOffset(bool enabled, int strengthPercent)
    {
        if (!enabled || strengthPercent <= 0)
        {
            this.Clear();
            return Vector2.Zero;
        }

        if (this.pending.Count == 0)
            return Vector2.Zero;

        var sumSquares = 0f;
        var weightedDirection = Vector2.Zero;
        var strongest = this.pending[0];
        foreach (var impulse in this.pending)
        {
            sumSquares += impulse.Strength * impulse.Strength;
            weightedDirection += impulse.Direction * impulse.Strength;
            if (impulse.Strength > strongest.Strength)
                strongest = impulse;
        }
        this.pending.Clear();

        var direction = weightedDirection.LengthSquared() > 0.0001f ? Vector2.Normalize(weightedDirection) : strongest.Direction;
        var perpendicular = new Vector2(-direction.Y, direction.X);
        var lateralStrength = 0.25f + this.random.NextSingle() * 0.35f;
        if (this.random.Next(2) == 0)
            lateralStrength = -lateralStrength;
        direction = Vector2.Normalize(direction + perpendicular * lateralStrength);

        var strength = MathF.Sqrt(sumSquares) * Math.Clamp(strengthPercent, 0, 200) / 100f;
        return direction * Math.Min(strength, HardMaximumPixels);
    }

    public void Clear()
    {
        this.pending.Clear();
    }
}
