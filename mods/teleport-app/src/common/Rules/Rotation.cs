using System;
using System.Collections.Generic;
using System.Linq;

namespace TeleportApp
{
    /// <summary>
    /// Which places the hotkeys step through (the "rotation"): whole kinds can be left out, and so can
    /// single places. Stored as text in the settings; game-free, so unit-tested.
    /// </summary>
    internal static class Rotation
    {
        private const char Separator = '|';
        private const string NoKinds = "None";

        /// <summary>Reads a list of place codes. Blank text is an empty list.</summary>
        public static HashSet<string> ParseCodes(string text)
        {
            var codes = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(text)) return codes;
            foreach (string part in text.Split(Separator))
            {
                string code = part.Trim();
                if (code.Length > 0) codes.Add(code);
            }
            return codes;
        }

        /// <summary>Writes the codes in a fixed order so the settings file does not change for no reason.</summary>
        public static string JoinCodes(IEnumerable<string> codes)
            => string.Join(Separator.ToString(), codes.OrderBy(c => c, StringComparer.Ordinal));

        /// <summary>
        /// Reads the kinds in the rotation, such as "Property|Dealer". Blank text means every kind (the
        /// default); "None" means no kind. Names that are not kinds are ignored.
        /// </summary>
        public static HashSet<PlaceKind> ParseKinds(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return AllKinds();
            var kinds = new HashSet<PlaceKind>();
            foreach (string part in text.Split(Separator))
                if (Enum.TryParse(part.Trim(), ignoreCase: true, out PlaceKind kind) && Enum.IsDefined(typeof(PlaceKind), kind))
                    kinds.Add(kind);
            return kinds;
        }

        public static string JoinKinds(IEnumerable<PlaceKind> kinds)
        {
            List<PlaceKind> ordered = kinds.Distinct().OrderBy(k => (int)k).ToList();
            return ordered.Count == 0 ? NoKinds : string.Join(Separator.ToString(), ordered);
        }

        public static HashSet<PlaceKind> AllKinds() => new HashSet<PlaceKind>((PlaceKind[])Enum.GetValues(typeof(PlaceKind)));

        /// <summary>A place is in the rotation when its kind is and the place itself has not been left out.</summary>
        public static bool Includes(ISet<PlaceKind> kinds, ISet<string> excluded, PlaceKind kind, string code)
            => kinds.Contains(kind) && !excluded.Contains(code);
    }
}
