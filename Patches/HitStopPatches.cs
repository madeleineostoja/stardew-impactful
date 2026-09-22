using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Impactful.Patches;

[HarmonyPatch(typeof(Game1), "Update", new[] { typeof(GameTime) })]
internal static class HitStopPatches
{
    private static void Prefix(ref PauseState __state)
    {
        if (!ModEntry.Instance.TryConsumeHitStopFrame())
            return;

        // Let the game take its normal single-player paused path instead of
        // skipping the update and bypassing other patches outright.
        __state = new PauseState(Game1.paused);
        Game1.paused = true;
    }

    private static void Postfix(ref PauseState __state)
    {
        RestorePauseState(ref __state);
    }

    private static Exception? Finalizer(Exception? __exception, ref PauseState __state)
    {
        RestorePauseState(ref __state);
        return __exception;
    }

    private static void RestorePauseState(ref PauseState state)
    {
        if (!state.Applied)
            return;

        Game1.paused = state.WasPaused;
        state.Applied = false;
    }

    private struct PauseState
    {
        public PauseState(bool wasPaused)
        {
            this.Applied = true;
            this.WasPaused = wasPaused;
        }

        public bool Applied;
        public readonly bool WasPaused;
    }
}
