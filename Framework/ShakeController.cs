using System.Numerics;

namespace Impactful.Framework;

public sealed class ShakeController
{
    public const float HardMaximumPixels = 28f;
    private const float DurationMilliseconds = 180f;

    private readonly Random random = new();
    private readonly List<ActiveImpulse> active = new(4);
    private float sumSquares;
    private Vector2 weightedDirection;
    private ShakeImpulse strongest;

    public Vector2 CurrentOffset { get; private set; }
    public bool IsActive => this.strongest.Strength > 0 || this.active.Count > 0;

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

        if (this.strongest.Strength <= 0)
            return Vector2.Zero;

        var strength = MathF.Sqrt(this.sumSquares) * Math.Clamp(strengthPercent, 0, 200) / 100f;
        var direction = this.weightedDirection.LengthSquared() > 0.0001f ? Vector2.Normalize(this.weightedDirection) : this.strongest.Direction;
        this.ClearPending();
        var perpendicular = new Vector2(-direction.Y, direction.X);
        var lateralStrength = 0.25f + this.random.NextSingle() * 0.35f;
        if (this.random.Next(2) == 0)
            lateralStrength = -lateralStrength;
        direction = Vector2.Normalize(direction + perpendicular * lateralStrength);

        return direction * Math.Min(strength, HardMaximumPixels);
    }

    public void Advance(float elapsedMilliseconds, bool enabled, int strengthPercent, bool visible = true)
    {
        // Preserve the kick while a native screen flash obscures the world,
        // but still clear it when effects are disabled.
        if (!visible && enabled && strengthPercent > 0)
            return;

        var activeCount = this.active.Count;
        var pendingOffset = this.ConsumeOffset(enabled, strengthPercent);
        if (pendingOffset != Vector2.Zero)
            this.active.Add(new ActiveImpulse(pendingOffset, 0));

        var offset = Vector2.Zero;
        for (var index = this.active.Count - 1; index >= 0; index--)
        {
            var impulse = this.active[index];
            // Give a new kick its full first frame even after a slow update.
            if (index < activeCount)
                impulse = impulse with { ElapsedMilliseconds = impulse.ElapsedMilliseconds + Math.Max(0, elapsedMilliseconds) };
            if (impulse.ElapsedMilliseconds >= DurationMilliseconds)
            {
                this.active.RemoveAt(index);
                continue;
            }

            this.active[index] = impulse;
            offset += impulse.Offset * GetEnvelope(impulse.ElapsedMilliseconds / DurationMilliseconds);
        }

        this.CurrentOffset = offset.LengthSquared() > HardMaximumPixels * HardMaximumPixels
            ? Vector2.Normalize(offset) * HardMaximumPixels
            : offset;
    }

    public void Clear()
    {
        this.ClearPending();
        this.active.Clear();
        this.CurrentOffset = Vector2.Zero;
    }

    private void ClearPending()
    {
        this.sumSquares = 0;
        this.weightedDirection = Vector2.Zero;
        this.strongest = default;
    }

    private static float GetEnvelope(float progress)
    {
        if (progress < 0.6f)
            return 1f - SmoothStep(progress / 0.6f);
        if (progress < 0.8f)
            return -0.15f * SmoothStep((progress - 0.6f) / 0.2f);
        return -0.15f * SmoothStep((1f - progress) / 0.2f);
    }

    private static float SmoothStep(float progress) => progress * progress * (3f - 2f * progress);

    private readonly record struct ActiveImpulse(Vector2 Offset, float ElapsedMilliseconds);
}
