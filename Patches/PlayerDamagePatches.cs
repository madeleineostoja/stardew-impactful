using System.Numerics;
using HarmonyLib;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;

namespace Impactful.Patches;

[HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
internal static class PlayerDamagePatches
{
    private static readonly Random DirectionRandom = new();

    private readonly record struct DamageState(int Health, bool HadDailyRevive, bool WasParry, Vector2 Direction);

    private static void Prefix(Farmer __instance, bool overrideParry, Monster? damager, ref DamageState __state)
    {
        var wasParry = damager is not null
            && !damager.isInvincible()
            && !overrideParry
            && __instance.CurrentTool is MeleeWeapon { isOnSpecial: true } weapon
            && weapon.type.Value == MeleeWeapon.defenseSword;
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

        __state = new DamageState(__instance.health, __instance.hasUsedDailyRevive.Value, wasParry, direction);
    }

    private static void Postfix(Farmer __instance, DamageState __state)
    {
        if (!__instance.IsLocalPlayer)
            return;

        if (__state.WasParry)
        {
            if (ModEntry.Instance.Config.Combat)
                ModEntry.Instance.Emit(ImpactTuning.Parry, __state.Direction);
            ModEntry.Instance.RequestHitStop(4);
            return;
        }

        var lostHealth = __instance.health < __state.Health;
        var revivedAfterDamage = !__state.HadDailyRevive && __instance.hasUsedDailyRevive.Value;
        if (ModEntry.Instance.Config.PlayerDamage && (lostHealth || revivedAfterDamage))
            ModEntry.Instance.Emit(ImpactTuning.PlayerDamage, __state.Direction);
    }
}
