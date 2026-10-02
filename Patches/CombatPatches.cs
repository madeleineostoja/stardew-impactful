using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;

namespace Impactful.Patches;

[HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.DoDamage))]
internal static class MeleeWeaponPatches
{
    [ThreadStatic]
    private static int resolvingMelee;
    [ThreadStatic]
    private static bool clubAttack;

    internal static bool IsResolvingLocalMelee => resolvingMelee > 0;
    internal static bool IsClubAttack => clubAttack;

    private static void Prefix(MeleeWeapon __instance, Farmer who, ref MeleeState __state)
    {
        __state = new MeleeState(resolvingMelee, clubAttack);
        if (!ModEntry.Instance.CanHitStop || !who.IsLocalPlayer)
            return;

        resolvingMelee++;
        clubAttack = __instance.type.Value == MeleeWeapon.club;
    }

    private static void Finalizer(MeleeState __state)
    {
        resolvingMelee = __state.Depth;
        clubAttack = __state.Club;
    }

    private readonly record struct MeleeState(int Depth, bool Club);
}

[HarmonyPatch(typeof(GameLocation), nameof(GameLocation.damageMonster), new[]
{
    typeof(Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(float), typeof(float), typeof(bool), typeof(Farmer), typeof(bool)
})]
internal static class CombatPatches
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var takeDamage = AccessTools.Method(typeof(Monster), nameof(Monster.takeDamage), new[]
        {
            typeof(int), typeof(int), typeof(int), typeof(bool), typeof(double), typeof(Farmer)
        });
        var killed = AccessTools.Method(typeof(GameLocation), "onMonsterKilled");

        // damageMonster's return value includes misses and immune targets.
        // Observe actual damage and the native kill callback without scanning
        // the location. Arguments 4, 10 and 11 are isBomb, who and isProjectile.
        return new CodeMatcher(instructions)
            .MatchEndForward(new CodeMatch(instruction => instruction.Calls(takeDamage)))
            .ThrowIfInvalid("Impactful couldn't find the monster damage call.")
            .Advance(1)
            .Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldarg_S, (byte)4),
                new CodeInstruction(OpCodes.Ldarg_S, (byte)10),
                new CodeInstruction(OpCodes.Ldarg_S, (byte)11),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CombatPatches), nameof(RecordDamage))))
            .Start()
            .MatchEndForward(new CodeMatch(instruction => instruction.Calls(killed)))
            .ThrowIfInvalid("Impactful couldn't find the monster kill call.")
            .Advance(1)
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_S, (byte)4),
                new CodeInstruction(OpCodes.Ldarg_S, (byte)10),
                new CodeInstruction(OpCodes.Ldarg_S, (byte)11),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CombatPatches), nameof(RecordKill))))
            .InstructionEnumeration();
    }

    private static void RecordDamage(int damage, bool isBomb, Farmer who, bool isProjectile)
    {
        if (MeleeWeaponPatches.IsResolvingLocalMelee && damage > 0 && !isBomb && !isProjectile && who.IsLocalPlayer)
            ModEntry.Instance.RequestMeleeDamage(damage, MeleeWeaponPatches.IsClubAttack);
    }

    private static void RecordKill(bool isBomb, Farmer who, bool isProjectile)
    {
        if (MeleeWeaponPatches.IsResolvingLocalMelee && !isBomb && !isProjectile && who.IsLocalPlayer)
            ModEntry.Instance.RequestMeleeKill(MeleeWeaponPatches.IsClubAttack);
    }
}
