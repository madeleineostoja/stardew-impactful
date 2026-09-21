using System.Numerics;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Tools;

namespace Impactful.Patches;

[HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.DoDamage))]
internal static class MeleeWeaponPatches
{
    [ThreadStatic]
    private static int resolvingMelee;
    [ThreadStatic]
    private static int attackFacing;
    [ThreadStatic]
    private static bool clubAttack;

    internal static bool IsResolvingLocalMelee => resolvingMelee > 0;
    internal static int AttackFacing => attackFacing;
    internal static bool IsClubAttack => clubAttack;

    private static void Prefix(MeleeWeapon __instance, Farmer who, ref MeleeState __state)
    {
        __state = new MeleeState(resolvingMelee, attackFacing, clubAttack);
        if (!who.IsLocalPlayer)
            return;

        resolvingMelee++;
        attackFacing = who.FacingDirection;
        clubAttack = __instance.type.Value == MeleeWeapon.club;
    }

    private static void Finalizer(MeleeState __state)
    {
        resolvingMelee = __state.Depth;
        attackFacing = __state.Facing;
        clubAttack = __state.Club;
    }

    private readonly record struct MeleeState(int Depth, int Facing, bool Club);
}

[HarmonyPatch(typeof(GameLocation), nameof(GameLocation.damageMonster), new[]
{
    typeof(Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(float), typeof(float), typeof(bool), typeof(Farmer), typeof(bool)
})]
internal static class CombatPatches
{
    private static void Postfix(bool __result, bool isBomb, Farmer who, bool isProjectile)
    {
        if (!__result || isBomb || isProjectile || !who.IsLocalPlayer || !MeleeWeaponPatches.IsResolvingLocalMelee || !ModEntry.Instance.Config.Combat)
            return;

        var strength = MeleeWeaponPatches.IsClubAttack ? ImpactTuning.ClubHit : ImpactTuning.MeleeHit;
        var duration = MeleeWeaponPatches.IsClubAttack ? 90 : 60;
        ModEntry.Instance.Emit(strength, duration, ModEntry.DirectionFromFacing(MeleeWeaponPatches.AttackFacing));
    }
}
