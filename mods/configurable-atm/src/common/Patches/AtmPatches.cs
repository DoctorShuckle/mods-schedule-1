using System;
using HarmonyLib;

namespace ConfigurableAtm
{
    /// <summary>
    /// Runs as the ATM screen starts, on both builds: the game labels the amount buttons from its
    /// amounts there.
    /// </summary>
    [HarmonyPatch(typeof(ATMInterface), "Start")]
    internal static class ATMInterface_Start_Patch
    {
        // Before the game's Start, which reads the amounts to label the buttons.
        private static void Prefix()
        {
            try
            {
                GameAccess.ApplyPresets(ModConfig.Presets);
                GameAccess.ScreenStarted = true;
                ModLog.Verbose("ATM buttons set to " + string.Join(", ", ModConfig.Presets) + ".");
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not set the ATM's amount buttons: " + ex);
            }
        }
    }

    /// <summary>
    /// Shows or hides the "spent / limit" readout while an ATM is open, on both builds. With the limit
    /// off the game would draw the largest float there. It is done every update rather than as the
    /// screen starts so that switching the setting while the game runs takes effect on the open ATM.
    /// </summary>
    [HarmonyPatch(typeof(ATMInterface), "Update")]
    internal static class ATMInterface_Update_Readout_Patch
    {
        private static bool _warned;

        private static void Postfix(ATMInterface __instance)
        {
            try
            {
                if (__instance.IsOpen) GameAccess.SetLimitDisplayVisible(__instance, ModConfig.LimitEnabled);
            }
            catch (Exception ex)
            {
                if (_warned) return; // every frame: say it once
                _warned = true;
                ModLog.Warning("Could not show or hide the deposit limit readout: " + ex);
            }
        }
    }
}
