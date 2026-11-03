using MelonLoader;

namespace MixPackagedDrugs
{
    /// <summary>
    /// The mod's settings. They live in UserData/MelonPreferences.cfg under [MixPackagedDrugs].
    /// </summary>
    internal static class ModConfig
    {
        private static MelonPreferences_Entry<bool> _bags;
        private static MelonPreferences_Entry<bool> _jars;
        private static MelonPreferences_Entry<bool> _bricks;
        private static MelonPreferences_Entry<bool> _verbose;

        public static bool AllowBags { get; private set; } = true;
        public static bool AllowJars { get; private set; } = true;
        public static bool AllowBricks { get; private set; } = true;

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("MixPackagedDrugs");

            _bags = category.CreateEntry("AllowBaggies", true, "Allow baggies",
                "true lets a baggie (1 unit) go into a mixing station's product slot. It is unpacked there.");
            _jars = category.CreateEntry("AllowJars", true, "Allow jars",
                "true lets a jar (5 units) go into a mixing station's product slot. It is unpacked there.");
            _bricks = category.CreateEntry("AllowBricks", true, "Allow bricks",
                "true lets a brick (20 units) go into a mixing station's product slot. It is unpacked there.");
            _verbose = category.CreateEntry("VerboseLogging", false, "Verbose logging",
                "Log each package the mod lets in or unpacks. Turn on when reporting a problem.");
        }

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (_bags == null) return;

            AllowBags = _bags.Value;
            AllowJars = _jars.Value;
            AllowBricks = _bricks.Value;
            ModLog.VerboseEnabled = _verbose.Value;
        }
    }
}
