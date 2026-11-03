using System;
using HarmonyLib;

namespace ConfigurableVehicles
{
    /// <summary>
    /// Applies a vehicle's settings as it starts. Not Awake: the vehicle's wheels are only filled in
    /// by Start, and the vehicle manager does not exist yet when the first vehicles wake.
    /// </summary>
    [HarmonyPatch(typeof(LandVehicle), "Start")]
    internal static class LandVehicle_Start_Patch
    {
        private static bool _listedCodes;

        private static void Postfix(LandVehicle __instance)
        {
            try
            {
                if (ModLog.VerboseEnabled && !_listedCodes && NetworkSingleton<VehicleManager>.InstanceExists)
                {
                    _listedCodes = true;
                    ModLog.Verbose("Vehicle codes: " + GameAccess.KnownCodes());
                }
                VehicleTuner.Apply(__instance);
            }
            catch (Exception ex)
            {
                ModLog.WarnOnce("Could not tune a vehicle: " + ex);
            }
        }
    }

    /// <summary>
    /// Sets the trunk's size before the storage builds its slots. SlotCount is read once, in Awake,
    /// so this has to run first.
    /// </summary>
    [HarmonyPatch(typeof(StorageEntity), "Awake")]
    internal static class StorageEntity_Awake_Patch
    {
        private static void Prefix(StorageEntity __instance)
        {
            try
            {
                LandVehicle vehicle = GameAccess.VehicleOf(__instance);
                if (vehicle == null) return;

                int slots = ModConfig.TrunkSlotsFor(vehicle.VehicleCode);
                if (slots <= 0) return;

                int rows = VehicleSettings.RowsFor(ModConfig.TrunkRows, slots);
                __instance.SlotCount = slots;
                __instance.DisplayRowCount = rows;
                ModLog.Verbose(vehicle.VehicleCode + " trunk set to " + slots + " slots in " + rows + " rows.");
            }
            catch (Exception ex)
            {
                ModLog.WarnOnce("Could not set the trunk size: " + ex);
            }
        }
    }
}
