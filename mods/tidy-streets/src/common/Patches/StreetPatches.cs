using System;
using HarmonyLib;

namespace TidyStreets
{
    // The map's own trash comes from three spawners, each skipped here by name. All three pick
    // what to spawn with TrashManager.GetRandomGeneratableTrashPrefab, which nothing else uses, but
    // that call must not be patched (a null answer would crash them), so the spawners are skipped
    // whole. TrashManager.CreateTrashItem is never touched: stations, botanists and the save
    // loader go through it too.

    /// <summary>Whether a spawner may run: false (skip it) when its setting asks for no trash.</summary>
    internal static class StreetTrash
    {
        public static bool ShouldRun(string spawner, bool suppress)
        {
            try
            {
                if (!suppress) return true;

                ModLog.Verbose("The " + spawner + " would have made trash; it did not.");
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not stop the " + spawner + "'s trash: " + ex);
                return true;
            }
        }
    }

    /// <summary>Fills the map's areas with up to a fifth of their trash cap as you fall asleep.</summary>
    [HarmonyPatch(typeof(TrashGenerator), "SleepStart")]
    internal static class TrashGenerator_SleepStart_Patch
    {
        private static bool Prefix() => StreetTrash.ShouldRun("trash generator", ModConfig.NoGeneratorTrash);
    }

    /// <summary>Drops trash into set places as you wake up.</summary>
    [HarmonyPatch(typeof(TrashSpawnVolume), "SleepStart")]
    internal static class TrashSpawnVolume_SleepStart_Patch
    {
        private static bool Prefix() => StreetTrash.ShouldRun("trash spawn volume", ModConfig.NoSpawnVolumeTrash);
    }

    /// <summary>Spawns a set amount of trash in a volume when something asks it to.</summary>
    [HarmonyPatch(typeof(TrashGenerationVolume), "Generate")]
    internal static class TrashGenerationVolume_Generate_Patch
    {
        private static bool Prefix() => StreetTrash.ShouldRun("trash generation volume", ModConfig.NoGenerationVolumeTrash);
    }
}
