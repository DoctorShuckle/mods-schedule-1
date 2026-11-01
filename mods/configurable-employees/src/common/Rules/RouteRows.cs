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
    }
}
