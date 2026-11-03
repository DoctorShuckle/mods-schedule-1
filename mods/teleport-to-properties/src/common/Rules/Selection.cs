using System;
using System.Collections.Generic;

namespace TeleportToProperties
{
    /// <summary>
    /// Which entry of the list is picked. The pick is remembered by code, not by position, so a
    /// property bought or lost while cycling does not move the pick to a different place.
    /// </summary>
    internal static class Selection
    {
        /// <summary>The position of the remembered code, or 0 when it is gone; -1 for an empty list.</summary>
        public static int Resolve(IList<string> codes, string remembered)
        {
            if (codes == null || codes.Count == 0) return -1;
            if (remembered != null)
            {
                for (int i = 0; i < codes.Count; i++)
                    if (string.Equals(codes[i], remembered, StringComparison.Ordinal)) return i;
            }
            return 0;
        }

        /// <summary>Moves by delta and wraps around either end. -1 for an empty list.</summary>
        public static int Step(int current, int count, int delta)
        {
            if (count <= 0) return -1;
            int next = (current + delta) % count;
            return next < 0 ? next + count : next;
        }
    }
}
