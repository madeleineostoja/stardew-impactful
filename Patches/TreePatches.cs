using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace Impactful.Patches;

[HarmonyPatch(typeof(Tree), nameof(Tree.performToolAction), new[] { typeof(Tool), typeof(int), typeof(Vector2) })]
internal static class TreeFallStartPatches
{
    private static void Prefix(Tree __instance, Tool? t, ref bool? __state)
    {
        if (ModEntry.Instance.CanShake && ModEntry.Instance.Config.Trees && t?.getLastFarmerToUse()?.IsLocalPlayer == true)
            __state = __instance.falling.Value;
    }

    private static void Postfix(Tree __instance, bool? __state)
    {
        if (__state == false && __instance.falling.Value)
            ModEntry.Instance.MarkLocalTreeFall(__instance);
    }
}

[HarmonyPatch(typeof(Tree), nameof(Tree.tickUpdate))]
internal static class TreeLandingPatches
{
    private static void Prefix(Tree __instance, ref bool __state)
    {
        __state = __instance.falling.Value && ModEntry.Instance.IsTrackingTreeFall(__instance);
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
