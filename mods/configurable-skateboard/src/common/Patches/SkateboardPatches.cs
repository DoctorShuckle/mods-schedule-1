using System;
using HarmonyLib;

namespace ConfigurableSkateboard
{
    /// <summary>Tunes a new board as it starts, before any weather change has given it its own settings.</summary>
    [HarmonyPatch(typeof(Skateboard), "Start")]
    internal static class Skateboard_Start_Patch
    {
        private static void Postfix(Skateboard __instance) => BoardTuner.AtStart(__instance);
    }

    /// <summary>Tunes the settings the game just rebuilt (default, plus the rain's effect).</summary>
    [HarmonyPatch(typeof(Skateboard), "OnWeatherChange")]
    internal static class Skateboard_OnWeatherChange_Patch
    {
        private static void Postfix(Skateboard __instance) => BoardTuner.AfterWeatherChange(__instance);
    }

    /// <summary>
    /// Scales what a push costs. The push spends stamina a little each frame through this method,
    /// and the game's start-of-push check against 12.5 is a constant the mod cannot reach, so the
    /// cost is lowered but the check stays.
    /// </summary>
    [HarmonyPatch(typeof(PlayerMovement), "ChangeStamina")]
    internal static class PlayerMovement_ChangeStamina_Patch
    {
        private static void Prefix(ref float change)
        {
            try
            {
                change = Tuning.StaminaChange(change, ModConfig.PushStamina, GameAccess.IsRiding);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not scale the stamina cost: " + ex);
            }
        }
    }
}
