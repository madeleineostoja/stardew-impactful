using HarmonyLib;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace Impactful.Patches;

[HarmonyPatch(typeof(ResourceClump), nameof(ResourceClump.performToolAction), new[]
{
    typeof(Tool), typeof(int), typeof(Microsoft.Xna.Framework.Vector2)
})]
internal static class MiningPatches
{
    private readonly record struct State(float Health, int FacingDirection);

    private static void Prefix(ResourceClump __instance, Tool? t, ref State? __state)
    {
        var who = t?.getLastFarmerToUse();
        if (ModEntry.Instance.Config.Mining && t is Pickaxe && who?.IsLocalPlayer == true)
            __state = new State(__instance.health.Value, who.FacingDirection);
    }

    private static void Postfix(ResourceClump __instance, bool __result, State? __state)
    {
        if (__state is not { Health: > 0 } state || !__result || __instance.health.Value > 0)
            return;

        ModEntry.Instance.Emit(ImpactTuning.LargeRockBreak, ModEntry.DirectionFromFacing(state.FacingDirection));
    }
}
