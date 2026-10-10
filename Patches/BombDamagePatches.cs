using HarmonyLib;
using StardewValley;

namespace Impactful.Patches;

[HarmonyPatch(typeof(GameLocation), "performDamagePlayers")]
internal static class BombDamagePatches
{
    private static readonly System.Reflection.FieldInfo IsBombField = AccessTools.Field(
        AccessTools.Inner(typeof(GameLocation), "DamagePlayersEventArg"), "IsBomb");

    [ThreadStatic]
    private static bool resolvingBombDamage;

    internal static bool IsResolvingBombDamage => resolvingBombDamage;

    private static void Prefix(object arg, ref bool __state)
    {
        __state = resolvingBombDamage;
        // Bomb damage can arrive through a queued network event after explode
        // returns, so scope its own callback rather than the explosion call.
        resolvingBombDamage = (bool)IsBombField.GetValue(arg)!;
    }

    private static void Finalizer(bool __state)
    {
        resolvingBombDamage = __state;
    }
}
