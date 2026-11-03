using System.Collections.Generic;
using System.Text;

namespace MixPackagedDrugs
{
    /// <summary>
    /// The text format of the small file that remembers, per mixing station, the packaging its mix will
    /// come out in. One line per station: the station's GUID, a tab, the packaging item's ID. A line that
    /// does not read is skipped, so a damaged file loses entries but never breaks the game.
    /// </summary>
    public static class PackagingMemory
    {
        private const string Header = "# MixPackagedDrugs: packaging of the mixes in progress, by mixing station GUID.";

        public static string Serialize(IDictionary<string, string> packagingByStation)
        {
            var text = new StringBuilder(Header).Append('\n');
            var keys = new List<string>(packagingByStation.Keys);
            keys.Sort(string.CompareOrdinal);
            foreach (string key in keys)
            {
                string value = packagingByStation[key];
                if (!IsField(key) || !IsField(value)) continue;
                text.Append(key).Append('\t').Append(value).Append('\n');
            }
            return text.ToString();
        }

        public static Dictionary<string, string> Parse(string text)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(text)) return result;
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#') continue;
                string[] fields = line.Split('\t');
                if (fields.Length != 2 || !IsField(fields[0]) || !IsField(fields[1])) continue;
                result[fields[0]] = fields[1];
            }
            return result;
        }

        /// <summary>
        /// A file name for a save: the save folder's own name, made safe, plus a hash of the whole path so two
        /// saves with the same folder name elsewhere do not share a file. The hash is FNV-1a, which is the same on
        /// every run (string.GetHashCode is not).
        /// </summary>
        public static string FileNameFor(string saveFolderPath)
        {
            string path = (saveFolderPath ?? string.Empty).TrimEnd('/', '\\');
            int cut = path.LastIndexOfAny(new[] { '/', '\\' });
            string name = cut >= 0 ? path.Substring(cut + 1) : path;

            var safe = new StringBuilder();
            foreach (char c in name)
                safe.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            if (safe.Length == 0) safe.Append("save");
            if (safe.Length > 40) safe.Length = 40;

            uint hash = 2166136261;
            foreach (char c in path)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return safe + "-" + hash.ToString("x8") + ".txt";
        }

        private static bool IsField(string value)
            => !string.IsNullOrEmpty(value) && value.IndexOfAny(new[] { '\t', '\n', '\r' }) < 0;
    }
}
