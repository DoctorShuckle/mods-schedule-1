using System;
using HarmonyLib;

namespace MixPackagedDrugs
{
    // The game keeps packaged product out of a mixing station with a filter on its product slot, and the
    // mixing code counts items, not units. So the mod does two things and leaves the mixing alone:
    //   * It answers yes for a packaged product in that one slot (DoesItemMatchHardFilters), and counts the
    //     room in whole packages (GetCapacityForItem), so a brick is never accepted into room for fewer
    //     than 20 units.
    //   * As soon as a packaged stack lands in the slot (SetStoredItem, on the host), it swaps it for the same
    //     product unpackaged: quantity times the package's units. From then on the game sees ordinary
    //     units, so a brick needs 20 mixers, and the hands-on mix, the Chemist and the batch size work as ever.
    //     The packaging is remembered for that station.
    //   * The memory is written to a file of the mod's own when the game saves (SaveManager.Save), so a mix
    //     running across a reload still comes out packed.
    //   * When the mix finishes, the output is put into the output slot (AddItem on the host). If the station
    //     remembers a packaging and the batch fills whole packages, the output is packed again that way.
    // The game's inventory drag drops a stack into an empty slot without checking room, and the game refuses
    // to take units off a stack that is over its limit, so a drag onto the product slot is cut down to the
    // whole packages that fit (EndDrag).

    /// <summary>Lets a packaged product into a mixing station's product slot.</summary>
    [HarmonyPatch(typeof(ItemSlot), "DoesItemMatchHardFilters")]
    internal static class ItemSlot_DoesItemMatchHardFilters_Patch
    {
        private static void Postfix(ItemSlot __instance, ItemInstance item, ref bool __result)
        {
            try
            {
                if (__result || !GameAccess.IsPackagedProduct(item, out int amount)) return;
                if (!GameAccess.IsAllowed(amount) || !GameAccess.IsMixerProductSlot(__instance)) return;
                __result = true;
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not check a packaged product for the mixing station: " + ex);
            }
        }
    }

    /// <summary>Counts the room for a packaged product in packages rather than in items.</summary>
    [HarmonyPatch(typeof(ItemSlot), "GetCapacityForItem")]
    internal static class ItemSlot_GetCapacityForItem_Patch
    {
        private static void Postfix(ItemSlot __instance, ItemInstance item, ref int __result)
        {
            try
            {
                if (__result <= 0 || !GameAccess.IsPackagedProduct(item, out int amount)) return;
                if (!GameAccess.IsMixerProductSlot(__instance)) return;

                // The game answered stack limit minus what is there, in items. Here an item is a package.
                int current = __instance.Quantity;
                __result = Packages.PackagesThatFit(__result + current, current, amount);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not count the room for a packaged product: " + ex);
            }
        }
    }

    /// <summary>Unpacks a packaged product the moment it is stored in a mixing station's product slot.</summary>
    [HarmonyPatch(typeof(ItemSlot), "SetStoredItem")]
    internal static class ItemSlot_SetStoredItem_Patch
    {
        // SetStoredItem on the replaced stack runs this patch again; it finds nothing packaged and stops.
        internal static bool Unpacking;

        private static void Postfix(ItemSlot __instance)
        {
            try
            {
                if (Unpacking || !GameAccess.IsServer) return;
                if (!GameAccess.IsPackagedProduct(__instance.ItemInstance, out int amount)) return;
                if (!GameAccess.IsMixerProductSlot(__instance)) return;

                Unpacking = true;
                try
                {
                    if (GameAccess.Unpack(__instance))
                        ModLog.Verbose("Unpacked a package of " + amount + " in a mixing station.");
                }
                finally { Unpacking = false; }
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not unpack a package in the mixing station: " + ex);
            }
        }
    }

    /// <summary>Forgets a station's remembered packaging when loose product is put in its product slot.</summary>
    [HarmonyPatch(typeof(ItemSlot), "SetStoredItem")]
    internal static class ItemSlot_SetStoredItem_Forget_Patch
    {
        private static void Postfix(ItemSlot __instance)
        {
            try
            {
                if (!GameAccess.IsServer || ItemSlot_SetStoredItem_Patch.Unpacking) return;
                if (!GameAccess.IsLooseProduct(__instance.ItemInstance) || !GameAccess.IsMixerProductSlot(__instance)) return;
                GameAccess.ForgetPackaging(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not update the station's packaging: " + ex);
            }
        }
    }

    /// <summary>Puts a finished mix back in the packaging it went in as, when it fills whole packages.</summary>
    [HarmonyPatch(typeof(ItemSlot), "AddItem")]
    internal static class ItemSlot_AddItem_Patch
    {
        private static void Prefix(ItemSlot __instance, ref ItemInstance item)
        {
            try
            {
                if (!GameAccess.IsServer || item == null || !GameAccess.IsMixerOutputSlot(__instance)) return;
                if (GameAccess.Repack(__instance, ref item, out int units, out int packages))
                    ModLog.Verbose("Packed a finished mix of " + units + " units into " + packages + " packages.");
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not repack a finished mix: " + ex);
            }
        }
    }

    /// <summary>Cuts a drag of packaged product onto a mixing station's product slot to what fits.</summary>
    [HarmonyPatch(typeof(ItemUIManager), "EndDrag")]
    internal static class ItemUIManager_EndDrag_Patch
    {
        private static void Prefix(ItemUIManager __instance)
        {
            try
            {
                GameAccess.ClampMixerDrop(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not check a drop on the mixing station: " + ex);
            }
        }
    }

    /// <summary>Writes the remembered packagings next to the game's own save.</summary>
    [HarmonyPatch(typeof(SaveManager), "Save", new[] { typeof(string) })]
    internal static class SaveManager_Save_Patch
    {
        private static void Postfix()
        {
            try
            {
                if (GameAccess.IsServer) GameAccess.WritePackagingMemory();
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not save the remembered packagings; mixes in progress will come out loose after a reload: " + ex.Message);
            }
        }
    }

    /// <summary>Forgets a station's packaging when the station is removed.</summary>
    [HarmonyPatch(typeof(MixingStation), "Destroy")]
    internal static class MixingStation_Destroy_Patch
    {
        private static void Postfix(MixingStation __instance)
        {
            try
            {
                if (GameAccess.IsServer) GameAccess.ForgetPackaging(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not forget a removed station's packaging: " + ex.Message);
            }
        }
    }
}
