using System.Numerics;
using HarmonyLib;
using Impactful.Framework;
using StardewValley;
using StardewValley.Monsters;

namespace Impactful.Patches;

[HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
internal static class PlayerDamagePatches
{
    private static readonly Random DirectionRandom = new();

    private readonly record struct DamageState(int Health, bool HadDailyRevive, Vector2? Direction);

    private static void Prefix(Farmer __instance, Monster? damager, ref DamageState? __state)
    {
        if (!ModEntry.Instance.CanShake || !ModEntry.Instance.Config.PlayerDamage || !__instance.IsLocalPlayer)
            return;

        Vector2? direction = damager is null ? null : ImpactDirection.WithFallback(
            new Vector2(__instance.Position.X - damager.Position.X, __instance.Position.Y - damager.Position.Y),
            ModEntry.DirectionFromFacing(__instance.FacingDirection));
        __state = new DamageState(__instance.health, __instance.hasUsedDailyRevive.Value, direction);
    }

    private static void Postfix(Farmer __instance, DamageState? __state)
    {
        if (__state is not { } state || !ModEntry.Instance.Config.PlayerDamage)
            return;

        var lostHealth = __instance.health < state.Health;
        var revivedAfterDamage = !state.HadDailyRevive && __instance.hasUsedDailyRevive.Value;
        if (!lostHealth && !revivedAfterDamage)
            return;

        var direction = state.Direction;
        if (direction is null)
        {
            // This is deliberately independent of the game's RNG.
            var angle = (float)(DirectionRandom.NextDouble() * MathF.Tau);
            direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }
        ModEntry.Instance.Emit(ImpactTuning.PlayerDamage, direction.Value);
    }
}

[HarmonyPatch(typeof(Monster), nameof(Monster.parried))]
internal static class ParryPatches
{
    private static void Postfix(Monster __instance, Farmer who)
    {
        if (!who.IsLocalPlayer)
            return;

        var mod = ModEntry.Instance;
        if (mod.CanShake && mod.Config.Combat)
        {
            var direction = ImpactDirection.WithFallback(
                new Vector2(who.Position.X - __instance.Position.X, who.Position.Y - __instance.Position.Y),
                ModEntry.DirectionFromFacing(who.FacingDirection));
            mod.Emit(ImpactTuning.Parry, direction);
        }
        mod.RequestHitStop(4);
    }
}
