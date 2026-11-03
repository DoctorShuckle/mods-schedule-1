using System;
using System.Collections.Generic;

namespace TeleportToProperties
{
    /// <summary>
    /// Which places the list offers, and in what order. Like the other Rules files it references no
    /// game, Unity or MelonLoader type, so it is unit-tested. It is generic so the game layer can
    /// hand in its own objects and get the same objects back.
    /// </summary>
    internal static class Destinations
    {
        /// <summary>
        /// Drops repeats (the same code twice, since a business is also a property), then orders
        /// properties before businesses and each group by name. Items with a blank code are kept and
        /// told apart by name instead. The order is stable between calls so the list does not jump.
        /// </summary>
        public static List<T> Order<T>(IEnumerable<T> items, Func<T, string> code, Func<T, string> name, Func<T, bool> isBusiness)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var kept = new List<T>();
            foreach (T item in items)
            {
                string key = string.IsNullOrWhiteSpace(code(item)) ? "name:" + name(item) : code(item);
                if (seen.Add(key)) kept.Add(item);
            }

            kept.Sort((a, b) =>
            {
                int byKind = isBusiness(a).CompareTo(isBusiness(b));
                if (byKind != 0) return byKind;
                int byName = string.Compare(name(a), name(b), StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : string.CompareOrdinal(code(a), code(b));
            });
            return kept;
        }
    }
}
