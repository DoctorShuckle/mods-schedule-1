using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

// ---------------------------------------------------------------------------------------------
// How the mod works, and why it needs no save data
//
// The game wipes a pot's additives at the end of every harvest (Pot.OnPlantFullyHarvested ->
// ClearAdditives), even when the soil still has uses left. This mod makes additives last for the
// soil's whole lifetime instead. It does so with two small patches and stores NOTHING:
//
//   1. Harvest patch.  Just before the game clears the pot we copy the pot's additive ids into a
//      local variable; just after, if the soil still has uses, we put them back. That copy lives
//      only for the duration of that one method call, so there is nothing to save or load.
//      The game already saves the pot's additives (PotData.AppliedAdditives) and the soil's
//      remaining uses, so they survive save/load, and the game's own clear-on-last-use
//      (ClearSoil) still removes the additives when the soil is spent.
//
//   2. Replant patch.  When the next seed goes in, the game already hands the pot's additives
//      to the new plant, which restores Fertilizer (quality) and PGR (yield). It deliberately
//      leaves out Speed Grow's one-shot growth boost, which only applies when an additive is
//      first added. We add that boost ourselves so Speed Grow works on every plant.
//
// Why "restore after" rather than "block ClearAdditives"? Blocking would mean patching a call
// that happens inside native game code on IL2CPP, which can be inlined and then silently never
// fire. Restoring afterwards depends only on two ordinary public methods. As a bonus, the
// restore goes through the game's networked ApplyAdditive_Server, so clients get the additives
// back even if they do not have this mod installed.
// ---------------------------------------------------------------------------------------------

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
                // Only the host decides. Every machine runs this handler, but the host's restore
                // is broadcast to everyone, so clients must stay out of it.
                if (__instance == null || !GameAccess.IsServer) return;

                // The game's own handler does nothing when there is no plant (it can be signalled
                // twice); mirror that so we never restore after a no-op.
                if (!GameAccess.HasPlant(__instance)) return;

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
                if (__instance == null || __state == null || __state.Count == 0) return;

                // The handler has already decremented the uses, so this is "uses left for the
                // NEXT plant". At zero the game has cleared the soil itself (ClearSoil also clears
                // additives) and that is exactly when additives should end.
                int usesLeft = GameAccess.RemainingUses(__instance);
                if (usesLeft <= 0)
                {
                    ModLog.Verbose("Soil is spent; leaving additives cleared (" + __state.Count + " dropped).");
                    return;
                }

                int restored = 0;
                foreach (string id in __state)
                {
                    // Cheap safety net: never double-apply if something already put it back.
                    if (GameAccess.IsApplied(__instance, id)) continue;

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

    /// <summary>
    /// Gives Speed Grow's growth boost to every new plant, not just the first.
    /// </summary>
    [HarmonyPatch]
    internal static class Pot_PlantSeed_Patch
    {
        // The RPC method's name carries a build-specific hash (RpcLogic___PlantSeed_Server_<n>),
        // so find it by prefix instead of hard-coding the number. It is the server-side entry
        // point for every planting: the player sowing, an NPC sowing, and the game restoring a
        // saved plant. If a game update ever renames it, the mod logs the failed patch at startup.
        private static MethodBase TargetMethod()
            => AccessTools.GetDeclaredMethods(typeof(Pot))
                .FirstOrDefault(m => m.Name.StartsWith("RpcLogic___PlantSeed_Server", StringComparison.Ordinal));

        /// <summary>
        /// Runs after the game has planted the seed. On the host this is synchronous: by the time
        /// we run, the new Plant exists and has already received the pot's additives (without
        /// Speed Grow's boost, because the game treats that replay as "not newly applied").
        /// </summary>
        private static void Postfix(Pot __instance, float normalizedSeedProgress)
        {
            try
            {
                if (__instance == null || !GameAccess.IsServer) return;

                // The game plants saved plants through this same method when a save loads. Those
                // already have their progress saved; boosting them would add growth on every load.
                if (GameAccess.IsLoadingSave) return;

                // Fresh plantings (player and NPC) always pass 0. Anything else is a restored
                // plant, so this is a second guard on top of the loading check.
                if (normalizedSeedProgress > 0f) return;

                // The game refuses some seeds; no plant means nothing to boost.
                Plant plant = __instance.Plant;
                if (plant == null) return;

                float boost = GameAccess.TotalInstantGrowth(__instance);
                if (boost <= 0f)
                {
                    ModLog.Verbose("New plant, no growth additive on the pot.");
                    return;
                }

                GameAccess.AddGrowth(plant, boost);
                ModLog.Verbose("New plant: gave the pot's additives' growth boost of " + boost.ToString("0.###") + ".");
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not apply the replant growth boost: " + ex);
            }
        }
    }
}
