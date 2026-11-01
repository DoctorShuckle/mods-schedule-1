using System;

namespace ConfigurableEmployees
{
    /// <summary>Per-property employee capacity: which value applies, and the idle points a larger capacity needs.</summary>
    internal static class PropertyCapacity
    {
        public const int Max = 50;

        /// <summary>The capacity a property should have: the configured one (at least 1, at most <see cref="Max"/>), else the game's.</summary>
        public static int Resolve(string propertyCode, int game)
        {
            PropertyEntry entry = PropertyCatalog.Find(propertyCode);
            return entry != null && entry.Configured >= 1 ? Math.Min(entry.Configured, Max) : game;
        }

        /// <summary>
        /// Each employee waits at the idle point matching their index, so a property needs at
        /// least as many idle points as its capacity or the game indexes past the array.
        /// </summary>
        public static int IdlePointsToAdd(int capacity, int have) => Math.Max(0, capacity - have);
    }
}
