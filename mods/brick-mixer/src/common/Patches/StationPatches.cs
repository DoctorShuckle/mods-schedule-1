using System;
using HarmonyLib;

namespace BrickMixer
{
    // Rules for the Brick Mixer's slots and batch. The game's mixing code counts items, not units, so a brick
    // is unpacked as it goes in (20 units) and the finished batch is packed as a brick again.
    // The slot filter and capacity patches run last so they win over another mod that opens the slot wider.

    /// <summary>The product slot takes bricks and nothing else.</summary>
    [HarmonyPatch(typeof(ItemSlot), "DoesItemMatchHardFilters")]
    internal static class ItemSlot_DoesItemMatchHardFilters_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ItemSlot __instance, ItemInstance item, ref bool __result)
        {
            try
            {
                if (item == null || !GameAccess.IsBrickProductSlot(__instance)) return;
                __result = GameAccess.IsBrickPackage(item);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not check an item for the Brick Mixer: " + ex);
            }
        }
    }

    /// <summary>Counts the room for a brick in bricks rather than in items.</summary>
    [HarmonyPatch(typeof(ItemSlot), "GetCapacityForItem")]
    internal static class ItemSlot_GetCapacityForItem_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ItemSlot __instance, ItemInstance item, ref int __result)
        {
            try
            {
                if (__result <= 0 || !GameAccess.IsBrickPackage(item) || !GameAccess.IsBrickProductSlot(__instance)) return;

                // The game answered stack limit minus what is there, in items. Here an item is a brick.
                int current = __instance.Quantity;
                __result = BrickRules.BricksThatFit(__result + current, current);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not count the room for a brick: " + ex);
            }
        }
    }

    /// <summary>Unpacks a brick the moment it is stored in a Brick Mixer's product slot (host only).</summary>
    [HarmonyPatch(typeof(ItemSlot), "SetStoredItem")]
    internal static class ItemSlot_SetStoredItem_Patch
    {
        // Storing the unpacked copy runs this patch again; it finds nothing packaged and stops.
        private static bool _unpacking;

        private static void Postfix(ItemSlot __instance)
        {
            try
            {
                if (_unpacking || !GameAccess.IsServer || !GameAccess.IsBrickPackage(__instance.ItemInstance)) return;
                if (!GameAccess.IsBrickProductSlot(__instance)) return;

                _unpacking = true;
                try
                {
                    if (GameAccess.Unpack(__instance)) ModLog.Verbose("Unpacked a brick in a Brick Mixer.");
                }
                finally { _unpacking = false; }
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not unpack a brick in the Brick Mixer: " + ex);
            }
        }
    }

    /// <summary>Packs a finished batch as a brick when it is put in a Brick Mixer's output slot (host only).</summary>
    [HarmonyPatch(typeof(ItemSlot), "AddItem")]
    internal static class ItemSlot_AddItem_Patch
    {
        private static void Prefix(ItemSlot __instance, ref ItemInstance item)
        {
            try
            {
                if (!GameAccess.IsServer || item == null || !GameAccess.IsBrickOutputSlot(__instance)) return;
                if (GameAccess.Repack(ref item, out int units, out int bricks))
                    ModLog.Verbose("Packed a finished mix of " + units + " units into " + bricks + " brick(s).");
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not pack a finished mix: " + ex);
            }
        }
    }

    /// <summary>A Brick Mixer mixes a whole brick (20 units, 20 mixers) or nothing.</summary>
    [HarmonyPatch(typeof(MixingStation), "GetMixQuantity")]
    internal static class MixingStation_GetMixQuantity_Patch
    {
        private static void Postfix(MixingStation __instance, ref int __result)
        {
            try
            {
                if (!GameAccess.IsBrickMixer(__instance)) return;
                // The copy's batch limit is 20, so the game's answer is 20 only when 20 units of product and 20
                // mixers are in; anything less is not a batch.
                __result = __result >= BrickRules.BrickUnits ? BrickRules.BrickUnits : 0;
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not set the Brick Mixer's batch: " + ex);
            }
        }
    }

    /// <summary>Keeps the start button off until a whole brick and 20 mixers are in.</summary>
    [HarmonyPatch(typeof(MixingStation), "CanStartMix")]
    internal static class MixingStation_CanStartMix_Patch
    {
        private static void Postfix(MixingStation __instance, ref bool __result)
        {
            try
            {
                if (!__result || !GameAccess.IsBrickMixer(__instance)) return;
                __result = __instance.GetMixQuantity() >= BrickRules.BrickUnits;
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not check whether the Brick Mixer can start: " + ex);
            }
        }
    }

    /// <summary>Cuts a drag of bricks onto a Brick Mixer's product slot to what fits.</summary>
    [HarmonyPatch(typeof(ItemUIManager), "EndDrag")]
    internal static class ItemUIManager_EndDrag_Patch
    {
        private static void Prefix(ItemUIManager __instance)
        {
            try
            {
                GameAccess.ClampDrop(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not check a drop on the Brick Mixer: " + ex);
            }
        }
    }
}
