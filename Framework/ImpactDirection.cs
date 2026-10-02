using System.Numerics;

namespace Impactful.Framework;

internal static class ImpactDirection
{
    internal static Vector2 WithFallback(Vector2 direction, Vector2 fallback)
    {
        return direction.LengthSquared() > 0 ? direction : fallback;
    }
}
