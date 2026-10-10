using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace Impactful.Patches;

[HarmonyPatch(typeof(Tree), nameof(Tree.performToolAction), new[] { typeof(Tool), typeof(int), typeof(Vector2) })]
internal static class TreeFallStartPatches
{
    private static void Prefix(Tree __instance, ref bool __state)
    {
        __state = __instance.falling.Value;
    }

    private static void Postfix(Tree __instance, Tool? t, bool __state)
    {
        if (!__state && __instance.falling.Value)
            ModEntry.Instance.TrackTreeFall(__instance, t?.getLastFarmerToUse()?.IsLocalPlayer == true);
    }
}

[HarmonyPatch(typeof(Tree), nameof(Tree.tickUpdate))]
internal static class TreeLandingPatches
{
    private static void Prefix(Tree __instance, ref bool __state)
    {
        __state = __instance.falling.Value;
    }

    private static void Postfix(Tree __instance, bool __result, bool __state)
    {
        if (!__state)
            return;

        if (!__instance.falling.Value)
            ModEntry.Instance.TriggerTreeLanding(__instance);
        else if (__result)
            ModEntry.Instance.ForgetTreeFall(__instance);
    }
}
