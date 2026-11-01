using System;
using HarmonyLib;

namespace TidyStations
{
    /// <summary>
    /// The scrap a plant drops when its last harvestable is picked. OnFullyHarvested spawns it only
    /// if the plant has a scrap prefab, then destroys the plant, so clearing that field on this one
    /// plant, just before, stops the scrap and leaves nothing behind to restore.
    /// </summary>
    [HarmonyPatch(typeof(Plant), "OnFullyHarvested")]
    internal static class Plant_OnFullyHarvested_Patch
    {
        private static void Prefix(Plant __instance)
        {
            try
            {
                if (!ModConfig.NoPlantScrapTrash || __instance == null || __instance.PlantScrapPrefab == null) return;

                ModLog.Verbose("A harvested plant would have dropped scrap; it did not.");
                __instance.PlantScrapPrefab = null;
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not stop a plant's scrap: " + ex);
            }
        }
    }
}
