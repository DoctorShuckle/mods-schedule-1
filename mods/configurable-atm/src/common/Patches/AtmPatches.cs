using System;
using HarmonyLib;

namespace ConfigurableAtm
{
    /// <summary>
    /// Runs as the ATM screen starts, on both builds: the game labels the amount buttons from its
    /// amounts there, and shows the limit readout there.
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

        // After the game's Start, which shows the readout unconditionally.
        private static void Postfix(ATMInterface __instance)
        {
            try
            {
                if (!ModConfig.LimitEnabled) GameAccess.SetLimitDisplayVisible(__instance, false);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not hide the deposit limit readout: " + ex);
            }
        }
    }
}
