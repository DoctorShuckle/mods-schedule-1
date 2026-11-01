using System;
using HarmonyLib;

namespace TidyStations
{
    // When a botanist finishes a task, GrowContainerBehaviour.PerformAction asks the behaviour what
    // to drop (GetTrashPrefab) and spawns it at the botanist's feet if the answer is not null. Three
    // behaviours override that question; answering "nothing" is the whole mod. The other botanist
    // tasks (water, harvest, mushroom spawn) drop nothing in the game.
    //
    // One class per behaviour so a game update that removes one method turns off one patch only.

    internal static class TrashRule
    {
        public static void Suppress(string task, bool suppress, ref TrashItem result)
        {
            try
            {
                if (!suppress || result == null) return;

                ModLog.Verbose("A botanist's " + task + " task would have dropped trash; it did not.");
                result = null;
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not stop a botanist's trash: " + ex);
            }
        }
    }

    /// <summary>The empty soil bag.</summary>
    [HarmonyPatch(typeof(AddSoilToGrowContainerBehaviour), "GetTrashPrefab")]
    internal static class AddSoil_GetTrashPrefab_Patch
    {
        private static void Postfix(ref TrashItem __result) => TrashRule.Suppress("soil", ModConfig.NoBotanistSoilTrash, ref __result);
    }

    /// <summary>The empty fertilizer, PGR, Speed Grow and other additive containers.</summary>
    [HarmonyPatch(typeof(ApplyAdditiveToGrowContainerBehaviour), "GetTrashPrefab")]
    internal static class ApplyAdditive_GetTrashPrefab_Patch
    {
        private static void Postfix(ref TrashItem __result) => TrashRule.Suppress("additive", ModConfig.NoBotanistAdditiveTrash, ref __result);
    }

    /// <summary>The empty seed packet.</summary>
    [HarmonyPatch(typeof(SowSeedInPotBehaviour), "GetTrashPrefab")]
    internal static class SowSeed_GetTrashPrefab_Patch
    {
        private static void Postfix(ref TrashItem __result) => TrashRule.Suppress("sowing", ModConfig.NoBotanistSeedTrash, ref __result);
    }
}
