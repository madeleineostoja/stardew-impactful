using System.Numerics;

namespace Impactful.Framework;

public readonly record struct ShakeImpulse(float Strength, Vector2 Direction)
{
    public bool IsValid => Strength > 0 && Direction.LengthSquared() > 0;
}
