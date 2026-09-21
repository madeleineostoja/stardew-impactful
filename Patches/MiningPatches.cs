using System.Numerics;
using HarmonyLib;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Tools;

namespace Impactful.Patches;

[HarmonyPatch(typeof(Pickaxe), nameof(Pickaxe.DoFunction))]
internal static class MiningPatches
{
    private sealed record State(Microsoft.Xna.Framework.Vector2 Tile, StardewValley.Object Stone);

    private static void Prefix(GameLocation location, int x, int y, Farmer who, ref State? __state)
    {
        if (!ModEntry.Instance.Config.Mining || !who.IsLocalPlayer)
            return;

        var target = GetTargetTile(location, x, y, who.FacingDirection);
        if (location.Objects.TryGetValue(target, out var item) && item.IsBreakableStone())
            __state = new State(target, item);
    }

    private static void Postfix(GameLocation location, Farmer who, State? __state)
    {
        if (__state is null || !who.IsLocalPlayer)
            return;

        var direction = Vector2.Normalize(ModEntry.DirectionFromFacing(who.FacingDirection) + new Vector2(0, 0.25f));
        ModEntry.Instance.Emit(ImpactTuning.MiningHit, 55, direction);
        if (!location.Objects.TryGetValue(__state.Tile, out var remaining) || !ReferenceEquals(remaining, __state.Stone))
            ModEntry.Instance.Emit(ImpactTuning.StoneBreak, 95, direction);
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
