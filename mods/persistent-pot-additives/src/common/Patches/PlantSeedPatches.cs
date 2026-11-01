using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace PersistentPotAdditives
{
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
                // Cheap early exit for clients; ShouldBoost checks it again with the rest.
                if (__instance == null || !GameAccess.IsServer) return;

                // The game refuses some seeds; no plant means nothing to boost.
                Plant plant = __instance.Plant;
                float boost = GameAccess.TotalInstantGrowth(__instance);

                // Saved plants are re-planted through this same method when a save loads, with
                // their progress already saved; boosting them would add growth on every load.
                if (!ReplantBoost.ShouldBoost(true, GameAccess.IsLoadingSave, normalizedSeedProgress, plant != null, boost))
                {
                    ModLog.Verbose("No replant boost (progress " + normalizedSeedProgress.ToString("0.###") +
                                   ", plant " + (plant != null) + ", pot boost " + boost.ToString("0.###") + ").");
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
