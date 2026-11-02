using System;
using System.Reflection;

namespace WarehouseAccess
{
    /// <summary>
    /// Every read of or call into the game goes through here. This file holds the runtime
    /// differences between Mono and IL2CPP, so the patches stay identical for both builds.
    /// </summary>
    internal static class GameAccess
    {
        // How often to search the scene again for the quest while it has not been found.
        private const float QuestSearchSeconds = 5f;

        private static Quest_Warehouse _quest;
        private static float _nextQuestSearch;

#if !IL2CPP
        // Private on Mono; the IL2CPP proxy exposes it as a public method.
        private static readonly MethodInfo GuardOfficeMethod =
            typeof(Igor).GetMethod("GuardOffice", BindingFlags.Instance | BindingFlags.NonPublic);
#endif

        /// <summary>
        /// True once the warehouse quest is completed. The state is read live each time, so loading a
        /// different save is handled; only the lookup of the quest object is remembered.
        /// </summary>
        public static bool WarehouseQuestCompleted()
        {
            if (_quest == null)
            {
                if (Time.unscaledTime < _nextQuestSearch) return false;
                _nextQuestSearch = Time.unscaledTime + QuestSearchSeconds;
                _quest = UnityEngine.Object.FindObjectOfType<Quest_Warehouse>();
                if (_quest == null) return false;
            }
            return _quest.State == EQuestState.Completed;
        }

        /// <summary>
        /// True when the player has the rank the mod asks for: the game's own door rank, or the
        /// configured one. False while the game has not finished loading.
        /// </summary>
        public static bool RankMet()
        {
            LevelManager levels = NetworkSingleton<LevelManager>.Instance;
            DarkMarket market = NetworkSingleton<DarkMarket>.Instance;
            if (levels == null || market == null) return false;

            FullRank needed = ModConfig.UseCustomRank
                ? new FullRank(ModConfig.MinimumRank, ModConfig.MinimumTier)
                : market.UnlockRank;
            return levels.GetFullRank() >= needed;
        }

        /// <summary>Sends Igor to his office, as the game does when the warehouse is unlocked.</summary>
        public static void GuardOffice(Igor igor)
        {
#if IL2CPP
            igor.GuardOffice();
#else
            GuardOfficeMethod.Invoke(igor, null);
#endif
        }
    }
}
