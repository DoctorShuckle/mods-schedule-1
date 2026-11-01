using System;
using HarmonyLib;

namespace TidyStations
{
    // The three stations make their trash in a method called when the job is done: the chemistry
    // station and the cauldron on the station itself, the mixing station on its task. Each loops
    // over the used items and spawns their packaging, so skipping the method skips the trash.

    /// <summary>Whether a station's trash method may run: false (skip it) when that station's setting asks for no trash.</summary>
    internal static class StationTrash
    {
        public static bool ShouldRun(string station, bool suppress)
        {
            try
            {
                if (!suppress) return true;

                ModLog.Verbose("The " + station + " would have made trash; it did not.");
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not stop the " + station + "'s trash: " + ex);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(ChemistryStation), "CreateTrash")]
    internal static class ChemistryStation_CreateTrash_Patch
    {
        private static bool Prefix() => StationTrash.ShouldRun("chemistry station", ModConfig.NoChemistryStationTrash);
    }

    [HarmonyPatch(typeof(Cauldron), "CreateTrash")]
    internal static class Cauldron_CreateTrash_Patch
    {
        private static bool Prefix() => StationTrash.ShouldRun("cauldron", ModConfig.NoCauldronTrash);
    }

    [HarmonyPatch(typeof(UseMixingStationTask), "CreateTrash")]
    internal static class UseMixingStationTask_CreateTrash_Patch
    {
        private static bool Prefix() => StationTrash.ShouldRun("mixing station", ModConfig.NoMixingStationTrash);
    }
}
