using System;
using System.Collections.Generic;
using HarmonyLib;

namespace PersistentPotAdditives
{
    /// <summary>
    /// Keeps a pot's additives across harvests while its soil still has uses left.
    /// </summary>
    [HarmonyPatch(typeof(Pot), "OnPlantFullyHarvested")]
    internal static class Pot_OnPlantFullyHarvested_Patch
    {
        /// <summary>
        /// Runs before the game's harvest handler. The returned list is Harmony "state": it is
        /// passed to <see cref="Postfix"/> for the same call and then discarded.
        /// </summary>
        private static void Prefix(Pot __instance, out List<string> __state)
        {
            // Null means "nothing to restore"; every early exit below leaves it that way.
            __state = null;

            try
            {
                // Only the host decides (every machine runs this handler, but the host's restore
                // is broadcast to everyone), and only if the game's handler will really run: it
                // does nothing without a plant, and can be signalled twice.
                if (__instance == null) return;
                if (!HarvestRestore.ShouldCapture(GameAccess.IsServer, GameAccess.HasPlant(__instance))) return;

                // Copy now: the game empties its list a few lines into the handler.
                __state = GameAccess.AppliedAdditiveIds(__instance);
            }
            catch (Exception ex)
            {
                // A failure here must never break the game's harvest, so log and carry on.
                ModLog.Warning("Could not note additives before harvest: " + ex);
            }
        }

        /// <summary>
        /// Runs after the game's harvest handler, which has by now used up one soil use and
        /// cleared the additives.
        /// </summary>
        private static void Postfix(Pot __instance, List<string> __state)
        {
            try
            {
                if (__instance == null || __state == null) return;

                // The handler has already decremented the uses, so this is "uses left for the
                // NEXT plant". At zero the game has cleared the soil itself (ClearSoil also clears
                // additives) and that is exactly when additives should end.
                int usesLeft = GameAccess.RemainingUses(__instance);
                if (!HarvestRestore.ShouldRestore(__state.Count, usesLeft))
                {
                    ModLog.Verbose("Nothing to restore (" + __state.Count + " captured, " + usesLeft + " soil use(s) left).");
                    return;
                }

                int restored = 0;
                // Skips anything already back on the pot, so nothing is applied twice.
                foreach (string id in HarvestRestore.IdsToRestore(__state, id => GameAccess.IsApplied(__instance, id)))
                {
                    GameAccess.ApplyAdditive(__instance, id);
                    restored++;
                }

                ModLog.Verbose("Restored " + restored + " additive(s) after harvest; " + usesLeft + " soil use(s) left.");
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not restore additives after harvest: " + ex);
            }
        }
    }
}
