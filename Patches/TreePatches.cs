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
        if (!__state && __instance.falling.Value && t?.getLastFarmerToUse()?.IsLocalPlayer == true)
            ModEntry.Instance.MarkLocalTreeFall(__instance);
    }
}
