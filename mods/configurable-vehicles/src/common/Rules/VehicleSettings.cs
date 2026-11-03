using System;

namespace ConfigurableVehicles
{
    /// <summary>
    /// The mod's decisions as plain functions of plain values. Like the other Rules files it
    /// references no game, Unity or MelonLoader type, so it is unit-tested.
    /// </summary>
    internal static class VehicleSettings
    {
        /// <summary>The trunk's slot count is a [Range(1, 20)] field in the game; the same bounds are kept.</summary>
        public const int MinSlots = 1;
        public const int MaxSlots = 20;

        /// <summary>The trunk screen's row count is a [Range(1, 5)] field in the game.</summary>
        public const int MinRows = 1;
        public const int MaxRows = 5;

        /// <summary>
        /// An override value. Zero means "leave the game's value", so anything unusable (not a number,
        /// negative) is zero too, and a typo like 1000000 is capped at <paramref name="max"/>.
        /// </summary>
        public static float Positive(float configured, float max)
        {
            if (float.IsNaN(configured) || float.IsInfinity(configured) || configured <= 0f) return 0f;
            return Math.Min(configured, max);
        }

        /// <summary>A slot count: 0 or less means "leave the game's size", anything else is clamped to the game's range.</summary>
        public static int Slots(int configured) => configured <= 0 ? 0 : Math.Max(MinSlots, Math.Min(MaxSlots, configured));

        /// <summary>The all-vehicles slot count, which is written as text: empty or not a number means none.</summary>
        public static int SlotsFromText(string text) => int.TryParse(text?.Trim(), out int slots) ? Slots(slots) : 0;

        /// <summary>
        /// Rows for the trunk screen, which lays slots out in a fixed number of rows: the configured
        /// count, clamped to the game's 1 to 5, and never more rows than slots.
        /// </summary>
        public static int RowsFor(int configuredRows, int slots)
            => Math.Max(MinRows, Math.Min(Math.Min(MaxRows, configuredRows), Math.Max(1, slots)));

        /// <summary>The trunk size for a vehicle: its own setting, else the one for all vehicles, else 0 (leave as in the game).</summary>
        public static int SlotsFor(int vehicleSlots, int allVehiclesSlots) => vehicleSlots > 0 ? vehicleSlots : allVehiclesSlots;
    }
}
