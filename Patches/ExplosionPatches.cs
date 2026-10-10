using HarmonyLib;
using StardewValley;

namespace Impactful.Patches;

[HarmonyPatch(typeof(GameLocation), nameof(GameLocation.explode), new[]
{
    typeof(Microsoft.Xna.Framework.Vector2), typeof(int), typeof(Farmer), typeof(bool), typeof(int), typeof(bool)
})]
internal static class ExplosionPatches
{
    private static void Prefix(int radius, ref int __state)
    {
        // explode halves its radius for the final soil pass.
        __state = radius;
    }

    private static void Postfix(GameLocation __instance, Microsoft.Xna.Framework.Vector2 tileLocation, int __state)
    {
        ModEntry.Instance.NotifyExplosion(__instance, tileLocation, __state);
    }
}
