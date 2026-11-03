using System;
using HarmonyLib;

namespace SkipDisclaimer
{
    // Disclaimer.Awake runs when the menu scene loads. It closes the screen at once (invoking OnClose,
    // which the menu waits on) when the static Shown flag is already set, and otherwise shows the
    // text for a few seconds. The mod sets that flag first, so the game takes its own quick path.

    /// <summary>Marks the disclaimer as already shown before the game decides whether to display it.</summary>
    [HarmonyPatch(typeof(Disclaimer), "Awake")]
    internal static class Disclaimer_Awake_Patch
    {
        private static void Prefix()
        {
            try
            {
                if (!ModConfig.Skip || Disclaimer.Shown) return;
                Disclaimer.Shown = true;
                ModLog.Verbose("Disclaimer skipped.");
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not skip the disclaimer: " + ex);
            }
        }
    }
}
