using System;

namespace ConfigurableEmployees
{
    /// <summary>The rows of the packager's routes list on the management screen.</summary>
    internal static class RouteRows
    {
        public const int Max = 20;

        /// <summary>How many route rows the management screen must gain to show <paramref name="wanted"/>.</summary>
        public static int RowsToAdd(int wanted, int have)
            => Math.Max(0, Math.Min(wanted, Max) - have);

        /// <summary>
        /// How far a scroll area's content must move, in the viewport's own units, so a row stays in
        /// view. Positive means the row is above the top edge, negative below the bottom edge; the
        /// content is moved by subtracting it from its position, as the game's own panels do. A row
        /// taller than the viewport is aligned to its top.
        /// </summary>
        public static float ScrollDelta(float childMin, float childMax, float viewMin, float viewMax)
        {
            if (childMax > viewMax) return childMax - viewMax;
            if (childMin < viewMin) return childMin - viewMin;
            return 0f;
        }
    }
}
