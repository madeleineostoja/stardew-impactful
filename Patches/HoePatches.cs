using System.Numerics;
using HarmonyLib;
using StardewValley;
using StardewValley.Tools;

namespace Impactful.Patches;

[HarmonyPatch(typeof(Hoe), nameof(Hoe.DoFunction), new[]
{
    typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer)
})]
internal static class HoePatches
{
    private sealed record State(int FacingDirection, List<KeyValuePair<Microsoft.Xna.Framework.Vector2, StardewValley.Object>> Spots);

    private static void Prefix(GameLocation location, Farmer who, ref State? __state)
    {
        if (!ModEntry.Instance.Config.ArtifactSpots || !who.IsLocalPlayer)
            return;

        var spots = location.Objects.Pairs
            .Where(pair => pair.Value.QualifiedItemId is "(O)590" or "(O)SeedSpot")
            .ToList();
        if (spots.Count > 0)
            __state = new State(who.FacingDirection, spots);
    }

    private static void Postfix(GameLocation location, State? __state)
    {
        if (__state is null || !__state.Spots.Any(spot => !location.Objects.TryGetValue(spot.Key, out var remaining) || !ReferenceEquals(remaining, spot.Value)))
            return;

        var direction = Vector2.Normalize(ModEntry.DirectionFromFacing(__state.FacingDirection) + new Vector2(0, 0.2f));
        ModEntry.Instance.Emit(ImpactTuning.ArtifactSpot, direction);
    }
}
