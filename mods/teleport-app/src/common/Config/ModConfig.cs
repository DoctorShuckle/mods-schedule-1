using System.Collections.Generic;
using MelonLoader;

namespace TeleportApp
{
    /// <summary>
    /// The mod's settings. They live in UserData/MelonPreferences.cfg under [TeleportApp].
    /// </summary>
    internal static class ModConfig
    {
        private static MelonPreferences_Entry<bool> _useInterior;
        private static MelonPreferences_Entry<bool> _showProperties;
        private static MelonPreferences_Entry<bool> _onlyOwnedProperties;
        private static MelonPreferences_Entry<bool> _showBusinesses;
        private static MelonPreferences_Entry<bool> _onlyOwnedBusinesses;
        private static MelonPreferences_Entry<bool> _showDealers;
        private static MelonPreferences_Entry<bool> _onlyHiredDealers;
        private static MelonPreferences_Entry<bool> _showStores;
        private static MelonPreferences_Entry<string> _rotationKinds;
        private static MelonPreferences_Entry<string> _rotationExcluded;
        private static MelonPreferences_Entry<bool> _enableHotkeys;
        private static MelonPreferences_Entry<string> _previousKey;
        private static MelonPreferences_Entry<string> _nextKey;
        private static MelonPreferences_Entry<string> _teleportKey;
        private static MelonPreferences_Entry<bool> _verbose;

        public static bool UseInterior { get; private set; }
        public static bool ShowProperties { get; private set; } = true;
        public static bool OnlyOwnedProperties { get; private set; } = true;
        public static bool ShowBusinesses { get; private set; } = true;
        public static bool OnlyOwnedBusinesses { get; private set; } = true;
        public static bool ShowDealers { get; private set; } = true;
        public static bool OnlyHiredDealers { get; private set; } = true;
        public static bool ShowStores { get; private set; } = true;
        public static HashSet<PlaceKind> RotationKinds { get; private set; } = Rotation.AllKinds();
        public static HashSet<string> RotationExcluded { get; private set; } = new HashSet<string>();
        public static bool EnableHotkeys { get; private set; } = true;
        public static KeyCode PreviousKey { get; private set; } = KeyCode.LeftBracket;
        public static KeyCode NextKey { get; private set; } = KeyCode.RightBracket;
        public static KeyCode TeleportKey { get; private set; } = KeyCode.Backslash;

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("TeleportApp");

            _useInterior = category.CreateEntry("UseInteriorSpawn", false, "Use the interior spawn point",
                "false teleports to the outside spawn point of a property; true to the inside one (falls back to the other if a place has only one).");
            _showProperties = category.CreateEntry("ShowProperties", true, "List properties",
                "false takes the Properties category out of the phone list and the hotkeys.");
            _onlyOwnedProperties = category.CreateEntry("OnlyOwnedProperties", true, "Only owned properties",
                "true lists only properties you own; false lists every property, owned or not (not-owned ones are marked).");
            _showBusinesses = category.CreateEntry("ShowBusinesses", true, "List businesses",
                "false takes the Businesses category out of the phone list and the hotkeys.");
            _onlyOwnedBusinesses = category.CreateEntry("OnlyOwnedBusinesses", true, "Only owned businesses",
                "true lists only businesses you own; false lists every business, owned or not (not-owned ones are marked).");
            _showDealers = category.CreateEntry("ShowDealers", true, "List dealers",
                "false takes the Dealers category out of the phone list and the hotkeys.");
            _onlyHiredDealers = category.CreateEntry("OnlyHiredDealers", true, "Only hired dealers",
                "true lists only dealers you have hired; false lists every dealer you can recruit (not-hired ones are marked).");
            _showStores = category.CreateEntry("ShowStores", true, "List stores",
                "false takes the Stores category out of the phone list and the hotkeys. Stores have no owned state, so there is nothing else to set.");
            _rotationKinds = category.CreateEntry("RotationKinds", "", "Kinds in the hotkey rotation",
                "Which kinds the hotkeys step through, such as Property|Dealer. Blank means every kind, None means no kind. Easiest to change on the phone: TP, then Hotkey rotation.");
            _rotationExcluded = category.CreateEntry("RotationExcluded", "", "Places left out of the hotkey rotation",
                "Codes of places the hotkeys skip, separated by |. Easiest to change on the phone: TP, then Hotkey rotation.");
            _enableHotkeys = category.CreateEntry("EnableHotkeys", true, "Enable the hotkeys",
                "Turns the three keys below on or off.");
            _previousKey = category.CreateEntry("PreviousKey", "LeftBracket", "Previous place key",
                "Key that moves the pick to the previous place. A Unity KeyCode name such as LeftBracket or F6.");
            _nextKey = category.CreateEntry("NextKey", "RightBracket", "Next place key",
                "Key that moves the pick to the next place.");
            _teleportKey = category.CreateEntry("TeleportKey", "Backslash", "Teleport key",
                "Key that teleports to the picked place. Map a Steam Deck button to this key in Steam Input.");
            _verbose = category.CreateEntry("VerboseLogging", false, "Verbose logging",
                "Log each pick and teleport. Turn on when reporting a problem.");
        }

        /// <summary>Copies the current preference values into the places the code reads them.</summary>
        public static void Apply()
        {
            if (_useInterior == null) return;

            UseInterior = _useInterior.Value;
            ShowProperties = _showProperties.Value;
            OnlyOwnedProperties = _onlyOwnedProperties.Value;
            ShowBusinesses = _showBusinesses.Value;
            OnlyOwnedBusinesses = _onlyOwnedBusinesses.Value;
            ShowDealers = _showDealers.Value;
            OnlyHiredDealers = _onlyHiredDealers.Value;
            ShowStores = _showStores.Value;
            RotationKinds = Rotation.ParseKinds(_rotationKinds.Value);
            RotationExcluded = Rotation.ParseCodes(_rotationExcluded.Value);
            EnableHotkeys = _enableHotkeys.Value;
            ModLog.VerboseEnabled = _verbose.Value;
            PreviousKey = ReadKey(_previousKey, KeyCode.LeftBracket);
            NextKey = ReadKey(_nextKey, KeyCode.RightBracket);
            TeleportKey = ReadKey(_teleportKey, KeyCode.Backslash);
        }

        public static bool InRotation(PlaceKind kind, string code) => Rotation.Includes(RotationKinds, RotationExcluded, kind, code);

        /// <summary>Puts a whole kind in or out of the hotkey rotation and saves it.</summary>
        public static void SetKindInRotation(PlaceKind kind, bool included)
        {
            if (included) RotationKinds.Add(kind); else RotationKinds.Remove(kind);
            _rotationKinds.Value = Rotation.JoinKinds(RotationKinds);
            MelonPreferences.Save();
        }

        /// <summary>Puts one place in or out of the hotkey rotation and saves it.</summary>
        public static void SetPlaceInRotation(string code, bool included)
        {
            if (included) RotationExcluded.Remove(code); else RotationExcluded.Add(code);
            _rotationExcluded.Value = Rotation.JoinCodes(RotationExcluded);
            MelonPreferences.Save();
        }

        private static KeyCode ReadKey(MelonPreferences_Entry<string> entry, KeyCode fallback)
        {
            if (KeyNames.TryParse(entry.Value, out KeyCode key)) return key;
            ModLog.Warning(entry.DisplayName + " \"" + entry.Value + "\" is not a key name; using " + fallback + ".");
            return fallback;
        }
    }
}
