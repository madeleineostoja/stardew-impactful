using System.Numerics;
using HarmonyLib;
using StardewValley;
using StardewValley.Monsters;

namespace Impactful.Patches;

[HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
internal static class PlayerDamagePatches
{
    private static readonly Random DirectionRandom = new();

    private readonly record struct DamageState(int Health, bool HadDailyRevive, Vector2 Direction);

    private static void Prefix(Farmer __instance, Monster? damager, ref DamageState __state)
    {
        var direction = ModEntry.DirectionFromFacing(__instance.FacingDirection);
        if (damager is not null)
        {
            direction = new Vector2(__instance.Position.X - damager.Position.X, __instance.Position.Y - damager.Position.Y);
            if (direction.LengthSquared() > 0)
                direction = Vector2.Normalize(direction);
        }
        else
        {
            // This is deliberately independent of the game's RNG.
            var angle = (float)(DirectionRandom.NextDouble() * MathF.Tau);
            direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }

        __state = new DamageState(__instance.health, __instance.hasUsedDailyRevive.Value, direction);
    }

    private static void Postfix(Farmer __instance, DamageState __state)
    {
        var lostHealth = __instance.health < __state.Health;
        var revivedAfterDamage = !__state.HadDailyRevive && __instance.hasUsedDailyRevive.Value;
        if (!__instance.IsLocalPlayer || !ModEntry.Instance.Config.PlayerDamage || (!lostHealth && !revivedAfterDamage))
            return;

        ModEntry.Instance.Emit(ImpactTuning.PlayerDamage, 105, __state.Direction);
    }
}
