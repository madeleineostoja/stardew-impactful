using HarmonyLib;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace Impactful.Patches;

[HarmonyPatch(typeof(Pickaxe), nameof(Pickaxe.DoFunction), new[]
{
    typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer)
})]
internal static class OrdinaryRockPatches
{
    private readonly record struct State(Microsoft.Xna.Framework.Vector2 Tile, StardewValley.Object Rock, int FacingDirection);

    private static void Prefix(GameLocation location, int x, int y, Farmer who, ref State? __state)
    {
        if (!ModEntry.Instance.Config.Mining || !who.IsLocalPlayer)
            return;

        var target = GetTargetTile(location, x, y, who.FacingDirection);
        if (location.Objects.TryGetValue(target, out var item) && item.IsBreakableStone())
            __state = new State(target, item, who.FacingDirection);
    }

    private static void Postfix(GameLocation location, State? __state)
    {
        if (__state is not { } state || (location.Objects.TryGetValue(state.Tile, out var remaining) && ReferenceEquals(remaining, state.Rock)))
            return;

        ModEntry.Instance.Emit(ImpactTuning.OrdinaryRockBreak, ModEntry.DirectionFromFacing(state.FacingDirection));
    }

    private static Microsoft.Xna.Framework.Vector2 GetTargetTile(GameLocation location, int x, int y, int facingDirection)
    {
        var tile = new Microsoft.Xna.Framework.Vector2(x / 64, y / 64);
        if (location.Objects.ContainsKey(tile))
            return tile;

        // This mirrors Pickaxe.DoFunction's permissive ±8-pixel edge lookup.
        var first = facingDirection is 0 or 2
            ? new Microsoft.Xna.Framework.Vector2((x - 8) / 64, y / 64)
            : new Microsoft.Xna.Framework.Vector2(x / 64, (y + 8) / 64);
        if (location.Objects.ContainsKey(first))
            return first;

        return facingDirection is 0 or 2
            ? new Microsoft.Xna.Framework.Vector2((x + 8) / 64, y / 64)
            : new Microsoft.Xna.Framework.Vector2(x / 64, (y - 8) / 64);
    }
}

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
