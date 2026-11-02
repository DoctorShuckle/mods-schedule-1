using System;
using MelonLoader;

namespace WarehouseAccess
{
    /// <summary>
    /// The mod's settings. They live in UserData/MelonPreferences.cfg under [WarehouseAccess].
    /// </summary>
    internal static class ModConfig
    {
        private static MelonPreferences_Entry<bool> _alwaysOpen;
        private static MelonPreferences_Entry<bool> _skipQuest;
        private static MelonPreferences_Entry<bool> _useCustomRank;
        private static MelonPreferences_Entry<string> _minimumRank;
        private static MelonPreferences_Entry<int> _minimumTier;
        private static MelonPreferences_Entry<bool> _verbose;

        public static bool AlwaysOpen { get; private set; } = true;
        public static bool SkipQuest { get; private set; }
        public static bool UseCustomRank { get; private set; }
        public static ERank MinimumRank { get; private set; } = ERank.Street_Rat;
        public static int MinimumTier { get; private set; } = 1;

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("WarehouseAccess");

            _alwaysOpen = category.CreateEntry("AlwaysOpen", true, "Always open",
                "true opens the warehouse for everyone, at every hour. false opens it only for players who have the rank below.");
            _skipQuest = category.CreateEntry("SkipWarehouseQuest", false, "Skip the warehouse quest",
                "true lets the mod work before the warehouse quest is completed. Nothing is written to the quest or the save.");
            _useCustomRank = category.CreateEntry("UseCustomRank", false, "Use a custom rank",
                "Only matters when AlwaysOpen is false. false asks for the rank the game itself asks for at the door; true asks for MinimumRank and MinimumTier.");
            _minimumRank = category.CreateEntry("MinimumRank", "Street_Rat", "Minimum rank",
                "The rank name used when UseCustomRank is true, such as Hoodlum or Block_Boss.");
            _minimumTier = category.CreateEntry("MinimumTier", 1, "Minimum tier",
                "The tier (1 to 5) used with MinimumRank when UseCustomRank is true.");
            _verbose = category.CreateEntry("VerboseLogging", false, "Verbose logging",
                "Log each time the mod opens the door or moves Igor. Turn on when reporting a problem.");
        }

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (_alwaysOpen == null) return;

            AlwaysOpen = _alwaysOpen.Value;
            SkipQuest = _skipQuest.Value;
            UseCustomRank = _useCustomRank.Value;
            MinimumTier = Access.Tier(_minimumTier.Value);
            ModLog.VerboseEnabled = _verbose.Value;

            if (Access.TryParseRank(_minimumRank.Value, out ERank rank))
            {
                MinimumRank = rank;
            }
            else
            {
                MinimumRank = ERank.Street_Rat;
                ModLog.Warning("MinimumRank \"" + _minimumRank.Value + "\" is not a rank name; using Street_Rat.");
            }
        }
    }
}
