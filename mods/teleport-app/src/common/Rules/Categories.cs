using System.Collections.Generic;

namespace TeleportApp
{
    /// <summary>The kinds of place the phone offers, in the order they are listed.</summary>
    public enum PlaceKind
    {
        Property = 0,
        Business = 1,
        Dealer = 2,
        Store = 3
    }

    /// <summary>The first step of the phone's picker: which kind of place. Game-free, so unit-tested.</summary>
    internal static class Categories
    {
        /// <summary>How many kinds there are.</summary>
        public const int Count = 4;

        /// <summary>The kinds that have at least one place, in list order. Empty kinds are not offered.</summary>
        public static List<PlaceKind> NonEmpty(IList<int> countByKind)
        {
            var kinds = new List<PlaceKind>();
            for (int i = 0; i < countByKind.Count; i++)
                if (countByKind[i] > 0) kinds.Add((PlaceKind)i);
            return kinds;
        }

        /// <summary>The plural name of a kind, such as "Properties".</summary>
        public static string Name(PlaceKind kind)
        {
            switch (kind)
            {
                case PlaceKind.Property: return "Properties";
                case PlaceKind.Business: return "Businesses";
                case PlaceKind.Dealer: return "Dealers";
                default: return "Stores";
            }
        }

        /// <summary>The text of a category button, such as "Properties (3)".</summary>
        public static string Label(PlaceKind kind, int count) => Name(kind) + " (" + count + ")";
    }
}
