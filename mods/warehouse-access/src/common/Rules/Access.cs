using System;

namespace WarehouseAccess
{
    /// <summary>
    /// The mod's decisions as plain functions of plain values. Like the other Rules files it
    /// references no game, Unity or MelonLoader type, so it is unit-tested.
    /// </summary>
    internal static class Access
    {
        /// <summary>
        /// Whether the warehouse is opened up: the quest gate must pass, and then either the mod is
        /// set to always open or the player has the rank it asks for.
        /// </summary>
        public static bool ShouldOpen(bool questGatePassed, bool alwaysOpen, bool rankMet)
            => questGatePassed && (alwaysOpen || rankMet);

        /// <summary>
        /// Reads a rank name such as "Street_Rat" or "street rat" (case and spaces do not matter).
        /// False for blank text, a number, or a name that is not a rank.
        /// </summary>
        public static bool TryParseRank<TRank>(string name, out TRank rank) where TRank : struct, Enum
        {
            rank = default;
            if (string.IsNullOrWhiteSpace(name)) return false;

            string text = name.Trim().Replace(' ', '_');
            // Enum.TryParse also accepts "3" and "1,2": only a real name counts.
            if (char.IsDigit(text[0]) || text[0] == '-' || text.IndexOf(',') >= 0) return false;
            return Enum.TryParse(text, ignoreCase: true, out rank);
        }

        /// <summary>The game has five tiers in a rank, numbered 1 to 5.</summary>
        public static int Tier(int configured) => Math.Min(5, Math.Max(1, configured));
    }
}
