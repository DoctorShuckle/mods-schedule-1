using System;
using System.Globalization;

namespace ConfigurableEmployees
{
    /// <summary>
    /// One property that can have employees: its setting in the cfg, the game's code for it, and the
    /// capacity the player configured. Like everything in the Rules folder this file must not
    /// reference any game, Unity or MelonLoader type.
    /// </summary>
    internal sealed class PropertyEntry
    {
        /// <summary>The game's code for the property (what the Property object reports).</summary>
        public readonly string Code;

        /// <summary>The key in the cfg.</summary>
        public readonly string Key;

        /// <summary>The property's name as the game shows it.</summary>
        public readonly string Label;

        /// <summary>The game's employee capacity, or 0 where it has not been seen yet (the log shows it at startup).</summary>
        public readonly int GameCapacity;

        /// <summary>The configured capacity; -1 leaves the game's alone.</summary>
        public int Configured = -1;

        public PropertyEntry(string code, string key, string label, int gameCapacity)
        {
            Code = code;
            Key = key;
            Label = label;
            GameCapacity = gameCapacity;
        }

        /// <summary>The cfg comment: what the setting does, the game's value if known, and what -1 means.</summary>
        public string Describe()
            => "Most employees the " + Label + " can have"
               + (GameCapacity > 0 ? " (game: " + GameCapacity.ToString(CultureInfo.InvariantCulture) + ")" : " (the game's value is in the log at startup)")
               + ". -1 leaves the game's value alone. Needs a game restart.";
    }

    /// <summary>
    /// The properties that can hold employees. The RV, the motel room and the businesses are left out of
    /// the Fixer's hiring menu by the game, and the sewer office has a capacity of 0 and no idle points
    /// (the mod only adds idle points next to an existing one), so a setting for any of them would
    /// never be used. The game's capacities are from the startup log.
    /// </summary>
    internal static class PropertyCatalog
    {
        public static readonly PropertyEntry Barn = new PropertyEntry("barn", "Barn", "Barn", 10);
        public static readonly PropertyEntry Bungalow = new PropertyEntry("bungalow", "Bungalow", "Bungalow", 5);
        public static readonly PropertyEntry DocksWarehouse = new PropertyEntry("dockswarehouse", "DocksWarehouse", "Docks Warehouse", 12);
        public static readonly PropertyEntry Manor = new PropertyEntry("manor", "Manor", "Hyland Manor", 12);
        public static readonly PropertyEntry StorageUnit = new PropertyEntry("storageunit", "StorageUnit", "Storage Unit", 3);
        public static readonly PropertyEntry Sweatshop = new PropertyEntry("sweatshop", "Sweatshop", "Sweatshop", 1);

        /// <summary>Every entry, in the order the cfg lists them.</summary>
        public static readonly PropertyEntry[] All = { Barn, Bungalow, DocksWarehouse, Manor, StorageUnit, Sweatshop };

        /// <summary>The entry for a property code (matched without regard to case), or null for a property the mod has no setting for.</summary>
        public static PropertyEntry Find(string code)
        {
            if (code == null) return null;
            foreach (PropertyEntry entry in All)
                if (string.Equals(entry.Code, code, StringComparison.OrdinalIgnoreCase)) return entry;
            return null;
        }
    }
}
