using MelonLoader;

namespace TeleportToProperties
{
    /// <summary>
    /// The mod's settings. They live in UserData/MelonPreferences.cfg under [TeleportToProperties].
    /// </summary>
    internal static class ModConfig
    {
        private static MelonPreferences_Entry<bool> _useInterior;
        private static MelonPreferences_Entry<bool> _enableHotkeys;
        private static MelonPreferences_Entry<string> _previousKey;
        private static MelonPreferences_Entry<string> _nextKey;
        private static MelonPreferences_Entry<string> _teleportKey;
        private static MelonPreferences_Entry<bool> _verbose;

        public static bool UseInterior { get; private set; }
        public static bool EnableHotkeys { get; private set; } = true;
        public static KeyCode PreviousKey { get; private set; } = KeyCode.LeftBracket;
        public static KeyCode NextKey { get; private set; } = KeyCode.RightBracket;
        public static KeyCode TeleportKey { get; private set; } = KeyCode.Backslash;

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("TeleportToProperties");

            _useInterior = category.CreateEntry("UseInteriorSpawn", false, "Use the interior spawn point",
                "false teleports to the outside spawn point of a property; true to the inside one (falls back to the other if a place has only one).");
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
            EnableHotkeys = _enableHotkeys.Value;
            ModLog.VerboseEnabled = _verbose.Value;
            PreviousKey = ReadKey(_previousKey, KeyCode.LeftBracket);
            NextKey = ReadKey(_nextKey, KeyCode.RightBracket);
            TeleportKey = ReadKey(_teleportKey, KeyCode.Backslash);
        }

        private static KeyCode ReadKey(MelonPreferences_Entry<string> entry, KeyCode fallback)
        {
            if (KeyNames.TryParse(entry.Value, out KeyCode key)) return key;
            ModLog.Warning(entry.DisplayName + " \"" + entry.Value + "\" is not a key name; using " + fallback + ".");
            return fallback;
        }
    }
}
