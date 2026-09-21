using HarmonyLib;
using StardewValley;

namespace Impactful.Patches;

[HarmonyPatch(typeof(GameLocation), nameof(GameLocation.explode), new[]
{
    typeof(Microsoft.Xna.Framework.Vector2), typeof(int), typeof(Farmer), typeof(bool), typeof(int), typeof(bool)
})]
internal static class ExplosionPatches
{
    private static void Postfix(GameLocation __instance, Microsoft.Xna.Framework.Vector2 tileLocation, int radius)
    {
        ModEntry.Instance.NotifyExplosion(__instance, tileLocation, radius);
    }
}
