using System;
using HarmonyLib;

namespace BrickMixer
{
    // Rules for the Brick Mixer's batch and output. What goes into its product slot is the game's own filter,
    // plus whatever another mod opens on every mixing station: with Mix Packaged Drugs installed it also takes
    // baggies, jars and bricks, which are unpacked as they go in. The game's mixing code counts items, not
    // units, so a full batch is 20 units and the finished batch is packed as a brick.

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
}
