using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Impactful.Patches;

[HarmonyPatch(typeof(Game1), nameof(Game1.UpdateViewPort), new[] { typeof(bool), typeof(Point) })]
internal static class CameraPatches
{
    private static void Prefix()
    {
        // Vanilla's club special changes the viewport immediately before this
        // update. Inject queued impacts at the same point so the native camera
        // interpolation controls their visible kick, overshoot, and settling.
        ModEntry.Instance.ApplyPendingCameraImpulse();
    }
}
