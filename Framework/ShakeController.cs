using System.Numerics;

namespace Impactful.Framework;

public sealed class ShakeController
{
    public const float HardMaximumPixels = 6f;

    private readonly List<ShakeImpulse> pending = new(4);
    private readonly List<ActiveImpulse> active = new(4);

    public Vector2 CurrentOffset { get; private set; }
    public bool IsActive => this.pending.Count > 0 || this.active.Count > 0;

    public void AddImpulse(float strength, int durationMilliseconds, Vector2 direction)
    {
        var impulse = new ShakeImpulse(strength, durationMilliseconds, direction);
        if (!impulse.IsValid)
            return;

        this.pending.Add(impulse with { Direction = Vector2.Normalize(direction) });
    }

    public void Advance(float elapsedMilliseconds, bool enabled, int strengthPercent)
    {
        if (!enabled || strengthPercent <= 0)
        {
            this.Clear();
            return;
        }

        var activeCountBeforePending = this.active.Count;
        this.StartPendingImpulse();
        var result = Vector2.Zero;
        for (var index = this.active.Count - 1; index >= 0; index--)
        {
            var impulse = this.active[index];
            if (index < activeCountBeforePending)
                impulse.ElapsedMilliseconds += Math.Max(0, elapsedMilliseconds);
            if (impulse.ElapsedMilliseconds >= impulse.DurationMilliseconds)
            {
                this.active.RemoveAt(index);
                continue;
            }

            this.active[index] = impulse;
            result += impulse.Direction * (impulse.Strength * GetEnvelope(impulse.ElapsedMilliseconds / impulse.DurationMilliseconds));
        }

        result *= Math.Clamp(strengthPercent, 0, 200) / 100f;
        this.CurrentOffset = ClampMagnitude(result, HardMaximumPixels);
    }

    public void Clear()
    {
        this.pending.Clear();
        this.active.Clear();
        this.CurrentOffset = Vector2.Zero;
    }

    public static (int X, int Y) ToViewportPixels(Vector2 screenOffset, int viewportWidth, int viewportHeight, int outputWidth, int outputHeight)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0 || outputWidth <= 0 || outputHeight <= 0)
            return (0, 0);

        return (
            (int)MathF.Round(screenOffset.X * viewportWidth / outputWidth, MidpointRounding.AwayFromZero),
            (int)MathF.Round(screenOffset.Y * viewportHeight / outputHeight, MidpointRounding.AwayFromZero)
        );
    }

    private void StartPendingImpulse()
    {
        if (this.pending.Count == 0)
            return;

        var sumSquares = 0f;
        var weightedDirection = Vector2.Zero;
        var strongest = this.pending[0];
        var duration = 0;
        foreach (var impulse in this.pending)
        {
            sumSquares += impulse.Strength * impulse.Strength;
            weightedDirection += impulse.Direction * impulse.Strength;
            duration = Math.Max(duration, impulse.DurationMilliseconds);
            if (impulse.Strength > strongest.Strength)
                strongest = impulse;
        }

        var direction = weightedDirection.LengthSquared() > 0.0001f ? Vector2.Normalize(weightedDirection) : strongest.Direction;
        this.active.Add(new ActiveImpulse(MathF.Sqrt(sumSquares), duration, direction)
        {
            // A short impulse needs a full-strength first rendered frame; otherwise
            // sub-pixel mining feedback can be rounded away before it is visible.
            ElapsedMilliseconds = duration * 0.16f
        });
        this.pending.Clear();
    }

    private static float GetEnvelope(float progress)
    {
        if (progress < 0.16f)
            return progress / 0.16f;
        if (progress < 0.58f)
            return 1f - ((progress - 0.16f) / 0.42f);
        if (progress < 0.76f)
            return -0.2f * ((progress - 0.58f) / 0.18f);
        return -0.2f * (1f - ((progress - 0.76f) / 0.24f));
    }

    private static Vector2 ClampMagnitude(Vector2 value, float maximum)
    {
        var lengthSquared = value.LengthSquared();
        return lengthSquared > maximum * maximum ? Vector2.Normalize(value) * maximum : value;
    }

    private struct ActiveImpulse
    {
        public ActiveImpulse(float strength, float durationMilliseconds, Vector2 direction)
        {
            this.Strength = strength;
            this.DurationMilliseconds = durationMilliseconds;
            this.Direction = direction;
            this.ElapsedMilliseconds = 0;
        }

        public float Strength { get; }
        public float DurationMilliseconds { get; }
        public Vector2 Direction { get; }
        public float ElapsedMilliseconds { get; set; }
    }
}
