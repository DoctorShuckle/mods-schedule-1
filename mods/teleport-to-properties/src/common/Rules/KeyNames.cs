using System;

namespace TeleportToProperties
{
    internal static class KeyNames
    {
        /// <summary>
        /// Reads a key name such as "LeftBracket" or "f6" (case and spaces do not matter). False for
        /// blank text, a number, or a name that is not a key.
        /// </summary>
        public static bool TryParse<TKey>(string name, out TKey key) where TKey : struct, Enum
        {
            key = default;
            if (string.IsNullOrWhiteSpace(name)) return false;

            string text = name.Trim().Replace(" ", "");
            // Enum.TryParse also accepts "3" and "1,2": only a real name counts.
            if (char.IsDigit(text[0]) || text[0] == '-' || text.IndexOf(',') >= 0) return false;
            return Enum.TryParse(text, ignoreCase: true, out key);
        }
    }
}
