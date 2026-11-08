using System;
using HarmonyLib;

namespace MixPackagedDrugs
{
    // The game keeps packaged product out of a mixing station with a filter on its product slot, and the
    // mixing code counts items, not units. So the mod does a few things and leaves the mixing alone:
    //   * It answers yes for a packaged product in that one slot (DoesItemMatchHardFilters).
    //   * It counts the room in whole packages (GetCapacityForItem), working it out from the game's own
    //     answer for the same product unpackaged, so a brick is never accepted into room for fewer than 20
    //     units, and the game's own checks (a different product or quality, a lock) still apply.
    //   * A package added to the slot (AddItem) or stored in it (SetStoredItem) becomes the same product
    //     unpackaged, on whichever machine does it, before the game's own code runs: quantity times the
    //     package's units. The game then stacks it with any loose
    //     product already there, and a brick needs 20 mixers. The packaging is used up and the output is
    //     never packed again, so the mod saves nothing.
    // The game's inventory drag drops a stack into an empty slot without checking room, refuses to take units
    // off a stack that is over its limit, and swaps a package with a loose stack, so a drag onto the product
    // slot is handled in EndDrag.

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
        private static void Postfix(ItemSlot __instance, ItemInstance item, bool checkPlayerFilters, ref int __result)
        {
            try
            {
                if (!GameAccess.IsPackagedProduct(item, out int amount) || !GameAccess.IsAllowed(amount)) return;
                if (!GameAccess.IsMixerProductSlot(__instance)) return;

                __result = GameAccess.PackageCapacity(__instance, item, checkPlayerFilters);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not count the room for a packaged product: " + ex);
            }
        }
    }

    /// <summary>Unpacks a packaged product stored in a mixing station's product slot.</summary>
    [HarmonyPatch(typeof(ItemSlot), "SetStoredItem")]
    internal static class ItemSlot_SetStoredItem_Patch
    {
        private static void Prefix(ItemSlot __instance, ref ItemInstance instance)
        {
            try
            {
                if (!GameAccess.IsPackagedProduct(instance, out int amount) || !GameAccess.IsAllowed(amount)) return;
                if (!GameAccess.IsMixerProductSlot(__instance)) return;

                ItemInstance loose = GameAccess.Unpackaged(instance);
                if (loose == null) return;
                instance = loose;
                ModLog.Verbose("Unpacked a package of " + amount + " stored in a mixing station.");
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not unpack a package in the mixing station: " + ex);
            }
        }
    }

    /// <summary>Unpacks a package added to a mixing station's product slot, so it stacks with loose product.</summary>
    [HarmonyPatch(typeof(ItemSlot), "AddItem")]
    internal static class ItemSlot_AddItem_Patch
    {
        private static void Prefix(ItemSlot __instance, ref ItemInstance item)
        {
            try
            {
                if (!GameAccess.IsPackagedProduct(item, out int amount)) return;
                if (!GameAccess.IsAllowed(amount) || !GameAccess.IsMixerProductSlot(__instance)) return;

                ItemInstance loose = GameAccess.Unpackaged(item);
                if (loose == null) return;
                item = loose;
                ModLog.Verbose("Unpacked a package of " + amount + " added to a mixing station.");
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not unpack a package added to the mixing station: " + ex);
            }
        }
    }

    /// <summary>Cuts a drag of packaged product onto a mixing station's product slot to what fits, or unpacks it into a loose stack.</summary>
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
}
