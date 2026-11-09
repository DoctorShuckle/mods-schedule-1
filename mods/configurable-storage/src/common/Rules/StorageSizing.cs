using System;

namespace ConfigurableStorage
{
    /// <summary>
    /// The mod's decisions as plain functions of plain values. Like the other Rules files it references no
    /// game, Unity or MelonLoader type, so it is unit-tested.
    /// </summary>
    internal static class StorageSizing
    {
        /// <summary>The game's own cap: StorageEntity.SlotCount is a [Range(1, 20)] field and MAX_SLOTS is 20.</summary>
        public const int MinSlots = 1;
        public const int MaxSlots = 20;

        /// <summary>The storage screen's row count is a [Range(1, 5)] field in the game.</summary>
        public const int MinRows = 1;
        public const int MaxRows = 5;

        /// <summary>
        /// The slot count for a container. 0 (or less) means "as in the game"; anything else is clamped to
        /// 1 to <see cref="MaxSlots"/>. A count below the game's own is allowed: items saved in slots past the
        /// new size are lost on the next load and save, which the README warns about.
        /// </summary>
        public static int Slots(int configured, int gameSlots)
            => configured <= 0 ? gameSlots : Math.Max(MinSlots, Math.Min(MaxSlots, configured));

        /// <summary>
        /// Rows for the storage screen. A configured row count (1 to 5) is used as it is, never more rows than
        /// slots. With none configured the rows follow the slot count and keep the game's width: a container
        /// the game lays out 2 across stays 2 across and grows downward, up to 5 rows, after which it grows wider.
        /// A container whose slot count is unchanged keeps the game's rows.
        /// </summary>
        public static int Rows(int configuredRows, int slots, int gameSlots, int gameRows)
        {
            int safeSlots = Math.Max(1, slots);
            if (configuredRows > 0) return Math.Max(MinRows, Math.Min(Math.Min(MaxRows, configuredRows), safeSlots));
            if (slots == gameSlots) return Math.Max(MinRows, Math.Min(MaxRows, gameRows));

            int columns = Math.Max(1, (int)Math.Ceiling(Math.Max(1, gameSlots) / (double)Math.Max(1, gameRows)));
            int rows = (int)Math.Ceiling(safeSlots / (double)columns);
            return Math.Max(MinRows, Math.Min(Math.Min(MaxRows, rows), safeSlots));
        }
    }
}
