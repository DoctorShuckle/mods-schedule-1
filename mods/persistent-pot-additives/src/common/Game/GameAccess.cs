using System.Collections.Generic;
using HarmonyLib;

namespace PersistentPotAdditives
{
    /// <summary>
    /// Every read of or call into the game goes through here. This file holds the runtime
    /// differences between Mono and IL2CPP, so the patches stay identical for both builds.
    /// </summary>
    internal static class GameAccess
    {
#if !IL2CPP
        // Protected field on Mono; the IL2CPP proxy exposes it as a public property.
        private static readonly AccessTools.FieldRef<GrowContainer, int> RemainingUsesRef =
            AccessTools.FieldRefAccess<GrowContainer, int>("_remainingSoilUses");
#endif

        /// <summary>
        /// True on the host (or in single-player). All decisions are made there; clients are told
        /// what to do through the game's own networked calls, so clients do not need this mod.
        /// </summary>
        public static bool IsServer => InstanceFinder.IsServer;

        /// <summary>
        /// True while a save is loading. The game re-plants every saved plant through the same
        /// method as a fresh planting, and we must not treat those as new plantings.
        /// </summary>
        public static bool IsLoadingSave
        {
            get
            {
                LoadManager loadManager = Singleton<LoadManager>.Instance;
                return loadManager != null && loadManager.IsLoading;
            }
        }

        /// <summary>The game's own count of soil uses left. It is saved by the game itself.</summary>
        public static int RemainingUses(GrowContainer container)
        {
#if IL2CPP
            return container._remainingSoilUses;
#else
            return RemainingUsesRef(container);
#endif
        }

        public static bool HasPlant(Pot pot) => pot.Plant != null;

        /// <summary>
        /// Ids of the additives currently on the pot. Returns a copy: the game mutates its own list
        /// (it clears it on harvest), so a live reference would be emptied under us.
        /// </summary>
        public static List<string> AppliedAdditiveIds(Pot pot)
        {
            var ids = new List<string>();
            var applied = pot.AppliedAdditives; // List<T> on Mono, Il2CppSystem List<T> on IL2CPP
            if (applied == null) return ids;

            for (int i = 0; i < applied.Count; i++)
            {
                AdditiveDefinition additive = applied[i];
                if (additive != null) ids.Add(additive.ID);
            }
            return ids;
        }

        /// <summary>
        /// Total one-shot growth boost of every additive on the pot (only Speed Grow has one).
        /// The game only grants this boost at the moment an additive is first applied.
        /// </summary>
        public static float TotalInstantGrowth(Pot pot)
        {
            var boosts = new List<float>();
            var applied = pot.AppliedAdditives;
            if (applied == null) return 0f;

            for (int i = 0; i < applied.Count; i++)
            {
                AdditiveDefinition additive = applied[i];
                if (additive != null) boosts.Add(additive.InstantGrowth);
            }
            return ReplantBoost.TotalGrowth(boosts);
        }

        public static bool IsApplied(Pot pot, string additiveId) => pot.IsAdditiveApplied(additiveId);

        /// <summary>
        /// Networked apply: the server runs it and tells every client, so the additive reappears
        /// on every machine (including ones that do not have this mod).
        /// </summary>
        public static void ApplyAdditive(Pot pot, string additiveId) => pot.ApplyAdditive_Server(additiveId);

        /// <summary>
        /// Advances the plant's growth by <paramref name="amount"/> (0..1 of total growth). The
        /// game clamps the result at fully grown.
        /// </summary>
        public static void AddGrowth(Plant plant, float amount)
            => plant.SetNormalizedGrowthProgress(plant.NormalizedGrowthProgress + amount);
    }
}
