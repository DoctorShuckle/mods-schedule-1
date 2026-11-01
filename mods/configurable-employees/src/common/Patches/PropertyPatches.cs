using System;
using HarmonyLib;

namespace ConfigurableEmployees
{
    /// <summary>
    /// Employee capacity per property. Runs before the property initialises so its listing poster
    /// and the hiring dialogue already see the new number. It only sees properties whose own Awake
    /// is the base one; PropertyTuning.ApplyAll, run after the scene loads, covers the others.
    /// </summary>
    [HarmonyPatch(typeof(GameProperty), "Awake")]
    internal static class Property_Awake_Patch
    {
        private static void Prefix(GameProperty __instance)
        {
            try { PropertyTuning.Apply(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not set a property's employee capacity: " + ex); }
        }
    }
}
